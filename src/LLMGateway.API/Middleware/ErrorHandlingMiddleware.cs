using LLMGateway.Core.Exceptions;
using System.Net;
using System.Text.Json;

namespace LLMGateway.API.Middleware;

/// <summary>
/// Middleware for handling errors
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next">Next middleware</param>
    /// <param name="logger">Logger</param>
    /// <param name="environment">Web host environment</param>
    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invoke the middleware
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");
        
        var code = HttpStatusCode.InternalServerError;
        var message = "An unexpected error occurred";
        var errorCode = "internal_server_error";
        
        // Map exception types to status codes
        switch (exception)
        {
            case ValidationException:
                code = HttpStatusCode.BadRequest;
                message = exception.Message;
                errorCode = "validation_error";
                break;
                
            case NotFoundException:
                code = HttpStatusCode.NotFound;
                message = exception.Message;
                errorCode = "not_found";
                break;
                
            // These specific not found cases are handled by the base NotFoundException case
            /*
            case ModelNotFoundException:
                code = HttpStatusCode.NotFound;
                message = exception.Message;
                errorCode = "model_not_found";
                break;
                
            case ProviderNotFoundException:
                code = HttpStatusCode.NotFound;
                message = exception.Message;
                errorCode = "provider_not_found";
                break;
            */
                
            case ProviderAuthenticationException:
                code = HttpStatusCode.Unauthorized;
                message = exception.Message;
                errorCode = "provider_authentication_error";
                break;
                
            case RateLimitExceededException:
                code = HttpStatusCode.TooManyRequests;
                message = exception.Message;
                errorCode = "rate_limit_exceeded";
                break;
                
            case ProviderUnavailableException:
                code = HttpStatusCode.ServiceUnavailable;
                message = exception.Message;
                errorCode = "provider_unavailable";
                break;
                
            case ProviderException:
                code = HttpStatusCode.BadGateway;
                message = exception.Message;
                errorCode = ((ProviderException)exception).ErrorCode ?? "provider_error";
                break;
                
            case RoutingException:
                code = HttpStatusCode.BadRequest;
                message = exception.Message;
                errorCode = "routing_error";
                break;
                
            case FallbackExhaustedException:
                code = HttpStatusCode.ServiceUnavailable;
                message = exception.Message;
                errorCode = "fallback_exhausted";
                break;
        }
        
        var result = JsonSerializer.Serialize(new
        {
            error = new
            {
                code = errorCode,
                message = message,
                stackTrace = _environment.IsDevelopment() ? exception.StackTrace : null
            }
        });
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        
        return context.Response.WriteAsync(result);
    }
}
