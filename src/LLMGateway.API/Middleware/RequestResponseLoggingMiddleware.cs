using LLMGateway.Core.Options;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;

namespace LLMGateway.API.Middleware;

/// <summary>
/// Middleware for logging requests and responses
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly LoggingOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next">Next middleware</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Logging options</param>
    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IOptions<LoggingOptions> options)
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
        // Start the timer
        var stopwatch = Stopwatch.StartNew();
        
        // Log the request
        var requestId = Guid.NewGuid().ToString();
        var requestMethod = context.Request.Method;
        var requestPath = context.Request.Path;
        var requestQuery = context.Request.QueryString;
        
        _logger.LogInformation("Request {RequestId} started: {Method} {Path}{Query}",
            requestId, requestMethod, requestPath, requestQuery);
        
        // Capture the request body if enabled
        string? requestBody = null;
        if (_options.LogRequestResponseBodies && context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            
            using (var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }
            
            // Mask sensitive information if needed
            if (!_options.LogSensitiveInformation)
            {
                requestBody = MaskSensitiveInformation(requestBody);
            }
            
            _logger.LogDebug("Request {RequestId} body: {Body}", requestId, requestBody);
        }
        
        // Capture the response body
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        
        try
        {
            // Call the next middleware
            await _next(context);
            
            // Capture the response body if enabled
            string? responseBody = null;
            if (_options.LogRequestResponseBodies)
            {
                responseBodyStream.Position = 0;
                
                using (var reader = new StreamReader(
                    responseBodyStream,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true))
                {
                    responseBody = await reader.ReadToEndAsync();
                }
                
                // Mask sensitive information if needed
                if (!_options.LogSensitiveInformation)
                {
                    responseBody = MaskSensitiveInformation(responseBody);
                }
                
                _logger.LogDebug("Response {RequestId} body: {Body}", requestId, responseBody);
                
                responseBodyStream.Position = 0;
            }
            
            // Copy the response body to the original stream
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
        
        // Stop the timer and log the response
        stopwatch.Stop();
        var elapsed = stopwatch.ElapsedMilliseconds;
        var statusCode = context.Response.StatusCode;
        
        _logger.LogInformation("Request {RequestId} completed: {Method} {Path}{Query} => {StatusCode} in {Elapsed}ms",
            requestId, requestMethod, requestPath, requestQuery, statusCode, elapsed);
    }

    private string MaskSensitiveInformation(string content)
    {
        // Mask API keys
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            "\"(api[_-]?key|apiKey|ApiKey|API[_-]?KEY)\"\\s*:\\s*\"[^\"]+\"",
            m => $"{m.Value.Substring(0, m.Value.LastIndexOf(':') + 2)}\"***\"");
        
        // Mask JWT tokens
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            "\"(token|Token|TOKEN|access[_-]?token|accessToken|AccessToken)\"\\s*:\\s*\"[^\"]+\"",
            m => $"{m.Value.Substring(0, m.Value.LastIndexOf(':') + 2)}\"***\"");
        
        // Mask authorization headers
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            "\"(Authorization|authorization)\"\\s*:\\s*\"[^\"]+\"",
            m => $"{m.Value.Substring(0, m.Value.LastIndexOf(':') + 2)}\"***\"");
        
        return content;
    }
}
