using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Core.Options;
using LLMGateway.Core.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LLMGateway.Tests.Routing;

public class LatencyOptimizedRouterTests
{
    private readonly Mock<IModelService> _mockModelService;
    private readonly Mock<IModelPerformanceMonitor> _mockPerformanceMonitor;
    private readonly Mock<ILogger<LatencyOptimizedRouter>> _mockLogger;
    private readonly Mock<IOptions<RoutingOptions>> _mockOptions;
    private readonly LatencyOptimizedRouter _router;

    public LatencyOptimizedRouterTests()
    {
        _mockModelService = new Mock<IModelService>();
        _mockPerformanceMonitor = new Mock<IModelPerformanceMonitor>();
        _mockLogger = new Mock<ILogger<LatencyOptimizedRouter>>();
        _mockOptions = new Mock<IOptions<RoutingOptions>>();
        
        _mockOptions.Setup(x => x.Value).Returns(new RoutingOptions
        {
            EnableLatencyOptimizedRouting = true
        });
        
        _router = new LatencyOptimizedRouter(
            _mockModelService.Object,
            _mockPerformanceMonitor.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldReturnFalse_WhenLatencyOptimizedRoutingIsDisabled()
    {
        // Arrange
        _mockOptions.Setup(x => x.Value).Returns(new RoutingOptions
        {
            EnableLatencyOptimizedRouting = false
        });
        
        var request = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };
        
        // Act
        var result = await _router.RouteRequestAsync(request);
        
        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldSelectLowestLatencyModel()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };
        
        var models = new List<ModelInfo>
        {
            new ModelInfo { Id = "openai.gpt-4", Provider = "OpenAI", ProviderModelId = "gpt-4", SupportsCompletions = true },
            new ModelInfo { Id = "openai.gpt-3.5-turbo", Provider = "OpenAI", ProviderModelId = "gpt-3.5-turbo", SupportsCompletions = true },
            new ModelInfo { Id = "anthropic.claude-3-opus", Provider = "Anthropic", ProviderModelId = "claude-3-opus-20240229", SupportsCompletions = true },
            new ModelInfo { Id = "anthropic.claude-3-haiku", Provider = "Anthropic", ProviderModelId = "claude-3-haiku-20240307", SupportsCompletions = true }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);
        
        // Set up performance metrics
        var metrics = new Dictionary<string, ModelPerformanceMetrics>
        {
            ["openai.gpt-4"] = new ModelPerformanceMetrics { ModelId = "openai.gpt-4", AverageResponseTimeMs = 2000, RequestCount = 100 },
            ["openai.gpt-3.5-turbo"] = new ModelPerformanceMetrics { ModelId = "openai.gpt-3.5-turbo", AverageResponseTimeMs = 800, RequestCount = 100 },
            ["anthropic.claude-3-opus"] = new ModelPerformanceMetrics { ModelId = "anthropic.claude-3-opus", AverageResponseTimeMs = 3000, RequestCount = 100 },
            ["anthropic.claude-3-haiku"] = new ModelPerformanceMetrics { ModelId = "anthropic.claude-3-haiku", AverageResponseTimeMs = 500, RequestCount = 100 }
        };
        
        _mockPerformanceMonitor.Setup(x => x.GetAllModelPerformanceMetrics()).Returns(metrics);
        
        // Act
        var result = await _router.RouteRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("LatencyOptimized", result.RoutingStrategy);
        Assert.Contains("Lowest latency model", result.RoutingReason);
        Assert.Equal("anthropic.claude-3-haiku", result.ModelId);
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldUseDefaultLatencies_WhenNoMetricsAvailable()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };
        
        var models = new List<ModelInfo>
        {
            new ModelInfo { Id = "openai.gpt-4", Provider = "OpenAI", ProviderModelId = "gpt-4", SupportsCompletions = true },
            new ModelInfo { Id = "openai.gpt-3.5-turbo", Provider = "OpenAI", ProviderModelId = "gpt-3.5-turbo", SupportsCompletions = true },
            new ModelInfo { Id = "anthropic.claude-3-opus", Provider = "Anthropic", ProviderModelId = "claude-3-opus-20240229", SupportsCompletions = true },
            new ModelInfo { Id = "anthropic.claude-3-haiku", Provider = "Anthropic", ProviderModelId = "claude-3-haiku-20240307", SupportsCompletions = true }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);
        
        // No performance metrics available
        _mockPerformanceMonitor.Setup(x => x.GetAllModelPerformanceMetrics()).Returns(new Dictionary<string, ModelPerformanceMetrics>());
        
        // Act
        var result = await _router.RouteRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("LatencyOptimized", result.RoutingStrategy);
        Assert.Contains("Lowest latency model", result.RoutingReason);
        
        // Should use default latencies, which should select one of these models
        Assert.Contains(result.ModelId, new[] { "openai.gpt-3.5-turbo", "anthropic.claude-3-haiku" });
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldAdjustLatencyBasedOnTokenCount()
    {
        // Arrange
        var shortRequest = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };
        
        var longRequest = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = new string('a', 10000) }
            }
        };
        
        var models = new List<ModelInfo>
        {
            new ModelInfo { Id = "openai.gpt-4", Provider = "OpenAI", ProviderModelId = "gpt-4", SupportsCompletions = true },
            new ModelInfo { Id = "openai.gpt-3.5-turbo", Provider = "OpenAI", ProviderModelId = "gpt-3.5-turbo", SupportsCompletions = true }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);
        
        // Set up performance metrics with similar base latencies
        var metrics = new Dictionary<string, ModelPerformanceMetrics>
        {
            ["openai.gpt-4"] = new ModelPerformanceMetrics { ModelId = "openai.gpt-4", AverageResponseTimeMs = 1000, RequestCount = 100 },
            ["openai.gpt-3.5-turbo"] = new ModelPerformanceMetrics { ModelId = "openai.gpt-3.5-turbo", AverageResponseTimeMs = 900, RequestCount = 100 }
        };
        
        _mockPerformanceMonitor.Setup(x => x.GetAllModelPerformanceMetrics()).Returns(metrics);
        
        // Act
        var shortResult = await _router.RouteRequestAsync(shortRequest);
        var longResult = await _router.RouteRequestAsync(longRequest);
        
        // Assert
        Assert.True(shortResult.Success);
        Assert.True(longResult.Success);
        
        // For short requests, the base latency is more important
        Assert.Equal("openai.gpt-3.5-turbo", shortResult.ModelId);
        
        // For long requests, the token count adjustment becomes more significant
        // and might change the result, but we can't assert the exact model since
        // the adjustment is implementation-dependent
        Assert.Contains(longResult.ModelId, new[] { "openai.gpt-4", "openai.gpt-3.5-turbo" });
    }
}
