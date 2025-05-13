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

public class ContentBasedRouterTests
{
    private readonly Mock<IModelService> _mockModelService;
    private readonly Mock<ILogger<ContentBasedRouter>> _mockLogger;
    private readonly Mock<IOptions<RoutingOptions>> _mockOptions;
    private readonly ContentBasedRouter _router;

    public ContentBasedRouterTests()
    {
        _mockModelService = new Mock<IModelService>();
        _mockLogger = new Mock<ILogger<ContentBasedRouter>>();
        _mockOptions = new Mock<IOptions<RoutingOptions>>();
        
        _mockOptions.Setup(x => x.Value).Returns(new RoutingOptions
        {
            EnableContentBasedRouting = true
        });
        
        _router = new ContentBasedRouter(_mockModelService.Object, _mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldReturnFalse_WhenContentBasedRoutingIsDisabled()
    {
        // Arrange
        _mockOptions.Setup(x => x.Value).Returns(new RoutingOptions
        {
            EnableContentBasedRouting = false
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
    public async Task RouteRequestAsync_ShouldDetectCodeContent()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Write a function to calculate the factorial of a number in Python:\n```python\ndef factorial(n):\n    pass\n```" }
            }
        };
        
        var models = new List<ModelInfo>
        {
            new ModelInfo { Id = "openai.gpt-4-turbo", Provider = "OpenAI", ProviderModelId = "gpt-4-turbo" },
            new ModelInfo { Id = "anthropic.claude-3-opus", Provider = "Anthropic", ProviderModelId = "claude-3-opus-20240229" }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);
        
        // Act
        var result = await _router.RouteRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("Code content detected", result.RoutingReason);
        Assert.Contains(result.ModelId, new[] { "openai.gpt-4-turbo", "anthropic.claude-3-opus" });
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldDetectMathContent()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Solve this integral: $\\int_{0}^{\\pi} \\sin(x) dx$" }
            }
        };
        
        var models = new List<ModelInfo>
        {
            new ModelInfo { Id = "anthropic.claude-3-opus", Provider = "Anthropic", ProviderModelId = "claude-3-opus-20240229" },
            new ModelInfo { Id = "openai.gpt-4-turbo", Provider = "OpenAI", ProviderModelId = "gpt-4-turbo" }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);
        
        // Act
        var result = await _router.RouteRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("Mathematical content detected", result.RoutingReason);
        Assert.Contains(result.ModelId, new[] { "anthropic.claude-3-opus", "openai.gpt-4-turbo" });
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldDetectCreativeContent()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Write a short story about a robot who learns to love." }
            }
        };
        
        var models = new List<ModelInfo>
        {
            new ModelInfo { Id = "anthropic.claude-3-opus", Provider = "Anthropic", ProviderModelId = "claude-3-opus-20240229" },
            new ModelInfo { Id = "anthropic.claude-3-sonnet", Provider = "Anthropic", ProviderModelId = "claude-3-sonnet-20240229" }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);
        
        // Act
        var result = await _router.RouteRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("Creative writing content detected", result.RoutingReason);
        Assert.Contains(result.ModelId, new[] { "anthropic.claude-3-opus", "anthropic.claude-3-sonnet" });
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldDetectAnalyticalContent()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Analyze the impact of artificial intelligence on the job market in the next decade." }
            }
        };
        
        var models = new List<ModelInfo>
        {
            new ModelInfo { Id = "anthropic.claude-3-opus", Provider = "Anthropic", ProviderModelId = "claude-3-opus-20240229" },
            new ModelInfo { Id = "openai.gpt-4-turbo", Provider = "OpenAI", ProviderModelId = "gpt-4-turbo" }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);
        
        // Act
        var result = await _router.RouteRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("Analytical content detected", result.RoutingReason);
        Assert.Contains(result.ModelId, new[] { "anthropic.claude-3-opus", "openai.gpt-4-turbo" });
    }

    [Fact]
    public async Task RouteRequestAsync_ShouldDetectLongFormContent()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Write a comprehensive essay on the history of artificial intelligence." }
            }
        };
        
        var models = new List<ModelInfo>
        {
            new ModelInfo { Id = "anthropic.claude-3-opus", Provider = "Anthropic", ProviderModelId = "claude-3-opus-20240229", ContextWindow = 200000 },
            new ModelInfo { Id = "openai.gpt-4-turbo", Provider = "OpenAI", ProviderModelId = "gpt-4-turbo", ContextWindow = 128000 }
        };
        
        _mockModelService.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);
        
        // Act
        var result = await _router.RouteRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("Long-form content detected", result.RoutingReason);
        Assert.Equal("anthropic.claude-3-opus", result.ModelId);
    }
}
