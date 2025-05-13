using LLMGateway.Core.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Serilog.Context;

namespace LLMGateway.API.Middleware;

/// <summary>
/// Enhanced middleware for handling errors with correlation IDs and structured responses
/// </summary>
public class EnhancedErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EnhancedErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next">Next middleware</param>
    /// <param name="logger">Logger</param>
    /// <param name="environment">Web host environment</param>
    public EnhancedErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<EnhancedErrorHandlingMiddleware> logger,
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
        // Generate a correlation ID if not already present
        if (!context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers.Add("X-Correlation-ID", correlationId);
        }

        // Add correlation ID to response headers
        context.Response.Headers.Add("X-Correlation-ID", correlationId);

        // Add correlation ID to the logging context
        using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, correlationId.ToString());
            }
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        _logger.LogError(exception, "An unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);

        var statusCode = GetStatusCode(exception);
        var errorCode = GetErrorCode(exception);
        var message = GetUserFriendlyMessage(exception);

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = GetTitle(exception),
            Detail = message,
            Instance = context.Request.Path,
            Extensions =
            {
                ["correlationId"] = correlationId,
                ["errorCode"] = errorCode,
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
            }
        };

        // Add additional details for specific exception types
        if (exception is ValidationException validationEx && validationEx.Errors != null)
        {
            problemDetails.Extensions["errors"] = validationEx.Errors;
        }

        if (exception is ProviderException providerEx)
        {
            problemDetails.Extensions["provider"] = providerEx.ProviderName;

            if (!string.IsNullOrEmpty(providerEx.ErrorCode))
            {
                problemDetails.Extensions["providerErrorCode"] = providerEx.ErrorCode;
            }
        }

        // Add stack trace in development environment
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;

            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    message = exception.InnerException.Message,
                    stackTrace = exception.InnerException.StackTrace,
                    exceptionType = exception.InnerException.GetType().Name
                };
            }
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private static HttpStatusCode GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => HttpStatusCode.BadRequest,
            ModelNotFoundException => HttpStatusCode.NotFound,
            ProviderNotFoundException => HttpStatusCode.NotFound,
            NotFoundException => HttpStatusCode.NotFound,
            ProviderAuthenticationException => HttpStatusCode.Unauthorized,
            RateLimitExceededException => HttpStatusCode.TooManyRequests,
            ProviderUnavailableException => HttpStatusCode.ServiceUnavailable,
            ProviderException => HttpStatusCode.BadGateway,
            RoutingException => HttpStatusCode.BadRequest,
            FallbackExhaustedException => HttpStatusCode.ServiceUnavailable,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static string GetErrorCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => "validation_error",
            ModelNotFoundException => "model_not_found",
            ProviderNotFoundException => "provider_not_found",
            NotFoundException => "not_found",
            ProviderAuthenticationException => "provider_authentication_error",
            RateLimitExceededException => "rate_limit_exceeded",
            ProviderUnavailableException => "provider_unavailable",
            ProviderException providerEx => providerEx.ErrorCode ?? "provider_error",
            RoutingException => "routing_error",
            FallbackExhaustedException => "fallback_exhausted",
            _ => "internal_server_error"
        };
    }

    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            ValidationException => "Validation Error",
            ModelNotFoundException => "Model Not Found",
            ProviderNotFoundException => "Provider Not Found",
            NotFoundException => "Resource Not Found",
            ProviderAuthenticationException => "Provider Authentication Error",
            RateLimitExceededException => "Rate Limit Exceeded",
            ProviderUnavailableException => "Provider Unavailable",
            ProviderException => "Provider Error",
            RoutingException => "Routing Error",
            FallbackExhaustedException => "Fallback Exhausted",
            _ => "Internal Server Error"
        };
    }

    private static string GetUserFriendlyMessage(Exception exception)
    {
        // Return the exception message for most cases
        // For internal server errors, provide a more generic message
        return exception is not LLMGatewayException && exception is not ValidationException
            ? "An unexpected error occurred. Please try again later or contact support if the problem persists."
            : exception.Message;
    }
}

/// <summary>
/// Problem details for RFC 7807 compliant error responses
/// </summary>
public class ProblemDetails
{
    /// <summary>
    /// The HTTP status code
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// A short, human-readable summary of the problem type
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the problem
    /// </summary>
    public Dictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();
}
