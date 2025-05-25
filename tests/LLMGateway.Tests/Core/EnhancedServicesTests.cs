using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Services;
using LLMGateway.Core.Options;
using LLMGateway.Core.Models.ContentFiltering;

namespace LLMGateway.Tests.Core;

/// <summary>
/// Tests for enhanced services
/// </summary>
public class EnhancedServicesTests
{
    [Fact]
    public async Task CircuitBreakerService_ShouldOpenCircuitAfterFailures()
    {
        // Arrange
        var logger = new Mock<ILogger<CircuitBreakerService>>();
        var circuitBreaker = new CircuitBreakerService(logger.Object);
        var failureCount = 0;

        // Act & Assert
        for (int i = 0; i < 6; i++) // 5 failures should open the circuit
        {
            try
            {
                await circuitBreaker.ExecuteAsync("test-circuit", async () =>
                {
                    failureCount++;
                    throw new InvalidOperationException($"Failure {failureCount}");
                }, failureThreshold: 5, timeout: TimeSpan.FromSeconds(1));
            }
            catch (InvalidOperationException)
            {
                // Expected for the first 5 failures
                if (i < 5)
                {
                    Assert.False(circuitBreaker.IsCircuitOpen("test-circuit"));
                }
            }
            catch (Exception)
            {
                // Circuit breaker should be open after 5 failures
                Assert.True(circuitBreaker.IsCircuitOpen("test-circuit"));
                break;
            }
        }

        // Verify circuit is open
        Assert.True(circuitBreaker.IsCircuitOpen("test-circuit"));
        
        // Verify circuit state
        var state = circuitBreaker.GetCircuitState("test-circuit");
        Assert.True(state.IsOpen);
        Assert.Equal(5, state.FailureCount);
        Assert.NotNull(state.LastException);
    }

    [Fact]
    public void CircuitBreakerService_ShouldResetCircuit()
    {
        // Arrange
        var logger = new Mock<ILogger<CircuitBreakerService>>();
        var circuitBreaker = new CircuitBreakerService(logger.Object);

        // Act
        circuitBreaker.ResetCircuit("test-circuit");

        // Assert
        Assert.False(circuitBreaker.IsCircuitOpen("test-circuit"));
        
        var state = circuitBreaker.GetCircuitState("test-circuit");
        Assert.False(state.IsOpen);
        Assert.Equal(0, state.FailureCount);
        Assert.Null(state.LastException);
    }

    [Fact]
    public void TiktokenTokenCountingService_ShouldCountTokensAccurately()
    {
        // Arrange
        var logger = new Mock<ILogger<TiktokenTokenCountingService>>();
        var modelService = new Mock<IModelService>();
        var providerFactory = new Mock<ILLMProviderFactory>();
        
        var tokenCountingService = new TiktokenTokenCountingService(
            logger.Object, 
            modelService.Object, 
            providerFactory.Object);

        // Act
        var tokenCount = tokenCountingService.CountTokens("Hello, world!", "gpt-4");

        // Assert
        Assert.True(tokenCount > 0);
        Assert.True(tokenCount < 10); // Should be around 3-4 tokens for "Hello, world!"
    }

    [Fact]
    public void TiktokenTokenCountingService_ShouldHandleEmptyText()
    {
        // Arrange
        var logger = new Mock<ILogger<TiktokenTokenCountingService>>();
        var modelService = new Mock<IModelService>();
        var providerFactory = new Mock<ILLMProviderFactory>();
        
        var tokenCountingService = new TiktokenTokenCountingService(
            logger.Object, 
            modelService.Object, 
            providerFactory.Object);

        // Act
        var tokenCount = tokenCountingService.CountTokens("", "gpt-4");

        // Assert
        Assert.Equal(0, tokenCount);
    }

    [Fact]
    public void TiktokenTokenCountingService_ShouldHandleNullText()
    {
        // Arrange
        var logger = new Mock<ILogger<TiktokenTokenCountingService>>();
        var modelService = new Mock<IModelService>();
        var providerFactory = new Mock<ILLMProviderFactory>();
        
        var tokenCountingService = new TiktokenTokenCountingService(
            logger.Object, 
            modelService.Object, 
            providerFactory.Object);

        // Act
        var tokenCount = tokenCountingService.CountTokens(null!, "gpt-4");

        // Assert
        Assert.Equal(0, tokenCount);
    }

    [Fact]
    public async Task MLBasedContentFilteringService_ShouldAllowSafeContent()
    {
        // Arrange
        var logger = new Mock<ILogger<MLBasedContentFilteringService>>();
        var options = Options.Create(new ContentFilteringOptions
        {
            EnableContentFiltering = true,
            FilterPrompts = true,
            UseMLFiltering = false, // Disable ML filtering for this test
            BlockedTerms = new List<string> { "badword" },
            BlockedPatterns = new List<string>()
        });
        var completionService = new Mock<ICompletionService>();
        var metricsService = new Mock<IMetricsService>();

        var contentFilteringService = new MLBasedContentFilteringService(
            logger.Object,
            options,
            completionService.Object,
            metricsService.Object);

        // Act
        var result = await contentFilteringService.FilterPromptAsync("This is a safe prompt about machine learning.");

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task MLBasedContentFilteringService_ShouldBlockBadContent()
    {
        // Arrange
        var logger = new Mock<ILogger<MLBasedContentFilteringService>>();
        var options = Options.Create(new ContentFilteringOptions
        {
            EnableContentFiltering = true,
            FilterPrompts = true,
            UseMLFiltering = false, // Disable ML filtering for this test
            BlockedTerms = new List<string> { "badword" },
            BlockedPatterns = new List<string>()
        });
        var completionService = new Mock<ICompletionService>();
        var metricsService = new Mock<IMetricsService>();

        var contentFilteringService = new MLBasedContentFilteringService(
            logger.Object,
            options,
            completionService.Object,
            metricsService.Object);

        // Act
        var result = await contentFilteringService.FilterPromptAsync("This prompt contains a badword that should be blocked.");

        // Assert
        Assert.False(result.IsAllowed);
        Assert.NotNull(result.Reason);
        Assert.Contains("badword", result.Reason);
    }

    [Fact]
    public async Task MLBasedContentFilteringService_ShouldRespectDisabledFiltering()
    {
        // Arrange
        var logger = new Mock<ILogger<MLBasedContentFilteringService>>();
        var options = Options.Create(new ContentFilteringOptions
        {
            EnableContentFiltering = false, // Disabled
            FilterPrompts = true,
            UseMLFiltering = false,
            BlockedTerms = new List<string> { "badword" },
            BlockedPatterns = new List<string>()
        });
        var completionService = new Mock<ICompletionService>();
        var metricsService = new Mock<IMetricsService>();

        var contentFilteringService = new MLBasedContentFilteringService(
            logger.Object,
            options,
            completionService.Object,
            metricsService.Object);

        // Act
        var result = await contentFilteringService.FilterPromptAsync("This prompt contains a badword but filtering is disabled.");

        // Assert
        Assert.True(result.IsAllowed); // Should be allowed because filtering is disabled
    }
}
