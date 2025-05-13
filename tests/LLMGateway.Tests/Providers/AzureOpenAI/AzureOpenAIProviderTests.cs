using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Providers.AzureOpenAI;
using LLMGateway.Providers.AzureOpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace LLMGateway.Tests.Providers.AzureOpenAI;

public class AzureOpenAIProviderTests
{
    private readonly Mock<ILogger<AzureOpenAIProvider>> _loggerMock;
    private readonly Mock<IOptions<AzureOpenAIOptions>> _optionsMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAIOptions _options;
    private readonly AzureOpenAIProvider _provider;

    public AzureOpenAIProviderTests()
    {
        _loggerMock = new Mock<ILogger<AzureOpenAIProvider>>();
        
        _options = new AzureOpenAIOptions
        {
            ApiKey = "test-api-key",
            Endpoint = "https://test-resource.openai.azure.com",
            ApiVersion = "2023-05-15",
            TimeoutSeconds = 30,
            StreamTimeoutSeconds = 120,
            Deployments = new List<AzureOpenAIDeployment>
            {
                new AzureOpenAIDeployment
                {
                    DeploymentId = "gpt-4",
                    DisplayName = "GPT-4",
                    ModelName = "gpt-4",
                    Type = AzureOpenAIDeploymentType.Completion
                },
                new AzureOpenAIDeployment
                {
                    DeploymentId = "text-embedding-ada-002",
                    DisplayName = "Text Embedding Ada 002",
                    ModelName = "text-embedding-ada-002",
                    Type = AzureOpenAIDeploymentType.Embedding
                }
            }
        };
        
        _optionsMock = new Mock<IOptions<AzureOpenAIOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_options);
        
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);
        
        _provider = new AzureOpenAIProvider(
            _httpClient,
            _httpClientFactoryMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Name_ShouldReturnAzureOpenAI()
    {
        // Act
        var name = _provider.Name;

        // Assert
        Assert.Equal("AzureOpenAI", name);
    }

    [Fact]
    public async Task GetModelsAsync_ShouldReturnConfiguredDeployments()
    {
        // Act
        var models = await _provider.GetModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.Equal(2, models.Count());
        
        var completionModel = models.First(m => m.Id == "azure-openai.gpt-4");
        Assert.Equal("GPT-4", completionModel.DisplayName);
        Assert.Equal("AzureOpenAI", completionModel.Provider);
        Assert.Equal("gpt-4", completionModel.ProviderModelId);
        Assert.True(completionModel.SupportsCompletions);
        Assert.False(completionModel.SupportsEmbeddings);
        
        var embeddingModel = models.First(m => m.Id == "azure-openai.text-embedding-ada-002");
        Assert.Equal("Text Embedding Ada 002", embeddingModel.DisplayName);
        Assert.Equal("AzureOpenAI", embeddingModel.Provider);
        Assert.Equal("text-embedding-ada-002", embeddingModel.ProviderModelId);
        Assert.False(embeddingModel.SupportsCompletions);
        Assert.True(embeddingModel.SupportsEmbeddings);
    }

    [Fact]
    public async Task GetModelAsync_WithValidModelId_ShouldReturnModel()
    {
        // Act
        var model = await _provider.GetModelAsync("azure-openai.gpt-4");

        // Assert
        Assert.NotNull(model);
        Assert.Equal("azure-openai.gpt-4", model.Id);
        Assert.Equal("GPT-4", model.DisplayName);
        Assert.Equal("AzureOpenAI", model.Provider);
        Assert.Equal("gpt-4", model.ProviderModelId);
    }

    [Fact]
    public async Task CreateCompletionAsync_ShouldCallAzureOpenAIAPI()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "azure-openai.gpt-4",
            Messages = new List<Message>
            {
                new Message { Role = "system", Content = "You are a helpful assistant." },
                new Message { Role = "user", Content = "Hello!" }
            }
        };

        var responseContent = new AzureOpenAIChatCompletionResponse
        {
            Id = "test-id",
            Object = "chat.completion",
            Created = 1677858242,
            Model = "gpt-4",
            Choices = new List<AzureOpenAIChatCompletionChoice>
            {
                new AzureOpenAIChatCompletionChoice
                {
                    Index = 0,
                    Message = new AzureOpenAIChatMessage
                    {
                        Role = "assistant",
                        Content = "Hello! How can I help you today?"
                    },
                    FinishReason = "stop"
                }
            },
            Usage = new AzureOpenAIChatCompletionUsage
            {
                PromptTokens = 10,
                CompletionTokens = 8,
                TotalTokens = 18
            }
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(responseContent),
                    Encoding.UTF8,
                    "application/json")
            });

        // Act
        var response = await _provider.CreateCompletionAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("test-id", response.Id);
        Assert.Equal("AzureOpenAI", response.Provider);
        Assert.Equal("gpt-4", response.Model);
        Assert.Single(response.Choices);
        Assert.Equal("assistant", response.Choices[0].Message.Role);
        Assert.Equal("Hello! How can I help you today?", response.Choices[0].Message.Content);
        Assert.Equal(10, response.Usage.PromptTokens);
        Assert.Equal(8, response.Usage.CompletionTokens);
        Assert.Equal(18, response.Usage.TotalTokens);
    }

    [Fact]
    public async Task CreateEmbeddingAsync_ShouldCallAzureOpenAIAPI()
    {
        // Arrange
        var request = new EmbeddingRequest
        {
            ModelId = "azure-openai.text-embedding-ada-002",
            Input = "Hello, world!"
        };

        var responseContent = new AzureOpenAIEmbeddingResponse
        {
            Object = "list",
            Model = "text-embedding-ada-002",
            Data = new List<AzureOpenAIEmbeddingData>
            {
                new AzureOpenAIEmbeddingData
                {
                    Object = "embedding",
                    Embedding = new List<float> { 0.1f, 0.2f, 0.3f },
                    Index = 0
                }
            },
            Usage = new AzureOpenAIEmbeddingUsage
            {
                PromptTokens = 3,
                TotalTokens = 3
            }
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(responseContent),
                    Encoding.UTF8,
                    "application/json")
            });

        // Act
        var response = await _provider.CreateEmbeddingAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("list", response.Object);
        Assert.Equal("AzureOpenAI", response.Provider);
        Assert.Equal("text-embedding-ada-002", response.Model);
        Assert.Single(response.Data);
        Assert.Equal(3, response.Data[0].Embedding.Count);
        Assert.Equal(3, response.Usage.PromptTokens);
        Assert.Equal(3, response.Usage.TotalTokens);
    }
}
