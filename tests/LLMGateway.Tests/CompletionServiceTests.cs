using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Options;
using LLMGateway.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LLMGateway.Tests;

public class CompletionServiceTests
{
    private readonly Mock<ILLMProviderFactory> _mockProviderFactory;
    private readonly Mock<IModelRouter> _mockModelRouter;
    private readonly Mock<ITokenUsageService> _mockTokenUsageService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<CompletionService>> _mockLogger;
    private readonly Mock<IOptions<GlobalOptions>> _mockGlobalOptions;
    private readonly Mock<IOptions<FallbackOptions>> _mockFallbackOptions;
    private readonly CompletionService _service;

    public CompletionServiceTests()
    {
        _mockProviderFactory = new Mock<ILLMProviderFactory>();
        _mockModelRouter = new Mock<IModelRouter>();
        _mockTokenUsageService = new Mock<ITokenUsageService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CompletionService>>();
        _mockGlobalOptions = new Mock<IOptions<GlobalOptions>>();
        _mockFallbackOptions = new Mock<IOptions<FallbackOptions>>();

        _mockGlobalOptions.Setup(x => x.Value).Returns(new GlobalOptions
        {
            EnableCaching = true,
            TrackTokenUsage = true
        });

        _mockFallbackOptions.Setup(x => x.Value).Returns(new FallbackOptions
        {
            EnableFallbacks = true,
            MaxFallbackAttempts = 3
        });

        _service = new CompletionService(
            _mockProviderFactory.Object,
            _mockModelRouter.Object,
            _mockTokenUsageService.Object,
            _mockCacheService.Object,
            _mockGlobalOptions.Object,
            _mockFallbackOptions.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateCompletionAsync_ShouldReturnCachedResponse_WhenCacheHit()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "test-model",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            },
            Temperature = 0.0
        };

        var cachedResponse = new CompletionResponse
        {
            Id = "cached-response-id",
            Model = "test-model",
            Provider = "test-provider",
            Choices = new List<CompletionChoice>
            {
                new CompletionChoice
                {
                    Message = new Message { Role = "assistant", Content = "Hello from cache" }
                }
            }
        };

        _mockCacheService
            .Setup(x => x.GetAsync<CompletionResponse>(It.IsAny<string>()))
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await _service.CreateCompletionAsync(request);

        // Assert
        Assert.Equal(cachedResponse, result);
        _mockModelRouter.Verify(x => x.RouteCompletionRequestAsync(It.IsAny<CompletionRequest>()), Times.Never);
        _mockProviderFactory.Verify(x => x.GetProvider(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateCompletionAsync_ShouldRouteAndCallProvider_WhenCacheMiss()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "test-model",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };

        var routingResult = new RoutingResult
        {
            Provider = "test-provider",
            ModelId = "test-model",
            ProviderModelId = "provider-model-id",
            RoutingStrategy = "test-strategy",
            Success = true
        };

        var providerResponse = new CompletionResponse
        {
            Id = "response-id",
            Model = "provider-model-id",
            Provider = "test-provider",
            Choices = new List<CompletionChoice>
            {
                new CompletionChoice
                {
                    Message = new Message { Role = "assistant", Content = "Hello from provider" }
                }
            },
            Usage = new CompletionUsage
            {
                PromptTokens = 10,
                CompletionTokens = 20,
                TotalTokens = 30
            }
        };

        var mockProvider = new Mock<ILLMProvider>();
        mockProvider
            .Setup(x => x.CreateCompletionAsync(It.IsAny<CompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResponse);

        _mockCacheService
            .Setup(x => x.GetAsync<CompletionResponse>(It.IsAny<string>()))
            .ReturnsAsync((CompletionResponse)null);

        _mockModelRouter
            .Setup(x => x.RouteCompletionRequestAsync(It.IsAny<CompletionRequest>()))
            .ReturnsAsync(routingResult);

        _mockProviderFactory
            .Setup(x => x.GetProvider(routingResult.Provider))
            .Returns(mockProvider.Object);

        // Act
        var result = await _service.CreateCompletionAsync(request);

        // Assert
        Assert.Equal("test-model", result.Model);
        Assert.Equal("test-provider", result.Provider);
        Assert.Equal("Hello from provider", result.Choices[0].Message.Content);
        
        _mockModelRouter.Verify(x => x.RouteCompletionRequestAsync(It.IsAny<CompletionRequest>()), Times.Once);
        _mockProviderFactory.Verify(x => x.GetProvider(routingResult.Provider), Times.Once);
        mockProvider.Verify(x => x.CreateCompletionAsync(It.Is<CompletionRequest>(r => r.ModelId == "provider-model-id"), It.IsAny<CancellationToken>()), Times.Once);
        _mockTokenUsageService.Verify(x => x.TrackUsageAsync(It.IsAny<Core.Models.TokenUsage.TokenUsageRecord>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<CompletionResponse>(), It.IsAny<TimeSpan?>()), Times.Once);
    }
}
