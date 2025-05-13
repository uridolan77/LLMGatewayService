using LLMGateway.API.Middleware;
using LLMGateway.Core.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;

namespace LLMGateway.Tests.API.Middleware;

public class EnhancedErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<EnhancedErrorHandlingMiddleware>> _loggerMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly EnhancedErrorHandlingMiddleware _middleware;
    private readonly RequestDelegate _nextMock;

    public EnhancedErrorHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<EnhancedErrorHandlingMiddleware>>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _nextMock = (HttpContext context) => Task.CompletedTask;
        _middleware = new EnhancedErrorHandlingMiddleware(_nextMock, _loggerMock.Object, _environmentMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithNoException_ShouldCallNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new EnhancedErrorHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithException_ShouldHandleError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new ValidationException("Validation error");
        };
        
        var middleware = new EnhancedErrorHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        
        // Check response body
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        Assert.Equal("Validation Error", problemDetails.GetProperty("title").GetString());
        Assert.Equal("Validation error", problemDetails.GetProperty("detail").GetString());
        Assert.Equal(400, problemDetails.GetProperty("status").GetInt32());
        Assert.True(problemDetails.GetProperty("extensions").TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task InvokeAsync_WithModelNotFoundException_ShouldReturn404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new ModelNotFoundException("gpt-4");
        };
        
        var middleware = new EnhancedErrorHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithProviderException_ShouldIncludeProviderInfo()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new ProviderException("OpenAI", "API error", "rate_limit_exceeded");
        };
        
        var middleware = new EnhancedErrorHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadGateway, context.Response.StatusCode);
        
        // Check response body
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        Assert.Equal("Provider Error", problemDetails.GetProperty("title").GetString());
        Assert.Equal("API error", problemDetails.GetProperty("detail").GetString());
        Assert.Equal("OpenAI", problemDetails.GetProperty("extensions").GetProperty("provider").GetString());
        Assert.Equal("rate_limit_exceeded", problemDetails.GetProperty("extensions").GetProperty("providerErrorCode").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WithCorrelationId_ShouldPreserveIt()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Headers["X-Correlation-ID"] = "test-correlation-id";
        
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new Exception("Test exception");
        };
        
        var middleware = new EnhancedErrorHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("test-correlation-id", context.Response.Headers["X-Correlation-ID"]);
    }

    [Fact]
    public async Task InvokeAsync_InDevelopmentEnvironment_ShouldIncludeStackTrace()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        _environmentMock.Setup(e => e.IsDevelopment()).Returns(true);
        
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new Exception("Test exception");
        };
        
        var middleware = new EnhancedErrorHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        Assert.True(problemDetails.GetProperty("extensions").TryGetProperty("stackTrace", out _));
        Assert.True(problemDetails.GetProperty("extensions").TryGetProperty("exceptionType", out _));
    }
}
