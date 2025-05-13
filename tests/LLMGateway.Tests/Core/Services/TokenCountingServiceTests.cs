using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LLMGateway.Tests.Core.Services;

public class TokenCountingServiceTests
{
    private readonly Mock<ILogger<TokenCountingService>> _loggerMock;
    private readonly Mock<ILLMProviderFactory> _providerFactoryMock;
    private readonly Mock<IModelService> _modelServiceMock;
    private readonly Mock<ILLMProvider> _providerMock;
    private readonly TokenCountingService _service;

    public TokenCountingServiceTests()
    {
        _loggerMock = new Mock<ILogger<TokenCountingService>>();
        _providerFactoryMock = new Mock<ILLMProviderFactory>();
        _modelServiceMock = new Mock<IModelService>();
        _providerMock = new Mock<ILLMProvider>();
        
        _service = new TokenCountingService(
            _loggerMock.Object,
            _providerFactoryMock.Object,
            _modelServiceMock.Object);
    }

    [Fact]
    public void CountTokens_WithEmptyText_ShouldReturnZero()
    {
        // Act
        var result = _service.CountTokens(string.Empty, "gpt-4");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountTokens_WithText_ShouldReturnTokenCount()
    {
        // Arrange
        var text = "This is a test message for token counting.";

        // Act
        var result = _service.CountTokens(text, "gpt-4");

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void CountTokens_WithDifferentModels_ShouldReturnDifferentCounts()
    {
        // Arrange
        var text = "This is a test message for token counting.";

        // Act
        var gpt4Count = _service.CountTokens(text, "gpt-4");
        var claudeCount = _service.CountTokens(text, "claude-3-opus");

        // Assert
        Assert.NotEqual(gpt4Count, claudeCount);
    }

    [Fact]
    public async Task EstimateTokensAsync_ForCompletionRequest_ShouldReturnEstimate()
    {
        // Arrange
        var request = new CompletionRequest
        {
            ModelId = "gpt-4",
            Messages = new List<Message>
            {
                new Message { Role = "system", Content = "You are a helpful assistant." },
                new Message { Role = "user", Content = "Tell me about token counting." }
            },
            MaxTokens = 100
        };

        var modelInfo = new ModelInfo
        {
            Id = "gpt-4",
            Provider = "OpenAI",
            ContextWindow = 8192
        };

        _modelServiceMock.Setup(m => m.GetModelAsync(It.IsAny<string>()))
            .ReturnsAsync(modelInfo);
        
        _providerFactoryMock.Setup(f => f.GetProvider(It.IsAny<string>()))
            .Returns(_providerMock.Object);

        // Act
        var result = await _service.EstimateTokensAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("gpt-4", result.ModelId);
        Assert.Equal("OpenAI", result.Provider);
        Assert.True(result.PromptTokens > 0);
        Assert.True(result.EstimatedCompletionTokens > 0);
        Assert.Equal(result.PromptTokens + result.EstimatedCompletionTokens, result.TotalTokens);
    }

    [Fact]
    public async Task EstimateTokensAsync_ForEmbeddingRequest_ShouldReturnEstimate()
    {
        // Arrange
        var request = new EmbeddingRequest
        {
            ModelId = "text-embedding-ada-002",
            Input = "This is a test input for embedding."
        };

        var modelInfo = new ModelInfo
        {
            Id = "text-embedding-ada-002",
            Provider = "OpenAI",
            ContextWindow = 8191
        };

        _modelServiceMock.Setup(m => m.GetModelAsync(It.IsAny<string>()))
            .ReturnsAsync(modelInfo);
        
        _providerFactoryMock.Setup(f => f.GetProvider(It.IsAny<string>()))
            .Returns(_providerMock.Object);

        // Act
        var result = await _service.EstimateTokensAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("text-embedding-ada-002", result.ModelId);
        Assert.Equal("OpenAI", result.Provider);
        Assert.True(result.PromptTokens > 0);
        Assert.Equal(0, result.EstimatedCompletionTokens); // Embeddings don't have completion tokens
        Assert.Equal(result.PromptTokens, result.TotalTokens);
    }

    [Fact]
    public async Task EstimateTokensAsync_WithArrayInput_ShouldCountAllInputs()
    {
        // Arrange
        var request = new EmbeddingRequest
        {
            ModelId = "text-embedding-ada-002",
            Input = new[] { "First input.", "Second input." }
        };

        var modelInfo = new ModelInfo
        {
            Id = "text-embedding-ada-002",
            Provider = "OpenAI",
            ContextWindow = 8191
        };

        _modelServiceMock.Setup(m => m.GetModelAsync(It.IsAny<string>()))
            .ReturnsAsync(modelInfo);
        
        _providerFactoryMock.Setup(f => f.GetProvider(It.IsAny<string>()))
            .Returns(_providerMock.Object);

        // Act
        var result = await _service.EstimateTokensAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PromptTokens > 0);
    }
}
