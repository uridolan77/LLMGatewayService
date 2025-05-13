using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.ContentFiltering;
using System.Text;
using System.Text.Json;

namespace LLMGateway.API.Middleware;

/// <summary>
/// Middleware for filtering content
/// </summary>
public class ContentFilteringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IContentFilteringService _contentFilteringService;
    private readonly ILogger<ContentFilteringMiddleware> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next">Next middleware</param>
    /// <param name="contentFilteringService">Content filtering service</param>
    /// <param name="logger">Logger</param>
    public ContentFilteringMiddleware(
        RequestDelegate next,
        IContentFilteringService contentFilteringService,
        ILogger<ContentFilteringMiddleware> logger)
    {
        _next = next;
        _contentFilteringService = contentFilteringService;
        _logger = logger;
    }

    /// <summary>
    /// Invoke the middleware
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only check POST requests to completion and embedding endpoints
        if (context.Request.Method == "POST" && 
            (context.Request.Path.StartsWithSegments("/api/v1/completions") ||
             context.Request.Path.StartsWithSegments("/api/v1/embeddings")))
        {
            // Enable buffering so we can read the request body multiple times
            context.Request.EnableBuffering();
            
            // Read the request body
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            
            var body = await reader.ReadToEndAsync();
            
            // Reset the request body position
            context.Request.Body.Position = 0;
            
            // Check if the content is allowed
            var filterResult = await _contentFilteringService.FilterContentAsync(body);
            
            if (!filterResult.IsAllowed)
            {
                _logger.LogWarning("Content filtered: {Reason}", filterResult.Reason);
                
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                
                var response = new
                {
                    error = new
                    {
                        code = "content_filtered",
                        message = "Content violates usage policies",
                        details = filterResult.Reason,
                        categories = filterResult.Categories
                    }
                };
                
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }
        }
        
        await _next(context);
    }
}
