using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Core.Options;
using LLMGateway.Core.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LLMGateway.Tests.Routing;

public class SmartModelRouterTests
{
    private readonly Mock<ILLMProviderFactory> _mockProviderFactory;
    private readonly Mock<IModelService> _mockModelService;
    private readonly Mock<IContentBasedRouter> _mockContentBasedRouter;
    private readonly Mock<ICostOptimizedRouter> _mockCostOptimizedRouter;
    private readonly Mock<ILatencyOptimizedRouter> _mockLatencyOptimizedRouter;
    private readonly Mock<ILogger<SmartModelRouter>> _mockLogger;
    private readonly Mock<IOptions<LLMRoutingOptions>> _mockRoutingOptions;
    private readonly Mock<IOptions<RoutingOptions>> _mockAdvancedRoutingOptions;
    private readonly Mock<IOptions<UserPreferencesOptions>> _mockUserPreferencesOptions;
    private readonly Mock<IOptions<FallbackOptions>> _mockFallbackOptions;
    private readonly SmartModelRouter _router;

    public SmartModelRouterTests()
    {
        _mockProviderFactory = new Mock<ILLMProviderFactory>();
        _mockModelService = new Mock<IModelService>();
        _mockContentBasedRouter = new Mock<IContentBasedRouter>();
        _mockCostOptimizedRouter = new Mock<ICostOptimizedRouter>();
        _mockLatencyOptimizedRouter = new Mock<ILatencyOptimizedRouter>();
        _mockLogger = new Mock<ILogger<SmartModelRouter>>();
        _mockRoutingOptions = new Mock<IOptions<LLMRoutingOptions>>();
        _mockAdvancedRoutingOptions = new Mock<IOptions<RoutingOptions>>();
        _mockUserPreferencesOptions = new Mock<IOptions<UserPreferencesOptions>>();
        _mockFallbackOptions = new Mock<IOptions<FallbackOptions>>();
        
        _mockRoutingOptions.Setup(x => x.Value).Returns(new LLMRoutingOptions
        {
            ModelMappings = new List<ModelMapping>
            {
                new ModelMapping
                {
                    ModelId = "gpt-4",
                    ProviderName = "OpenAI",
                    ProviderModelId = "gpt-4",
                    ContextWindow = 8192
                },
                new ModelMapping
                {
                    ModelId = "gpt-3.5-turbo",
                    ProviderName = "OpenAI",
                    ProviderModelId = "gpt-3.5-turbo",
                    ContextWindow = 16384
                }
            }
        });
        
        _mockAdvancedRoutingOptions.Setup(x => x.Value).Returns(new RoutingOptions
        {
            EnableSmartRouting = true,
            EnableContentBasedRouting = true,
            EnableCostOptimizedRouting = true,
            EnableLatencyOptimizedRouting = true,
            ModelMappings = new List<ModelMappingEntry>
            {
                new ModelMappingEntry
                {
                    ModelId = "gpt-4-alias",
                    TargetModelId = "gpt-4"
                }
            },
            ModelRoutingStrategies = new List<ModelRoutingStrategy>
            {
                new ModelRoutingStrategy
                {
                    ModelId = "gpt-4",
                    Strategy = "QualityOptimized"
                }
            }
        });
        
        _mockUserPreferencesOptions.Setup(x => x.Value).Returns(new UserPreferencesOptions
        {
            UserModelPreferences = new List<UserModelPreference>
            {
                new UserModelPreference
                {
                    UserId = "user1",
                    PreferredModelId = "gpt-4"
                }
            },
            UserRoutingPreferences = new List<UserRoutingPreference>
            {
                new UserRoutingPreference
                {
                    UserId = "user1",
                    RoutingStrategy = "CostOptimized"
                }
            }
        });
        
        _mockFallbackOptions.Setup(x => x.Value).Returns(new FallbackOptions
        {
            EnableFallbacks = true,
            MaxFallbackAttempts = 3,
            Rules = new List<FallbackRule>
            {
                new FallbackRule
                {
                    ModelId = "gpt-4",
                    FallbackModels = new List<string> { "gpt-3.5-turbo" },
                    ErrorCodes = new List<string> { "rate_limit_exceeded" }
                }
            }
        });
        
        _router = new SmartModelRouter(
            _mockProviderFactory.Object,
            _mockModelService.Object,
            _mockContentBasedRouter.Object,
            _mockCostOptimizedRouter.Object,
            _mockLatencyOptimizedRouter.Object,
            _mockRoutingOptions.Object,
            _mockAdvancedRoutingOptions.Object,
            _mockUserPreferencesOptions.Object,
            _mockFallbackOptions.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RouteCompletionRequestAsync_ShouldUseDirectMapping_WhenModelMappingExists()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "gpt-4",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };
        
        // Act
        var result = await _router.RouteCompletionRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("OpenAI", result.Provider);
        Assert.Equal("gpt-4", result.ModelId);
        Assert.Equal("gpt-4", result.ProviderModelId);
        Assert.Equal("DirectMapping", result.RoutingStrategy);
    }

    [Fact]
    public async Task RouteCompletionRequestAsync_ShouldMapModelAlias()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "gpt-4-alias",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };
        
        // Act
        var result = await _router.RouteCompletionRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("OpenAI", result.Provider);
        Assert.Equal("gpt-4", result.ModelId);
        Assert.Equal("gpt-4", result.ProviderModelId);
        Assert.Equal("DirectMapping", result.RoutingStrategy);
    }

    [Fact]
    public async Task RouteCompletionRequestAsync_ShouldUseUserPreferredModel()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "gpt-3.5-turbo",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            },
            User = "user1"
        };
        
        // Act
        var result = await _router.RouteCompletionRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("OpenAI", result.Provider);
        Assert.Equal("gpt-4", result.ModelId);
        Assert.Equal("gpt-4", result.ProviderModelId);
        Assert.Equal("DirectMapping", result.RoutingStrategy);
    }

    [Fact]
    public async Task RouteCompletionRequestAsync_ShouldUseCostOptimizedRouter()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "unknown-model",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            },
            User = "user1" // This user has CostOptimized strategy preference
        };
        
        var costOptimizedResult = new RoutingResult
        {
            Provider = "OpenAI",
            ModelId = "gpt-3.5-turbo",
            ProviderModelId = "gpt-3.5-turbo",
            RoutingStrategy = "CostOptimized",
            Success = true
        };
        
        _mockCostOptimizedRouter
            .Setup(x => x.RouteRequestAsync(It.IsAny<CompletionRequest>()))
            .ReturnsAsync(costOptimizedResult);
        
        // Act
        var result = await _router.RouteCompletionRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("OpenAI", result.Provider);
        Assert.Equal("gpt-3.5-turbo", result.ModelId);
        Assert.Equal("gpt-3.5-turbo", result.ProviderModelId);
        Assert.Equal("CostOptimized", result.RoutingStrategy);
        
        _mockCostOptimizedRouter.Verify(x => x.RouteRequestAsync(It.IsAny<CompletionRequest>()), Times.Once);
    }

    [Fact]
    public async Task RouteCompletionRequestAsync_ShouldUseContentBasedRouter()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "unknown-model",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Write a function to calculate factorial" }
            }
        };
        
        // Override the user preferences options to not have a strategy for this user
        _mockUserPreferencesOptions.Setup(x => x.Value).Returns(new UserPreferencesOptions
        {
            UserModelPreferences = new List<UserModelPreference>(),
            UserRoutingPreferences = new List<UserRoutingPreference>()
        });
        
        // Override the model routing strategies to use ContentBased for unknown models
        _mockAdvancedRoutingOptions.Setup(x => x.Value).Returns(new RoutingOptions
        {
            EnableSmartRouting = true,
            EnableContentBasedRouting = true,
            ModelRoutingStrategies = new List<ModelRoutingStrategy>
            {
                new ModelRoutingStrategy
                {
                    ModelId = "unknown-model",
                    Strategy = "ContentBased"
                }
            }
        });
        
        var contentBasedResult = new RoutingResult
        {
            Provider = "OpenAI",
            ModelId = "gpt-4",
            ProviderModelId = "gpt-4",
            RoutingStrategy = "ContentBased",
            Success = true
        };
        
        _mockContentBasedRouter
            .Setup(x => x.RouteRequestAsync(It.IsAny<CompletionRequest>()))
            .ReturnsAsync(contentBasedResult);
        
        // Act
        var result = await _router.RouteCompletionRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("OpenAI", result.Provider);
        Assert.Equal("gpt-4", result.ModelId);
        Assert.Equal("gpt-4", result.ProviderModelId);
        Assert.Equal("ContentBased", result.RoutingStrategy);
        
        _mockContentBasedRouter.Verify(x => x.RouteRequestAsync(It.IsAny<CompletionRequest>()), Times.Once);
    }

    [Fact]
    public async Task RouteEmbeddingRequestAsync_ShouldUseDirectMapping()
    {
        // Arrange
        var request = new EmbeddingRequest
        {
            ModelId = "gpt-4",
            Input = "Hello"
        };
        
        // Act
        var result = await _router.RouteEmbeddingRequestAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("OpenAI", result.Provider);
        Assert.Equal("gpt-4", result.ModelId);
        Assert.Equal("gpt-4", result.ProviderModelId);
        Assert.Equal("DirectMapping", result.RoutingStrategy);
    }

    [Fact]
    public async Task GetFallbackModelsAsync_ShouldReturnFallbackModels()
    {
        // Act
        var result = await _router.GetFallbackModelsAsync("gpt-4", "rate_limit_exceeded");
        
        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("gpt-3.5-turbo", result);
    }

    [Fact]
    public async Task GetFallbackModelsAsync_ShouldReturnEmptyList_WhenNoRuleExists()
    {
        // Act
        var result = await _router.GetFallbackModelsAsync("unknown-model");
        
        // Assert
        Assert.Empty(result);
    }
}
