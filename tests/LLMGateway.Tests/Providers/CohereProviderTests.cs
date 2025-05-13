using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Providers.Cohere;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace LLMGateway.Tests.Providers;

public class CohereProviderTests
{
    private readonly Mock<IOptions<CohereOptions>> _mockOptions;
    private readonly Mock<ILogger<CohereProvider>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly CohereProvider _provider;

    public CohereProviderTests()
    {
        _mockOptions = new Mock<IOptions<CohereOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(new CohereOptions
        {
            ApiKey = "test-api-key",
            ApiUrl = "https://api.cohere.ai",
            TimeoutSeconds = 30
        });
        
        _mockLogger = new Mock<ILogger<CohereProvider>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        
        _provider = new CohereProvider(_httpClient, _mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetModelsAsync_ShouldReturnModels()
    {
        // Act
        var result = await _provider.GetModelsAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, m => m.Id == "cohere.command-r");
        Assert.Contains(result, m => m.Id == "cohere.command-r-plus");
        Assert.Contains(result, m => m.Id == "cohere.command-light");
    }

    [Fact]
    public async Task GetModelAsync_ShouldReturnModel_WhenModelExists()
    {
        // Arrange
        var modelId = "cohere.command-r";
        
        // Act
        var result = await _provider.GetModelAsync(modelId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(modelId, result.Id);
        Assert.Equal("Cohere", result.Provider);
        Assert.Equal("command-r", result.ProviderModelId);
    }

    [Fact]
    public async Task CreateCompletionAsync_ShouldReturnCompletion()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "command-r",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            }
        };
        
        var response = new CohereChatResponse
        {
            Text = "Hello! How can I help you today?",
            GenerationId = "gen_123",
            FinishReason = "COMPLETE",
            TokenCount = new CohereTokenCount
            {
                PromptTokens = 10,
                ResponseTokens = 20,
                TotalTokens = 30
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
        Assert.Equal("gen_123", result.Id);
        Assert.Equal("command-r", result.Model);
        Assert.Equal("Cohere", result.Provider);
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
            ModelId = "embed-english-v3.0",
            Input = "Hello, world!"
        };
        
        var response = new CohereEmbeddingResponse
        {
            Id = "emb_123",
            Embeddings = new List<List<float>> { new List<float> { 0.1f, 0.2f, 0.3f } },
            Meta = new CohereMeta
            {
                BilledUnits = new CohereBilledUnits
                {
                    InputTokens = 5,
                    OutputTokens = 0
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
        var result = await _provider.CreateEmbeddingAsync(request);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("list", result.Object);
        Assert.Equal("embed-english-v3.0", result.Model);
        Assert.Equal("Cohere", result.Provider);
        Assert.NotEmpty(result.Data);
        Assert.Equal(0, result.Data[0].Index);
        Assert.Equal(3, result.Data[0].Embedding.Count);
        Assert.Equal(5, result.Usage.PromptTokens);
        Assert.Equal(5, result.Usage.TotalTokens);
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
