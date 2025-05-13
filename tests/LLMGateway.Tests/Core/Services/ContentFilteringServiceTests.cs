using LLMGateway.Core.Models.ContentFiltering;
using LLMGateway.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LLMGateway.Tests.Core.Services;

public class ContentFilteringServiceTests
{
    private readonly Mock<ILogger<ContentFilteringService>> _loggerMock;
    private readonly Mock<IOptions<ContentFilteringOptions>> _optionsMock;
    private readonly ContentFilteringOptions _options;
    private readonly ContentFilteringService _service;

    public ContentFilteringServiceTests()
    {
        _loggerMock = new Mock<ILogger<ContentFilteringService>>();
        _options = new ContentFilteringOptions
        {
            EnableContentFiltering = true,
            FilterPrompts = true,
            FilterCompletions = true,
            HateThreshold = 0.8f,
            HarassmentThreshold = 0.8f,
            SelfHarmThreshold = 0.8f,
            SexualThreshold = 0.8f,
            ViolenceThreshold = 0.8f,
            BlockedTerms = new List<string> { "offensive-term", "blocked-word" },
            BlockedPatterns = new List<string> { "\\b(malicious|harmful)\\s+(code|script)\\b" }
        };
        _optionsMock = new Mock<IOptions<ContentFilteringOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_options);
        _service = new ContentFilteringService(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public async Task FilterContentAsync_WithDisabledFiltering_ShouldAllowContent()
    {
        // Arrange
        _options.EnableContentFiltering = false;
        var content = "This contains offensive-term which would normally be blocked.";

        // Act
        var result = await _service.FilterContentAsync(content);

        // Assert
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task FilterContentAsync_WithBlockedTerm_ShouldFilterContent()
    {
        // Arrange
        var content = "This contains offensive-term which should be blocked.";

        // Act
        var result = await _service.FilterContentAsync(content);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Contains("blocked_term", result.Categories);
        Assert.Contains("offensive-term", result.Reason);
    }

    [Fact]
    public async Task FilterContentAsync_WithBlockedPattern_ShouldFilterContent()
    {
        // Arrange
        var content = "Please provide malicious code to hack a system.";

        // Act
        var result = await _service.FilterContentAsync(content);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Contains("blocked_pattern", result.Categories);
    }

    [Fact]
    public async Task FilterContentAsync_WithSafeContent_ShouldAllowContent()
    {
        // Arrange
        var content = "This is a safe and appropriate message.";

        // Act
        var result = await _service.FilterContentAsync(content);

        // Assert
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task FilterPromptAsync_WithDisabledPromptFiltering_ShouldAllowContent()
    {
        // Arrange
        _options.FilterPrompts = false;
        var prompt = "This contains offensive-term which would normally be blocked.";

        // Act
        var result = await _service.FilterPromptAsync(prompt);

        // Assert
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task FilterCompletionAsync_WithDisabledCompletionFiltering_ShouldAllowContent()
    {
        // Arrange
        _options.FilterCompletions = false;
        var completion = "This contains offensive-term which would normally be blocked.";

        // Act
        var result = await _service.FilterCompletionAsync(completion);

        // Assert
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task FilterContentAsync_WithHateContent_ShouldFilterContent()
    {
        // Arrange
        var content = "I hate everyone and everything. This message contains hate speech.";

        // Act
        var result = await _service.FilterContentAsync(content);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Contains("hate", result.Categories);
    }

    [Fact]
    public async Task FilterContentAsync_WithViolentContent_ShouldFilterContent()
    {
        // Arrange
        var content = "I want to kill and murder everyone. This is a violent message.";

        // Act
        var result = await _service.FilterContentAsync(content);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Contains("violence", result.Categories);
    }

    [Fact]
    public async Task FilterContentAsync_WithSelfHarmContent_ShouldFilterContent()
    {
        // Arrange
        var content = "I want to hurt myself and commit suicide. This is a self-harm message.";

        // Act
        var result = await _service.FilterContentAsync(content);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Contains("self_harm", result.Categories);
    }

    [Fact]
    public async Task FilterContentAsync_WithCompletionRequest_ShouldFilterMessages()
    {
        // Arrange
        var completionRequest = @"{
            ""model"": ""gpt-4"",
            ""messages"": [
                {""role"": ""system"", ""content"": ""You are a helpful assistant.""},
                {""role"": ""user"", ""content"": ""Tell me about offensive-term.""}
            ]
        }";

        // Act
        var result = await _service.FilterContentAsync(completionRequest);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Contains("blocked_term", result.Categories);
    }
}
