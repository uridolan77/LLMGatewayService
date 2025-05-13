using LLMGateway.API.Middleware;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.ContentFiltering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;

namespace LLMGateway.Tests.API.Middleware;

public class ContentFilteringMiddlewareTests
{
    private readonly Mock<ILogger<ContentFilteringMiddleware>> _loggerMock;
    private readonly Mock<IContentFilteringService> _contentFilteringServiceMock;
    private readonly ContentFilteringMiddleware _middleware;
    private readonly RequestDelegate _nextMock;

    public ContentFilteringMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ContentFilteringMiddleware>>();
        _contentFilteringServiceMock = new Mock<IContentFilteringService>();
        _nextMock = (HttpContext context) => Task.CompletedTask;
        _middleware = new ContentFilteringMiddleware(_nextMock, _contentFilteringServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithNonFilteredEndpoint_ShouldCallNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/users";
        
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        
        var middleware = new ContentFilteringMiddleware(next, _contentFilteringServiceMock.Object, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        _contentFilteringServiceMock.Verify(s => s.FilterContentAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithCompletionsEndpoint_ShouldFilterContent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/completions";
        
        var requestBody = "{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"user\",\"content\":\"Hello\"}]}";
        var requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);
        context.Request.Body = new MemoryStream(requestBodyBytes);
        
        _contentFilteringServiceMock.Setup(s => s.FilterContentAsync(It.IsAny<string>()))
            .ReturnsAsync(ContentFilterResult.Allowed());
        
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        
        var middleware = new ContentFilteringMiddleware(next, _contentFilteringServiceMock.Object, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        _contentFilteringServiceMock.Verify(s => s.FilterContentAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithEmbeddingsEndpoint_ShouldFilterContent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/embeddings";
        
        var requestBody = "{\"model\":\"text-embedding-ada-002\",\"input\":\"Hello\"}";
        var requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);
        context.Request.Body = new MemoryStream(requestBodyBytes);
        
        _contentFilteringServiceMock.Setup(s => s.FilterContentAsync(It.IsAny<string>()))
            .ReturnsAsync(ContentFilterResult.Allowed());
        
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        
        var middleware = new ContentFilteringMiddleware(next, _contentFilteringServiceMock.Object, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        _contentFilteringServiceMock.Verify(s => s.FilterContentAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithBlockedContent_ShouldReturn403()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/completions";
        context.Response.Body = new MemoryStream();
        
        var requestBody = "{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"user\",\"content\":\"Offensive content\"}]}";
        var requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);
        context.Request.Body = new MemoryStream(requestBodyBytes);
        
        _contentFilteringServiceMock.Setup(s => s.FilterContentAsync(It.IsAny<string>()))
            .ReturnsAsync(ContentFilterResult.Filtered("Content contains blocked terms", "hate", "violence"));
        
        var middleware = new ContentFilteringMiddleware(_nextMock, _contentFilteringServiceMock.Object, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        
        // Check response body
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        Assert.Equal("content_filtered", response.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("Content violates usage policies", response.GetProperty("error").GetProperty("message").GetString());
        Assert.Equal("Content contains blocked terms", response.GetProperty("error").GetProperty("details").GetString());
        
        var categories = response.GetProperty("error").GetProperty("categories").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("hate", categories);
        Assert.Contains("violence", categories);
    }

    [Fact]
    public async Task InvokeAsync_ShouldResetRequestBodyPosition()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/completions";
        
        var requestBody = "{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"user\",\"content\":\"Hello\"}]}";
        var requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);
        context.Request.Body = new MemoryStream(requestBodyBytes);
        
        _contentFilteringServiceMock.Setup(s => s.FilterContentAsync(It.IsAny<string>()))
            .ReturnsAsync(ContentFilterResult.Allowed());
        
        var bodyReadInNext = string.Empty;
        RequestDelegate next = async (HttpContext ctx) =>
        {
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
            bodyReadInNext = await reader.ReadToEndAsync();
        };
        
        var middleware = new ContentFilteringMiddleware(next, _contentFilteringServiceMock.Object, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(requestBody, bodyReadInNext);
    }
}
