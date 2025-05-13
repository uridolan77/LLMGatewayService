using LLMGateway.Core.Options;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace LLMGateway.API.Middleware;

/// <summary>
/// Middleware for API key authentication
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly ApiKeyOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next">Next middleware</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">API key options</param>
    public ApiKeyMiddleware(
        RequestDelegate next,
        ILogger<ApiKeyMiddleware> logger,
        IOptions<ApiKeyOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Invoke the middleware
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key authentication for health checks
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }
        
        // Check if the request has a valid API key
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            // No API key provided, continue to the next middleware
            // JWT authentication will handle this
            await _next(context);
            return;
        }
        
        var apiKey = apiKeyHeader.ToString();
        
        // Find the API key in the options
        var apiKeyConfig = _options.ApiKeys.FirstOrDefault(k => k.Key == apiKey);
        
        if (apiKeyConfig == null)
        {
            _logger.LogWarning("Invalid API key provided");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }
        
        // Create claims for the API key
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, apiKeyConfig.Owner),
            new Claim(ClaimTypes.NameIdentifier, apiKeyConfig.Id)
        };
        
        // Add permissions as claims
        foreach (var permission in apiKeyConfig.Permissions)
        {
            claims.Add(new Claim("llm-permissions", permission));
        }
        
        // Create the identity and principal
        var identity = new ClaimsIdentity(claims, "ApiKey");
        var principal = new ClaimsPrincipal(identity);
        
        // Set the user
        context.User = principal;
        
        await _next(context);
    }
}
