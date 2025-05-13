using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Providers.HuggingFace;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace LLMGateway.Tests.Providers;

public class HuggingFaceProviderTests
{
    private readonly Mock<IOptions<HuggingFaceOptions>> _mockOptions;
    private readonly Mock<ILogger<HuggingFaceProvider>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly HuggingFaceProvider _provider;

    public HuggingFaceProviderTests()
    {
        _mockOptions = new Mock<IOptions<HuggingFaceOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(new HuggingFaceOptions
        {
            ApiKey = "test-api-key",
            ApiUrl = "https://api-inference.huggingface.co/models",
            TimeoutSeconds = 60
        });
        
        _mockLogger = new Mock<ILogger<HuggingFaceProvider>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        
        _provider = new HuggingFaceProvider(_httpClient, _mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetModelsAsync_ShouldReturnModels()
    {
        // Act
        var result = await _provider.GetModelsAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, m => m.Id.Contains("mistralai"));
        Assert.Contains(result, m => m.Id.Contains("meta-llama"));
    }

    [Fact]
    public async Task GetModelAsync_ShouldReturnModel_WhenModelExists()
    {
        // Arrange
        var modelId = "mistralai/Mistral-7B-Instruct-v0.2";
        
        // Act
        var result = await _provider.GetModelAsync(modelId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("huggingface.mistralai_Mistral-7B-Instruct-v0.2", result.Id);
        Assert.Equal("HuggingFace", result.Provider);
        Assert.Equal(modelId, result.ProviderModelId);
    }

    [Fact]
    public async Task CreateCompletionAsync_ShouldReturnCompletion()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "mistralai/Mistral-7B-Instruct-v0.2",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };
        
        var response = new HuggingFaceTextGenerationResponse[]
        {
            new HuggingFaceTextGenerationResponse
            {
                GeneratedText = "Hello! How can I help you today?",
                GeneratedTokens = 20,
                Details = new HuggingFaceTextGenerationDetails
                {
                    FinishReason = "stop",
                    PromptTokens = 10,
                    GeneratedTokens = 20
                }
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
        Assert.NotEmpty(result.Id);
        Assert.Equal("mistralai/Mistral-7B-Instruct-v0.2", result.Model);
        Assert.Equal("HuggingFace", result.Provider);
        Assert.NotEmpty(result.Choices);
        Assert.Equal("Hello! How can I help you today?", result.Choices[0].Message.Content);
        Assert.Equal(10, result.Usage.PromptTokens);
        Assert.Equal(20, result.Usage.CompletionTokens);
        Assert.Equal(30, result.Usage.TotalTokens);
    }

    [Fact]
    public async Task CreateEmbeddingAsync_ShouldReturnEmbedding()
    {
        // Arrange
        var request = new EmbeddingRequest
        {
            ModelId = "sentence-transformers/all-MiniLM-L6-v2",
            Input = "Hello, world!"
        };
        
        var response = new List<List<float>>
        {
            new List<float> { 0.1f, 0.2f, 0.3f }
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
        var result = await _provider.CreateEmbeddingAsync(request);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("list", result.Object);
        Assert.Equal("sentence-transformers/all-MiniLM-L6-v2", result.Model);
        Assert.Equal("HuggingFace", result.Provider);
        Assert.NotEmpty(result.Data);
        Assert.Equal(0, result.Data[0].Index);
        Assert.Equal(3, result.Data[0].Embedding.Count);
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
