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

public class CostOptimizedRouterTests
{
    private readonly Mock<IModelService> _mockModelService;
    private readonly Mock<ILogger<CostOptimizedRouter>> _mockLogger;
    private readonly Mock<IOptions<RoutingOptions>> _mockOptions;
    private readonly CostOptimizedRouter _router;

    public CostOptimizedRouterTests()
    {
        _mockModelService = new Mock<IModelService>();
        _mockLogger = new Mock<ILogger<CostOptimizedRouter>>();
        _mockOptions = new Mock<IOptions<RoutingOptions>>();
        
        _mockOptions.Setup(x => x.Value).Returns(new RoutingOptions
        {
            EnableCostOptimizedRouting = true
        });
        
        _router = new CostOptimizedRouter(_mockModelService.Object, _mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldReturnFalse_WhenCostOptimizedRoutingIsDisabled()
    {
        // Arrange
        _mockOptions.Setup(x => x.Value).Returns(new RoutingOptions
        {
            EnableCostOptimizedRouting = false
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
    public async Task RouteRequestAsync_ShouldSelectLowestCostModel()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            },
            MaxTokens = 1000
        };
        
        var models = new List<ModelInfo>
        {
            new ModelInfo { Id = "openai.gpt-4", Provider = "OpenAI", ProviderModelId = "gpt-4", SupportsCompletions = true },
            new ModelInfo { Id = "openai.gpt-3.5-turbo", Provider = "OpenAI", ProviderModelId = "gpt-3.5-turbo", SupportsCompletions = true },
            new ModelInfo { Id = "anthropic.claude-3-opus", Provider = "Anthropic", ProviderModelId = "claude-3-opus-20240229", SupportsCompletions = true },
            new ModelInfo { Id = "anthropic.claude-3-haiku", Provider = "Anthropic", ProviderModelId = "claude-3-haiku-20240307", SupportsCompletions = true }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);
        
        // Act
        var result = await _router.RouteRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("CostOptimized", result.RoutingStrategy);
        Assert.Contains("Lowest cost model", result.RoutingReason);
        
        // The lowest cost model should be one of these
        Assert.Contains(result.ModelId, new[] { "openai.gpt-3.5-turbo", "anthropic.claude-3-haiku" });
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldConsiderInputAndOutputTokens()
    {
        // Arrange
        var shortRequest = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            },
            MaxTokens = 100
        };
        
        var longRequest = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = new string('a', 10000) }
            },
            MaxTokens = 5000
        };
        
        var models = new List<ModelInfo>
        {
            new ModelInfo { Id = "openai.gpt-4", Provider = "OpenAI", ProviderModelId = "gpt-4", SupportsCompletions = true },
            new ModelInfo { Id = "openai.gpt-3.5-turbo", Provider = "OpenAI", ProviderModelId = "gpt-3.5-turbo", SupportsCompletions = true },
            new ModelInfo { Id = "anthropic.claude-3-opus", Provider = "Anthropic", ProviderModelId = "claude-3-opus-20240229", SupportsCompletions = true },
            new ModelInfo { Id = "anthropic.claude-3-haiku", Provider = "Anthropic", ProviderModelId = "claude-3-haiku-20240307", SupportsCompletions = true }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);
        
        // Act
        var shortResult = await _router.RouteRequestAsync(shortRequest);
        var longResult = await _router.RouteRequestAsync(longRequest);
        
        // Assert
        Assert.True(shortResult.Success);
        Assert.True(longResult.Success);
        
        // For short requests, the input cost is less significant
        Assert.Contains(shortResult.ModelId, new[] { "openai.gpt-3.5-turbo", "anthropic.claude-3-haiku" });
        
        // For long requests, the input cost becomes more significant
        Assert.Contains(longResult.ModelId, new[] { "openai.gpt-3.5-turbo", "anthropic.claude-3-haiku" });
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldReturnFalse_WhenNoModelsAvailable()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(new List<ModelInfo>());
        
        // Act
        var result = await _router.RouteRequestAsync(request);
        
        // Assert
        Assert.False(result.Success);
    }
}
