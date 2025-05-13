using LLMGateway.Core.Models.Completion;
using LLMGateway.Providers.Anthropic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace LLMGateway.Tests.Providers;

public class AnthropicProviderTests
{
    private readonly Mock<IOptions<AnthropicOptions>> _mockOptions;
    private readonly Mock<ILogger<AnthropicProvider>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly HttpClient _httpClient;
    private readonly AnthropicProvider _provider;

    public AnthropicProviderTests()
    {
        _mockOptions = new Mock<IOptions<AnthropicOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(new AnthropicOptions
        {
            ApiKey = "test-api-key",
            ApiUrl = "https://api.anthropic.com",
            TimeoutSeconds = 30,
            ApiVersion = "2023-06-01"
        });
        
        _mockLogger = new Mock<ILogger<AnthropicProvider>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        
        _provider = new AnthropicProvider(_httpClient, _mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetModelsAsync_ShouldReturnModels()
    {
        // Act
        var result = await _provider.GetModelsAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, m => m.Id == "anthropic.claude-3-opus");
        Assert.Contains(result, m => m.Id == "anthropic.claude-3-sonnet");
        Assert.Contains(result, m => m.Id == "anthropic.claude-3-haiku");
    }

    [Fact]
    public async Task GetModelAsync_ShouldReturnModel_WhenModelExists()
    {
        // Arrange
        var modelId = "anthropic.claude-3-opus";
        
        // Act
        var result = await _provider.GetModelAsync(modelId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(modelId, result.Id);
        Assert.Equal("Anthropic", result.Provider);
        Assert.Equal("claude-3-opus-20240229", result.ProviderModelId);
    }

    [Fact]
    public async Task CreateCompletionAsync_ShouldReturnCompletion()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "claude-3-opus-20240229",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };
        
        var response = new AnthropicMessageResponse
        {
            Id = "msg_123",
            Model = "claude-3-opus-20240229",
            Role = "assistant",
            Content = new List<AnthropicContentBlock>
            {
                new AnthropicContentBlock { Type = "text", Text = "Hello! How can I help you today?" }
            },
            Usage = new AnthropicUsage
            {
                InputTokens = 10,
                OutputTokens = 20
            }
        };
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
        
        // Act
        var result = await _provider.CreateCompletionAsync(request);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("msg_123", result.Id);
        Assert.Equal("claude-3-opus-20240229", result.Model);
        Assert.Equal("Anthropic", result.Provider);
        Assert.NotEmpty(result.Choices);
        Assert.Equal("Hello! How can I help you today?", result.Choices[0].Message.Content);
        Assert.Equal(10, result.Usage.PromptTokens);
        Assert.Equal(20, result.Usage.CompletionTokens);
        Assert.Equal(30, result.Usage.TotalTokens);
    }

    [Fact]
    public async Task IsAvailableAsync_ShouldReturnTrue_WhenApiIsAvailable()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });
        
        // Act
        var result = await _provider.IsAvailableAsync();
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_ShouldReturnFalse_WhenApiIsUnavailable()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
        
        // Act
        var result = await _provider.IsAvailableAsync();
        
        // Assert
        Assert.False(result);
    }
}
