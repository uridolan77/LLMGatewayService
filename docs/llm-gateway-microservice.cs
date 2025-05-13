// LLMGateway.sln Solution Structure:
// 
// - LLMGateway.API               (Main API project)
// - LLMGateway.Core              (Core domain logic)
// - LLMGateway.Providers         (Provider implementations)
// - LLMGateway.Infrastructure    (Infrastructure services)
// - LLMGateway.Tests             (Unit and integration tests)

// src/LLMGateway.API/Program.cs
using LLMGateway.API.Extensions;
using LLMGateway.API.Middleware;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Services;
using LLMGateway.Infrastructure.Caching;
using LLMGateway.Infrastructure.Logging;
using LLMGateway.Infrastructure.Telemetry;
using LLMGateway.Providers.Factory;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((hostContext, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(hostContext.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName();
});

// Configure services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LLM Gateway API",
        Version = "v1",
        Description = "A comprehensive API gateway for Language Learning Models",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Add XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

// Add core services
builder.Services.AddLLMGatewayOptions(builder.Configuration);
builder.Services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<ICompletionService, CompletionService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IModelService, ModelService>();
builder.Services.AddSingleton<ITokenUsageService, TokenUsageService>();

// Add infrastructure services
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddTelemetry(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddHealthChecks();

// Add authentication and authorization
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

// Configure providers
builder.Services.AddLLMProviders(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LLM Gateway API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckExtensions.WriteHealthCheckResponse
});

app.Run();

// src/LLMGateway.API/Extensions/ServiceCollectionExtensions.cs
using LLMGateway.Core.Options;
using LLMGateway.Providers.Anthropic;
using LLMGateway.Providers.Cohere;
using LLMGateway.Providers.HuggingFace;
using LLMGateway.Providers.OpenAI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

namespace LLMGateway.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLLMGatewayOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GlobalOptions>(configuration.GetSection("GlobalOptions"));
        services.Configure<LLMRoutingOptions>(configuration.GetSection("LLMRouting"));
        services.Configure<FallbackOptions>(configuration.GetSection("Fallbacks"));
        services.Configure<LoggingOptions>(configuration.GetSection("Logging"));
        services.Configure<TelemetryOptions>(configuration.GetSection("Telemetry"));
        services.Configure<ApiKeyOptions>(configuration.GetSection("ApiKeys"));
        services.Configure<TokenUsageOptions>(configuration.GetSection("TokenUsage"));
        
        return services;
    }
    
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = new JwtOptions();
        configuration.GetSection("Jwt").Bind(jwtOptions);
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
            };
        });
        
        return services;
    }
    
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CompletionAccess", policy => policy.RequireClaim("llm-permissions", "completion"));
            options.AddPolicy("EmbeddingAccess", policy => policy.RequireClaim("llm-permissions", "embedding"));
            options.AddPolicy("AdminAccess", policy => policy.RequireClaim("llm-permissions", "admin"));
        });
        
        return services;
    }
    
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitOptions = new RateLimitOptions();
        configuration.GetSection("RateLimiting").Bind(rateLimitOptions);
        
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault() ?? string.Empty;
                
                return RateLimitPartition.GetTokenBucketLimiter(apiKey, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = rateLimitOptions.TokenLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = rateLimitOptions.QueueLimit,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimitOptions.ReplenishmentPeriodSeconds),
                    TokensPerPeriod = rateLimitOptions.TokensPerPeriod,
                    AutoReplenishment = true
                });
            });
            
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests. Please try again later.",
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter.TotalSeconds : null
                }, token);
            };
        });
        
        return services;
    }
    
    public static IServiceCollection AddLLMProviders(this IServiceCollection services, IConfiguration configuration)
    {
        // Register all provider configurations
        services.Configure<OpenAIOptions>(configuration.GetSection("Providers:OpenAI"));
        services.Configure<AnthropicOptions>(configuration.GetSection("Providers:Anthropic"));
        services.Configure<CohereOptions>(configuration.GetSection("Providers:Cohere"));
        services.Configure<HuggingFaceOptions>(configuration.GetSection("Providers:HuggingFace"));
        
        // Register provider services
        services.AddHttpClient<OpenAIProvider>()
            .AddPolicyHandler(PoliciesToProviders.GetRetryPolicy())
            .AddPolicyHandler(PoliciesToProviders.GetCircuitBreakerPolicy());
            
        services.AddHttpClient<AnthropicProvider>()
            .AddPolicyHandler(PoliciesToProviders.GetRetryPolicy())
            .AddPolicyHandler(PoliciesToProviders.GetCircuitBreakerPolicy());
            
        services.AddHttpClient<CohereProvider>()
            .AddPolicyHandler(PoliciesToProviders.GetRetryPolicy())
            .AddPolicyHandler(PoliciesToProviders.GetCircuitBreakerPolicy());
            
        services.AddHttpClient<HuggingFaceProvider>()
            .AddPolicyHandler(PoliciesToProviders.GetRetryPolicy())
            .AddPolicyHandler(PoliciesToProviders.GetCircuitBreakerPolicy());
        
        return services;
    }
}

// src/LLMGateway.API/Extensions/HealthCheckExtensions.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace LLMGateway.API.Extensions;

public static class HealthCheckExtensions
{
    public static Task WriteHealthCheckResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json";
        
        var result = JsonSerializer.Serialize(new
        {
            status = healthReport.Status.ToString(),
            checks = healthReport.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = healthReport.TotalDuration.TotalMilliseconds
        });
        
        return context.Response.WriteAsync(result);
    }
}

// src/LLMGateway.API/Extensions/PoliciesToProviders.cs
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace LLMGateway.API.Extensions;

public static class PoliciesToProviders
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    context.GetLogger()?.LogWarning(
                        "Delaying for {delay}ms, then making retry {retry}",
                        timespan.TotalMilliseconds, retryAttempt);
                });
    }
    
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                onBreak: (outcome, timespan, context) =>
                {
                    context.GetLogger()?.LogWarning(
                        "Circuit breaker opened for {timespan}s",
                        timespan.TotalSeconds);
                },
                onReset: context =>
                {
                    context.GetLogger()?.LogInformation("Circuit breaker reset");
                });
    }
}

// src/LLMGateway.API/Middleware/ApiKeyMiddleware.cs
using LLMGateway.Core.Options;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace LLMGateway.API.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<ApiKeyOptions> _apiKeyOptions;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    
    public ApiKeyMiddleware(
        RequestDelegate next,
        IOptions<ApiKeyOptions> apiKeyOptions,
        ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _apiKeyOptions = apiKeyOptions;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key validation for health check and swagger
        var path = context.Request.Path.ToString().ToLower();
        if (path.StartsWith("/health") || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }
        
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey))
        {
            _logger.LogWarning("API key was not provided");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key is required" });
            return;
        }
        
        var apiKey = extractedApiKey.ToString();
        var validApiKey = _apiKeyOptions.Value.ApiKeys.FirstOrDefault(k => k.Key == apiKey);
        
        if (validApiKey == null)
        {
            _logger.LogWarning("Invalid API key provided: {apiKey}", apiKey);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }
        
        // Add claims based on API key permissions
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, validApiKey.Owner),
            new Claim("ApiKeyId", validApiKey.Id.ToString())
        };
        
        foreach (var permission in validApiKey.Permissions)
        {
            claims.Add(new Claim("llm-permissions", permission));
        }
        
        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);
        
        _logger.LogInformation("API key validated for owner: {owner}", validApiKey.Owner);
        
        await _next(context);
    }
}

// src/LLMGateway.API/Middleware/ErrorHandlingMiddleware.cs
using LLMGateway.Core.Exceptions;
using System.Net;
using System.Text.Json;

namespace LLMGateway.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;
    
    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }
    
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
        var code = HttpStatusCode.InternalServerError;
        var errorDetail = "An unexpected error occurred";
        
        switch (exception)
        {
            case ValidationException validationEx:
                code = HttpStatusCode.BadRequest;
                errorDetail = validationEx.Message;
                _logger.LogWarning(exception, "Validation error: {Message}", validationEx.Message);
                break;
                
            case NotFoundException notFoundEx:
                code = HttpStatusCode.NotFound;
                errorDetail = notFoundEx.Message;
                _logger.LogWarning(exception, "Resource not found: {Message}", notFoundEx.Message);
                break;
                
            case ProviderException providerEx:
                code = HttpStatusCode.BadGateway;
                errorDetail = providerEx.Message;
                _logger.LogError(exception, "Provider error: {Message}", providerEx.Message);
                break;
                
            case UnauthorizedException unauthorizedEx:
                code = HttpStatusCode.Unauthorized;
                errorDetail = unauthorizedEx.Message;
                _logger.LogWarning(exception, "Unauthorized access: {Message}", unauthorizedEx.Message);
                break;
                
            default:
                _logger.LogError(exception, "Unhandled exception");
                break;
        }
        
        var result = JsonSerializer.Serialize(new
        {
            error = new
            {
                message = errorDetail,
                exceptionType = exception.GetType().Name,
                stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
            }
        });
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        
        return context.Response.WriteAsync(result);
    }
}

// src/LLMGateway.API/Middleware/RequestResponseLoggingMiddleware.cs
namespace LLMGateway.API.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    
    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // First, log the incoming request
        LogRequest(context);
        
        // Create a new response body stream to capture the response
        var originalBodyStream = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        
        try
        {
            // Continue down the middleware pipeline
            await _next(context);
            
            // Log the response after the request has been processed
            await LogResponse(context, responseBody, originalBodyStream);
        }
        finally
        {
            // Always restore the original response body stream
            context.Response.Body = originalBodyStream;
        }
    }
    
    private void LogRequest(HttpContext context)
    {
        var request = context.Request;
        
        _logger.LogInformation(
            "HTTP {Method} {Path} from {RemoteIP} with Query {Query}",
            request.Method,
            request.Path,
            context.Connection.RemoteIpAddress,
            request.QueryString);
    }
    
    private async Task LogResponse(HttpContext context, MemoryStream responseBody, Stream originalBodyStream)
    {
        responseBody.Position = 0;
        
        // Don't log the response body for binary content or large responses
        var contentType = context.Response.ContentType ?? string.Empty;
        var skipBodyLogging = contentType.Contains("image") || 
                              contentType.Contains("application/octet-stream") ||
                              responseBody.Length > 10000;
        
        if (skipBodyLogging)
        {
            _logger.LogInformation(
                "HTTP {StatusCode} returned for {Method} {Path} (Body logging skipped: {ContentType}, {ContentLength} bytes)",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path,
                contentType,
                responseBody.Length);
        }
        else
        {
            var text = await new StreamReader(responseBody).ReadToEndAsync();
            
            _logger.LogInformation(
                "HTTP {StatusCode} returned for {Method} {Path} (Content-Type: {ContentType}, {ContentLength} bytes)",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path,
                contentType,
                responseBody.Length);
            
            // Only log response body for development environments or if needed for debugging
            _logger.LogDebug("Response body: {ResponseBody}", text);
        }
        
        // Reset the position and copy to the original stream
        responseBody.Position = 0;
        await responseBody.CopyToAsync(originalBodyStream);
    }
}

// src/LLMGateway.API/Controllers/V1/CompletionController.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/completion")]
[Authorize(Policy = "CompletionAccess")]
public class CompletionController : ControllerBase
{
    private readonly ICompletionService _completionService;
    private readonly ILogger<CompletionController> _logger;
    
    public CompletionController(
        ICompletionService completionService,
        ILogger<CompletionController> logger)
    {
        _completionService = completionService;
        _logger = logger;
    }
    
    /// <summary>
    /// Creates a completion for the specified text prompt
    /// </summary>
    /// <param name="request">The completion request parameters</param>
    /// <returns>A completion response</returns>
    /// <response code="200">Returns the completion response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the API key is invalid</response>
    /// <response code="403">If the user doesn't have permission to access the endpoint</response>
    /// <response code="404">If the requested model doesn't exist</response>
    /// <response code="429">If rate limit is exceeded</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCompletion([FromBody] CompletionRequest request)
    {
        _logger.LogInformation("Processing completion request for model: {Model}", request.Model);
        
        var response = await _completionService.CreateCompletionAsync(request, GetUserIdentifier());
        
        return Ok(response);
    }
    
    /// <summary>
    /// Creates a streaming completion for the specified text prompt
    /// </summary>
    /// <param name="request">The completion request parameters</param>
    /// <returns>A stream of completion chunks</returns>
    /// <response code="200">Returns the completion stream</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the API key is invalid</response>
    /// <response code="403">If the user doesn't have permission to access the endpoint</response>
    /// <response code="404">If the requested model doesn't exist</response>
    /// <response code="429">If rate limit is exceeded</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost("stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StreamCompletion([FromBody] CompletionRequest request)
    {
        _logger.LogInformation("Processing streaming completion request for model: {Model}", request.Model);
        
        var responseStream = _completionService.StreamCompletionAsync(request, GetUserIdentifier());
        
        return new StreamingResult(responseStream);
    }
    
    private string GetUserIdentifier()
    {
        return User.Claims.FirstOrDefault(c => c.Type == "ApiKeyId")?.Value ?? "unknown";
    }
}

// Custom IActionResult for streaming SSE responses
public class StreamingResult : IActionResult
{
    private readonly IAsyncEnumerable<object> _stream;
    
    public StreamingResult(IAsyncEnumerable<object> stream)
    {
        _stream = stream;
    }
    
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = "text/event-stream";
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");
        
        await foreach (var chunk in _stream)
        {
            var serialized = System.Text.Json.JsonSerializer.Serialize(chunk);
            await response.WriteAsync($"data: {serialized}\n\n");
            await response.Body.FlushAsync();
        }
    }
}

// src/LLMGateway.API/Controllers/V1/EmbeddingController.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/embedding")]
[Authorize(Policy = "EmbeddingAccess")]
public class EmbeddingController : ControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<EmbeddingController> _logger;
    
    public EmbeddingController(
        IEmbeddingService embeddingService,
        ILogger<EmbeddingController> logger)
    {
        _embeddingService = embeddingService;
        _logger = logger;
    }
    
    /// <summary>
    /// Creates embeddings for the provided input text
    /// </summary>
    /// <param name="request">The embedding request parameters</param>
    /// <returns>The embedding vectors</returns>
    /// <response code="200">Returns the embedding vectors</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the API key is invalid</response>
    /// <response code="403">If the user doesn't have permission to access the endpoint</response>
    /// <response code="404">If the requested model doesn't exist</response>
    /// <response code="429">If rate limit is exceeded</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEmbedding([FromBody] EmbeddingRequest request)
    {
        _logger.LogInformation("Processing embedding request for model: {Model}", request.Model);
        
        var response = await _embeddingService.CreateEmbeddingAsync(request, GetUserIdentifier());
        
        return Ok(response);
    }
    
    private string GetUserIdentifier()
    {
        return User.Claims.FirstOrDefault(c => c.Type == "ApiKeyId")?.Value ?? "unknown";
    }
}

// src/LLMGateway.API/Controllers/V1/ModelController.cs
using LLMGateway.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/models")]
public class ModelController : ControllerBase
{
    private readonly IModelService _modelService;
    private readonly ILogger<ModelController> _logger;
    
    public ModelController(
        IModelService modelService,
        ILogger<ModelController> logger)
    {
        _modelService = modelService;
        _logger = logger;
    }
    
    /// <summary>
    /// Lists all available models
    /// </summary>
    /// <returns>A list of available models</returns>
    /// <response code="200">Returns the list of models</response>
    /// <response code="401">If the API key is invalid</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetModels()
    {
        _logger.LogInformation("Retrieving all available models");
        
        var models = await _modelService.GetAvailableModelsAsync();
        
        return Ok(models);
    }
    
    /// <summary>
    /// Gets information about a specific model
    /// </summary>
    /// <param name="id">The model ID</param>
    /// <returns>Information about the specified model</returns>
    /// <response code="200">Returns the model information</response>
    /// <response code="401">If the API key is invalid</response>
    /// <response code="404">If the model doesn't exist</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetModel(string id)
    {
        _logger.LogInformation("Retrieving model details for: {ModelId}", id);
        
        var model = await _modelService.GetModelByIdAsync(id);
        
        return Ok(model);
    }
    
    /// <summary>
    /// Gets the provider-specific models for a provider
    /// </summary>
    /// <param name="provider">The provider name</param>
    /// <returns>A list of models for the specified provider</returns>
    /// <response code="200">Returns the list of models</response>
    /// <response code="401">If the API key is invalid</response>
    /// <response code="404">If the provider doesn't exist</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("provider/{provider}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetModelsByProvider(string provider)
    {
        _logger.LogInformation("Retrieving models for provider: {Provider}", provider);
        
        var models = await _modelService.GetModelsByProviderAsync(provider);
        
        return Ok(models);
    }
}

// src/LLMGateway.API/Controllers/V1/AdminController.cs
using LLMGateway.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Policy = "AdminAccess")]
public class AdminController : ControllerBase
{
    private readonly ITokenUsageService _tokenUsageService;
    private readonly ILogger<AdminController> _logger;
    
    public AdminController(
        ITokenUsageService tokenUsageService,
        ILogger<AdminController> logger)
    {
        _tokenUsageService = tokenUsageService;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets token usage statistics
    /// </summary>
    /// <param name="startDate">The start date for the report</param>
    /// <param name="endDate">The end date for the report</param>
    /// <returns>Token usage statistics</returns>
    /// <response code="200">Returns the token usage statistics</response>
    /// <response code="401">If the API key is invalid</response>
    /// <response code="403">If the user doesn't have permission to access the endpoint</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("usage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTokenUsage(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;
        
        _logger.LogInformation("Retrieving token usage statistics from {StartDate} to {EndDate}",
            startDate, endDate);
        
        var usage = await _tokenUsageService.GetTokenUsageAsync(startDate.Value, endDate.Value);
        
        return Ok(usage);
    }
    
    /// <summary>
    /// Gets token usage by user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="startDate">The start date for the report</param>
    /// <param name="endDate">The end date for the report</param>
    /// <returns>Token usage statistics for the specified user</returns>
    /// <response code="200">Returns the token usage statistics</response>
    /// <response code="401">If the API key is invalid</response>
    /// <response code="403">If the user doesn't have permission to access the endpoint</response>
    /// <response code="404">If the user doesn't exist</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("usage/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTokenUsageByUser(
        string userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;
        
        _logger.LogInformation("Retrieving token usage statistics for user {UserId} from {StartDate} to {EndDate}",
            userId, startDate, endDate);
        
        var usage = await _tokenUsageService.GetTokenUsageByUserAsync(userId, startDate.Value, endDate.Value);
        
        return Ok(usage);
    }
}

// src/LLMGateway.Core/Interfaces/ICompletionService.cs
namespace LLMGateway.Core.Interfaces;

using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;

public interface ICompletionService
{
    Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request, string userId);
    IAsyncEnumerable<CompletionChunk> StreamCompletionAsync(CompletionRequest request, string userId);
}

// src/LLMGateway.Core/Interfaces/IEmbeddingService.cs
namespace LLMGateway.Core.Interfaces;

using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;

public interface IEmbeddingService
{
    Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, string userId);
}

// src/LLMGateway.Core/Interfaces/ILLMProvider.cs
namespace LLMGateway.Core.Interfaces;

using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;

public interface ILLMProvider
{
    string ProviderName { get; }
    
    Task<List<ModelInfo>> GetAvailableModelsAsync();
    
    Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request);
    
    IAsyncEnumerable<CompletionChunk> StreamCompletionAsync(CompletionRequest request);
    
    Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request);
    
    bool SupportsModel(string modelId);
    
    int CalculateTokenCount(string text, string modelId);
}

// src/LLMGateway.Core/Interfaces/ILLMProviderFactory.cs
namespace LLMGateway.Core.Interfaces;

public interface ILLMProviderFactory
{
    ILLMProvider GetProvider(string providerName);
    ILLMProvider GetProviderForModel(string modelId);
    IEnumerable<ILLMProvider> GetAllProviders();
}

// src/LLMGateway.Core/Interfaces/IModelService.cs
namespace LLMGateway.Core.Interfaces;

using LLMGateway.Core.Models;

public interface IModelService
{
    Task<List<ModelInfo>> GetAvailableModelsAsync();
    Task<ModelInfo> GetModelByIdAsync(string id);
    Task<List<ModelInfo>> GetModelsByProviderAsync(string provider);
}

// src/LLMGateway.Core/Interfaces/ITokenUsageService.cs
namespace LLMGateway.Core.Interfaces;

using LLMGateway.Core.Models;

public interface ITokenUsageService
{
    Task TrackTokenUsageAsync(TokenUsageInfo usage);
    Task<TokenUsageReport> GetTokenUsageAsync(DateTime startDate, DateTime endDate);
    Task<TokenUsageReport> GetTokenUsageByUserAsync(string userId, DateTime startDate, DateTime endDate);
    Task<TokenUsageReport> GetTokenUsageByModelAsync(string modelId, DateTime startDate, DateTime endDate);
    Task<TokenUsageReport> GetTokenUsageByProviderAsync(string provider, DateTime startDate, DateTime endDate);
}

// src/LLMGateway.Core/Models/ModelInfo.cs
namespace LLMGateway.Core.Models;

public class ModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public ModelCapabilities Capabilities { get; set; } = new();
    public string ProviderModelId { get; set; } = string.Empty;
    public int? ContextWindow { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class ModelCapabilities
{
    public bool SupportsCompletion { get; set; }
    public bool SupportsEmbedding { get; set; }
    public bool SupportsStreaming { get; set; }
    public bool SupportsFunctionCalling { get; set; }
    public bool SupportsVision { get; set; }
    public bool SupportsJSON { get; set; }
}

// src/LLMGateway.Core/Models/TokenUsageInfo.cs
namespace LLMGateway.Core.Models;

public class TokenUsageInfo
{
    public string UserId { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// src/LLMGateway.Core/Models/TokenUsageReport.cs
namespace LLMGateway.Core.Models;

public class TokenUsageReport
{
    public int TotalPromptTokens { get; set; }
    public int TotalCompletionTokens { get; set; }
    public int TotalTokens => TotalPromptTokens + TotalCompletionTokens;
    public Dictionary<string, ProviderUsage> UsageByProvider { get; set; } = new();
    public Dictionary<string, ModelUsage> UsageByModel { get; set; } = new();
    public Dictionary<string, UserUsage> UsageByUser { get; set; } = new();
    public Dictionary<string, int> UsageByRequestType { get; set; } = new();
    public List<DailyUsage> DailyUsage { get; set; } = new();
}

public class ProviderUsage
{
    public string Provider { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}

public class ModelUsage
{
    public string ModelId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}

public class UserUsage
{
    public string UserId { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public Dictionary<string, int> TokensByModel { get; set; } = new();
}

public class DailyUsage
{
    public DateTime Date { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}

// src/LLMGateway.Core/Models/Requests/CompletionRequest.cs
namespace LLMGateway.Core.Models.Requests;

using System.Text.Json.Serialization;

public class CompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new();
    
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }
    
    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }
    
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
    
    [JsonPropertyName("frequency_penalty")]
    public float? FrequencyPenalty { get; set; }
    
    [JsonPropertyName("presence_penalty")]
    public float? PresencePenalty { get; set; }
    
    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }
    
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
    
    [JsonPropertyName("logit_bias")]
    public Dictionary<string, int>? LogitBias { get; set; }
    
    [JsonPropertyName("user")]
    public string? User { get; set; }
    
    [JsonPropertyName("tools")]
    public List<Tool>? Tools { get; set; }
    
    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }
    
    [JsonPropertyName("response_format")]
    public ResponseFormat? ResponseFormat { get; set; }
    
    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }
    
    // Extra options that will be passed directly to specific providers
    [JsonPropertyName("provider_options")]
    public Dictionary<string, object>? ProviderOptions { get; set; }
}

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
}

public class Tool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public FunctionDefinition Function { get; set; } = new();
}

public class FunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } = new();
}

public class ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public FunctionCall Function { get; set; } = new();
}

public class FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

public class ResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
}

// src/LLMGateway.Core/Models/Requests/EmbeddingRequest.cs
namespace LLMGateway.Core.Models.Requests;

using System.Text.Json.Serialization;

public class EmbeddingRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("input")]
    public object Input { get; set; } = new();
    
    [JsonPropertyName("encoding_format")]
    public string? EncodingFormat { get; set; }
    
    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; }
    
    [JsonPropertyName("user")]
    public string? User { get; set; }
    
    // Extra options that will be passed directly to specific providers
    [JsonPropertyName("provider_options")]
    public Dictionary<string, object>? ProviderOptions { get; set; }
}

// src/LLMGateway.Core/Models/Responses/CompletionResponse.cs
namespace LLMGateway.Core.Models.Responses;

using System.Text.Json.Serialization;

public class CompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion";
    
    [JsonPropertyName("created")]
    public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public List<CompletionChoice> Choices { get; set; } = new();
    
    [JsonPropertyName("usage")]
    public UsageInfo Usage { get; set; } = new();
    
    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }
}

public class CompletionChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("message")]
    public MessageResponse Message { get; set; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class MessageResponse
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
}

public class UsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens => PromptTokens + CompletionTokens;
}

// src/LLMGateway.Core/Models/Responses/CompletionChunk.cs
namespace LLMGateway.Core.Models.Responses;

using System.Text.Json.Serialization;

public class CompletionChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion.chunk";
    
    [JsonPropertyName("created")]
    public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public List<ChunkChoice> Choices { get; set; } = new();
}

public class ChunkChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("delta")]
    public DeltaMessage Delta { get; set; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class DeltaMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
}

// src/LLMGateway.Core/Models/Responses/EmbeddingResponse.cs
namespace LLMGateway.Core.Models.Responses;

using System.Text.Json.Serialization;

public class EmbeddingResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = "list";
    
    [JsonPropertyName("data")]
    public List<EmbeddingData> Data { get; set; } = new();
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;
    
    [JsonPropertyName("usage")]
    public UsageInfo Usage { get; set; } = new();
}

public class EmbeddingData
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = "embedding";
    
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; } = new();
}

// src/LLMGateway.Core/Options/ApiKeyOptions.cs
namespace LLMGateway.Core.Options;

public class ApiKeyOptions
{
    public List<ApiKeyInfo> ApiKeys { get; set; } = new();
}

public class ApiKeyInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public Dictionary<string, int>? TokenLimits { get; set; }
}

// src/LLMGateway.Core/Options/FallbackOptions.cs
namespace LLMGateway.Core.Options;

public class FallbackOptions
{
    public bool EnableFallbacks { get; set; } = true;
    public int MaxFallbackAttempts { get; set; } = 3;
    public List<FallbackRule> Rules { get; set; } = new();
}

public class FallbackRule
{
    public string ModelId { get; set; } = string.Empty;
    public List<string> FallbackModels { get; set; } = new();
    public List<string> ErrorCodes { get; set; } = new();
}

// src/LLMGateway.Core/Options/GlobalOptions.cs
namespace LLMGateway.Core.Options;

public class GlobalOptions
{
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 60;
    public bool TrackTokenUsage { get; set; } = true;
    public bool EnableProviderDiscovery { get; set; } = true;
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int DefaultStreamTimeoutSeconds { get; set; } = 120;
}

// src/LLMGateway.Core/Options/JwtOptions.cs
namespace LLMGateway.Core.Options;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}

// src/LLMGateway.Core/Options/LLMRoutingOptions.cs
namespace LLMGateway.Core.Options;

public class LLMRoutingOptions
{
    public bool UseDynamicRouting { get; set; } = true;
    public List<ModelMapping> ModelMappings { get; set; } = new();
}

public class ModelMapping
{
    public string ModelId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderModelId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public int? ContextWindow { get; set; }
    public Dictionary<string, string>? Properties { get; set; }
}

// src/LLMGateway.Core/Options/LoggingOptions.cs
namespace LLMGateway.Core.Options;

public class LoggingOptions
{
    public bool LogRequests { get; set; } = true;
    public bool LogResponses { get; set; } = true;
    public bool LogTokenUsage { get; set; } = true;
    public int MaxLoggedContentLength { get; set; } = 1000;
    public List<string> SensitiveParametersToRedact { get; set; } = new();
}

// src/LLMGateway.Core/Options/RateLimitOptions.cs
namespace LLMGateway.Core.Options;

public class RateLimitOptions
{
    public int TokenLimit { get; set; } = 100;
    public int TokensPerPeriod { get; set; } = 10;
    public int ReplenishmentPeriodSeconds { get; set; } = 1;
    public int QueueLimit { get; set; } = 50;
}

// src/LLMGateway.Core/Options/TelemetryOptions.cs
namespace LLMGateway.Core.Options;

public class TelemetryOptions
{
    public bool EnableTelemetry { get; set; } = true;
    public string? ApplicationInsightsConnectionString { get; set; }
    public bool TrackPerformance { get; set; } = true;
    public bool TrackExceptions { get; set; } = true;
    public bool TrackDependencies { get; set; } = true;
    public bool EnrichWithUserInfo { get; set; } = true;
}

// src/LLMGateway.Core/Options/TokenUsageOptions.cs
namespace LLMGateway.Core.Options;

public class TokenUsageOptions
{
    public bool EnableTokenCounting { get; set; } = true;
    public string StorageProvider { get; set; } = "InMemory";
    public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromDays(90);
    public bool EnableAlerts { get; set; } = false;
    public int AlertThresholdPercentage { get; set; } = 80;
}

// src/LLMGateway.Core/Exceptions/NotFoundException.cs
namespace LLMGateway.Core.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException() : base("The requested resource was not found") { }
    
    public NotFoundException(string message) : base(message) { }
    
    public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
    
    public NotFoundException(string resourceType, string resourceId) 
        : base($"The {resourceType} with ID '{resourceId}' was not found") { }
}

// src/LLMGateway.Core/Exceptions/ProviderException.cs
namespace LLMGateway.Core.Exceptions;

public class ProviderException : Exception
{
    public string ProviderName { get; }
    public string? ErrorCode { get; }
    
    public ProviderException(string providerName, string message) 
        : base(message)
    {
        ProviderName = providerName;
    }
    
    public ProviderException(string providerName, string message, string errorCode) 
        : base(message)
    {
        ProviderName = providerName;
        ErrorCode = errorCode;
    }
    
    public ProviderException(string providerName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ProviderName = providerName;
    }
    
    public ProviderException(string providerName, string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ProviderName = providerName;
        ErrorCode = errorCode;
    }
}

// src/LLMGateway.Core/Exceptions/UnauthorizedException.cs
namespace LLMGateway.Core.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base("Unauthorized access") { }
    
    public UnauthorizedException(string message) : base(message) { }
    
    public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
}

// src/LLMGateway.Core/Exceptions/ValidationException.cs
namespace LLMGateway.Core.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]>? Errors { get; }
    
    public ValidationException() : base("One or more validation errors occurred") { }
    
    public ValidationException(string message) : base(message) { }
    
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    
    public ValidationException(string message, IDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}

// src/LLMGateway.Core/Services/CompletionService.cs
namespace LLMGateway.Core.Services;

using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

public class CompletionService : ICompletionService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IOptions<FallbackOptions> _fallbackOptions;
    private readonly IOptions<GlobalOptions> _globalOptions;
    private readonly ILogger<CompletionService> _logger;
    
    public CompletionService(
        ILLMProviderFactory providerFactory,
        ITokenUsageService tokenUsageService,
        IOptions<FallbackOptions> fallbackOptions,
        IOptions<GlobalOptions> globalOptions,
        ILogger<CompletionService> logger)
    {
        _providerFactory = providerFactory;
        _tokenUsageService = tokenUsageService;
        _fallbackOptions = fallbackOptions;
        _globalOptions = globalOptions;
        _logger = logger;
    }
    
    public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request, string userId)
    {
        ValidateRequest(request);
        
        var provider = _providerFactory.GetProviderForModel(request.Model);
        
        if (provider == null)
        {
            throw new NotFoundException("model", request.Model);
        }
        
        try
        {
            var response = await provider.CreateCompletionAsync(request);
            
            // Track token usage
            if (_globalOptions.Value.TrackTokenUsage)
            {
                await TrackTokenUsage(response, provider.ProviderName, request.Model, userId);
            }
            
            return response;
        }
        catch (ProviderException ex)
        {
            return await HandleProviderExceptionAsync(ex, request, userId);
        }
    }
    
    public async IAsyncEnumerable<CompletionChunk> StreamCompletionAsync(
        CompletionRequest request, 
        string userId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        
        request.Stream = true;
        
        var provider = _providerFactory.GetProviderForModel(request.Model);
        
        if (provider == null)
        {
            throw new NotFoundException("model", request.Model);
        }
        
        // Set up token counting
        int promptTokens = 0;
        int completionTokens = 0;
        
        if (_globalOptions.Value.TrackTokenUsage)
        {
            // Calculate prompt tokens
            promptTokens = CalculatePromptTokens(request, provider);
        }
        
        try
        {
            var streamingResponse = provider.StreamCompletionAsync(request);
            
            await foreach (var chunk in streamingResponse.WithCancellation(cancellationToken))
            {
                if (_globalOptions.Value.TrackTokenUsage && chunk.Choices.Any(c => !string.IsNullOrEmpty(c.Delta.Content)))
                {
                    // Count completion tokens in each chunk
                    completionTokens += CountTokensInChunk(chunk, provider, request.Model);
                }
                
                yield return chunk;
            }
            
            // After streaming is complete, track token usage
            if (_globalOptions.Value.TrackTokenUsage)
            {
                await _tokenUsageService.TrackTokenUsageAsync(new TokenUsageInfo
                {
                    UserId = userId,
                    ModelId = request.Model,
                    Provider = provider.ProviderName,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens,
                    RequestType = "streaming_completion",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (ProviderException ex)
        {
            _logger.LogError(ex, "Error from provider {ProviderName} when streaming completion: {ErrorMessage}",
                provider.ProviderName, ex.Message);
            
            if (_fallbackOptions.Value.EnableFallbacks)
            {
                var fallbackModel = GetFallbackModel(request.Model, ex.ErrorCode);
                
                if (fallbackModel != null)
                {
                    _logger.LogInformation("Falling back to model {FallbackModel} after error from {OriginalModel}",
                        fallbackModel, request.Model);
                    
                    request.Model = fallbackModel;
                    
                    await foreach (var chunk in StreamCompletionAsync(request, userId, cancellationToken))
                    {
                        yield return chunk;
                    }
                    
                    yield break;
                }
            }
            
            throw;
        }
    }
    
    private void ValidateRequest(CompletionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Model))
        {
            throw new ValidationException("The model parameter is required");
        }
        
        if (request.Messages == null || !request.Messages.Any())
        {
            throw new ValidationException("At least one message is required");
        }
        
        if (request.Temperature.HasValue && (request.Temperature < 0 || request.Temperature > 2))
        {
            throw new ValidationException("Temperature must be between 0 and 2");
        }
        
        if (request.TopP.HasValue && (request.TopP < 0 || request.TopP > 1))
        {
            throw new ValidationException("TopP must be between 0 and 1");
        }
    }
    
    private async Task<CompletionResponse> HandleProviderExceptionAsync(
        ProviderException ex, 
        CompletionRequest request, 
        string userId)
    {
        _logger.LogError(ex, "Error from provider {ProviderName} when creating completion: {ErrorMessage}",
            ex.ProviderName, ex.Message);
        
        if (_fallbackOptions.Value.EnableFallbacks)
        {
            var fallbackModel = GetFallbackModel(request.Model, ex.ErrorCode);
            
            if (fallbackModel != null)
            {
                _logger.LogInformation("Falling back to model {FallbackModel} after error from {OriginalModel}",
                    fallbackModel, request.Model);
                
                request.Model = fallbackModel;
                return await CreateCompletionAsync(request, userId);
            }
        }
        
        throw;
    }
    
    private string? GetFallbackModel(string modelId, string? errorCode)
    {
        var fallbackRules = _fallbackOptions.Value.Rules;
        
        // Find matching rule for this model
        var rule = fallbackRules.FirstOrDefault(r => r.ModelId == modelId);
        
        if (rule != null && rule.FallbackModels.Any())
        {
            // If error code specific fallbacks are defined and we have an error code
            if (errorCode != null && rule.ErrorCodes.Any())
            {
                // Only apply fallback if this error code matches
                if (rule.ErrorCodes.Contains(errorCode))
                {
                    return rule.FallbackModels.First();
                }
            }
            else
            {
                // Apply fallback for any error
                return rule.FallbackModels.First();
            }
        }
        
        return null;
    }
    
    private async Task TrackTokenUsage(
        CompletionResponse response,
        string providerName,
        string modelId,
        string userId)
    {
        await _tokenUsageService.TrackTokenUsageAsync(new TokenUsageInfo
        {
            UserId = userId,
            ModelId = modelId,
            Provider = providerName,
            PromptTokens = response.Usage.PromptTokens,
            CompletionTokens = response.Usage.CompletionTokens,
            RequestType = "completion",
            Timestamp = DateTime.UtcNow
        });
    }
    
    private int CalculatePromptTokens(CompletionRequest request, ILLMProvider provider)
    {
        int totalTokens = 0;
        
        foreach (var message in request.Messages)
        {
            totalTokens += provider.CalculateTokenCount(message.Content, request.Model);
        }
        
        return totalTokens;
    }
    
    private int CountTokensInChunk(CompletionChunk chunk, ILLMProvider provider, string modelId)
    {
        int totalTokens = 0;
        
        foreach (var choice in chunk.Choices)
        {
            if (!string.IsNullOrEmpty(choice.Delta.Content))
            {
                totalTokens += provider.CalculateTokenCount(choice.Delta.Content, modelId);
            }
        }
        
        return totalTokens;
    }
}

// src/LLMGateway.Core/Services/EmbeddingService.cs
namespace LLMGateway.Core.Services;

using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class EmbeddingService : IEmbeddingService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IOptions<GlobalOptions> _globalOptions;
    private readonly ILogger<EmbeddingService> _logger;
    
    public EmbeddingService(
        ILLMProviderFactory providerFactory,
        ITokenUsageService tokenUsageService,
        IOptions<GlobalOptions> globalOptions,
        ILogger<EmbeddingService> logger)
    {
        _providerFactory = providerFactory;
        _tokenUsageService = tokenUsageService;
        _globalOptions = globalOptions;
        _logger = logger;
    }
    
    public async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, string userId)
    {
        ValidateRequest(request);
        
        var provider = _providerFactory.GetProviderForModel(request.Model);
        
        if (provider == null)
        {
            throw new NotFoundException("model", request.Model);
        }
        
        try
        {
            var response = await provider.CreateEmbeddingAsync(request);
            
            // Track token usage
            if (_globalOptions.Value.TrackTokenUsage)
            {
                await _tokenUsageService.TrackTokenUsageAsync(new TokenUsageInfo
                {
                    UserId = userId,
                    ModelId = request.Model,
                    Provider = provider.ProviderName,
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = 0,
                    RequestType = "embedding",
                    Timestamp = DateTime.UtcNow
                });
            }
            
            return response;
        }
        catch (ProviderException ex)
        {
            _logger.LogError(ex, "Error from provider {ProviderName} when creating embedding: {ErrorMessage}",
                ex.ProviderName, ex.Message);
            throw;
        }
    }
    
    private void ValidateRequest(EmbeddingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Model))
        {
            throw new ValidationException("The model parameter is required");
        }
        
        if (request.Input == null)
        {
            throw new ValidationException("The input parameter is required");
        }
        
        // Check if input is a string or array of strings
        bool isValidInput = false;
        
        if (request.Input is string)
        {
            isValidInput = true;
        }
        else if (request.Input is IEnumerable<string>)
        {
            isValidInput = true;
        }
        else if (request.Input is object[] array && array.All(item => item is string))
        {
            isValidInput = true;
        }
        
        if (!isValidInput)
        {
            throw new ValidationException("Input must be a string or an array of strings");
        }
    }
}

// src/LLMGateway.Core/Services/ModelService.cs
namespace LLMGateway.Core.Services;

using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using Microsoft.Extensions.Logging;

public class ModelService : IModelService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILogger<ModelService> _logger;
    
    public ModelService(
        ILLMProviderFactory providerFactory,
        ILogger<ModelService> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }
    
    public async Task<List<ModelInfo>> GetAvailableModelsAsync()
    {
        var allModels = new List<ModelInfo>();
        var providers = _providerFactory.GetAllProviders();
        
        foreach (var provider in providers)
        {
            try
            {
                var providerModels = await provider.GetAvailableModelsAsync();
                allModels.AddRange(providerModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving models from provider {ProviderName}: {ErrorMessage}",
                    provider.ProviderName, ex.Message);
            }
        }
        
        return allModels;
    }
    
    public async Task<ModelInfo> GetModelByIdAsync(string id)
    {
        var provider = _providerFactory.GetProviderForModel(id);
        
        if (provider == null)
        {
            throw new NotFoundException("model", id);
        }
        
        var models = await provider.GetAvailableModelsAsync();
        var model = models.FirstOrDefault(m => m.Id == id);
        
        if (model == null)
        {
            throw new NotFoundException("model", id);
        }
        
        return model;
    }
    
    public async Task<List<ModelInfo>> GetModelsByProviderAsync(string provider)
    {
        var providerInstance = _providerFactory.GetProvider(provider);
        
        if (providerInstance == null)
        {
            throw new NotFoundException("provider", provider);
        }
        
        return await providerInstance.GetAvailableModelsAsync();
    }
}

// src/LLMGateway.Core/Services/TokenUsageService.cs
namespace LLMGateway.Core.Services;

using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

public class TokenUsageService : ITokenUsageService
{
    private readonly ConcurrentBag<TokenUsageInfo> _usageRecords = new();
    private readonly ILogger<TokenUsageService> _logger;
    
    public TokenUsageService(ILogger<TokenUsageService> logger)
    {
        _logger = logger;
    }
    
    public Task TrackTokenUsageAsync(TokenUsageInfo usage)
    {
        _usageRecords.Add(usage);
        _logger.LogDebug("Tracked token usage: {PromptTokens} prompt, {CompletionTokens} completion for model {ModelId}",
            usage.PromptTokens, usage.CompletionTokens, usage.ModelId);
        
        return Task.CompletedTask;
    }
    
    public Task<TokenUsageReport> GetTokenUsageAsync(DateTime startDate, DateTime endDate)
    {
        var filteredRecords = _usageRecords
            .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
            .ToList();
        
        var report = GenerateReport(filteredRecords);
        
        return Task.FromResult(report);
    }
    
    public Task<TokenUsageReport> GetTokenUsageByUserAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var filteredRecords = _usageRecords
            .Where(r => r.UserId == userId && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .ToList();
        
        var report = GenerateReport(filteredRecords);
        
        return Task.FromResult(report);
    }
    
    public Task<TokenUsageReport> GetTokenUsageByModelAsync(string modelId, DateTime startDate, DateTime endDate)
    {
        var filteredRecords = _usageRecords
            .Where(r => r.ModelId == modelId && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .ToList();
        
        var report = GenerateReport(filteredRecords);
        
        return Task.FromResult(report);
    }
    
    public Task<TokenUsageReport> GetTokenUsageByProviderAsync(string provider, DateTime startDate, DateTime endDate)
    {
        var filteredRecords = _usageRecords
            .Where(r => r.Provider == provider && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .ToList();
        
        var report = GenerateReport(filteredRecords);
        
        return Task.FromResult(report);
    }
    
    private TokenUsageReport GenerateReport(List<TokenUsageInfo> records)
    {
        var report = new TokenUsageReport
        {
            TotalPromptTokens = records.Sum(r => r.PromptTokens),
            TotalCompletionTokens = records.Sum(r => r.CompletionTokens)
        };
        
        // Generate usage by provider
        foreach (var providerGroup in records.GroupBy(r => r.Provider))
        {
            report.UsageByProvider[providerGroup.Key] = new ProviderUsage
            {
                Provider = providerGroup.Key,
                PromptTokens = providerGroup.Sum(r => r.PromptTokens),
                CompletionTokens = providerGroup.Sum(r => r.CompletionTokens)
            };
        }
        
        // Generate usage by model
        foreach (var modelGroup in records.GroupBy(r => r.ModelId))
        {
            report.UsageByModel[modelGroup.Key] = new ModelUsage
            {
                ModelId = modelGroup.Key,
                Provider = modelGroup.First().Provider,
                PromptTokens = modelGroup.Sum(r => r.PromptTokens),
                CompletionTokens = modelGroup.Sum(r => r.CompletionTokens)
            };
        }
        
        // Generate usage by user
        foreach (var userGroup in records.GroupBy(r => r.UserId))
        {
            var userUsage = new UserUsage
            {
                UserId = userGroup.Key,
                PromptTokens = userGroup.Sum(r => r.PromptTokens),
                CompletionTokens = userGroup.Sum(r => r.CompletionTokens)
            };
            
            // Calculate tokens by model for this user
            foreach (var modelGroup in userGroup.GroupBy(r => r.ModelId))
            {
                userUsage.TokensByModel[modelGroup.Key] = 
                    modelGroup.Sum(r => r.PromptTokens + r.CompletionTokens);
            }
            
            report.UsageByUser[userGroup.Key] = userUsage;
        }
        
        // Generate usage by request type
        foreach (var requestTypeGroup in records.GroupBy(r => r.RequestType))
        {
            report.UsageByRequestType[requestTypeGroup.Key] = 
                requestTypeGroup.Sum(r => r.PromptTokens + r.CompletionTokens);
        }
        
        // Generate daily usage
        foreach (var dateGroup in records.GroupBy(r => r.Timestamp.Date))
        {
            report.DailyUsage.Add(new DailyUsage
            {
                Date = dateGroup.Key,
                PromptTokens = dateGroup.Sum(r => r.PromptTokens),
                CompletionTokens = dateGroup.Sum(r => r.CompletionTokens)
            });
        }
        
        // Sort daily usage by date
        report.DailyUsage = report.DailyUsage.OrderBy(d => d.Date).ToList();
        
        return report;
    }
}

// src/LLMGateway.Providers/Factory/LLMProviderFactory.cs
namespace LLMGateway.Providers.Factory;

using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<LLMRoutingOptions> _routingOptions;
    private readonly ILogger<LLMProviderFactory> _logger;
    private readonly ConcurrentDictionary<string, ILLMProvider> _providers = new();
    
    public LLMProviderFactory(
        IServiceProvider serviceProvider,
        IOptions<LLMRoutingOptions> routingOptions,
        ILogger<LLMProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _routingOptions = routingOptions;
        _logger = logger;
        
        InitializeProviders();
    }
    
    public ILLMProvider GetProvider(string providerName)
    {
        if (_providers.TryGetValue(providerName.ToLowerInvariant(), out var provider))
        {
            return provider;
        }
        
        _logger.LogWarning("Provider not found: {ProviderName}", providerName);
        return null;
    }
    
    public ILLMProvider GetProviderForModel(string modelId)
    {
        // Check explicit mappings first
        var mapping = _routingOptions.Value.ModelMappings
            .FirstOrDefault(m => m.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
        
        if (mapping != null)
        {
            return GetProvider(mapping.ProviderName);
        }
        
        // Check if any provider can handle this model
        foreach (var provider in _providers.Values)
        {
            if (provider.SupportsModel(modelId))
            {
                return provider;
            }
        }
        
        // Try to infer provider from model ID pattern
        var inferredProvider = InferProviderFromModelId(modelId);
        if (inferredProvider != null)
        {
            return inferredProvider;
        }
        
        _logger.LogWarning("No provider found for model: {ModelId}", modelId);
        return null;
    }
    
    public IEnumerable<ILLMProvider> GetAllProviders()
    {
        return _providers.Values;
    }
    
    private void InitializeProviders()
    {
        try
        {
            // Get all provider implementations from DI
            var providers = _serviceProvider
                .GetServices(typeof(ILLMProvider))
                .Cast<ILLMProvider>()
                .ToList();
            
            foreach (var provider in providers)
            {
                _providers[provider.ProviderName.ToLowerInvariant()] = provider;
                _logger.LogInformation("Registered LLM provider: {ProviderName}", provider.ProviderName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing LLM providers");
        }
    }
    
    private ILLMProvider InferProviderFromModelId(string modelId)
    {
        modelId = modelId.ToLowerInvariant();
        
        if (modelId.StartsWith("openai.") || 
            modelId.StartsWith("gpt-") || 
            modelId.StartsWith("text-embedding-") ||
            modelId.StartsWith("text-davinci-"))
        {
            return GetProvider("openai");
        }
        
        if (modelId.StartsWith("anthropic.") || 
            modelId.StartsWith("claude-"))
        {
            return GetProvider("anthropic");
        }
        
        if (modelId.StartsWith("cohere.") || 
            modelId.StartsWith("command-"))
        {
            return GetProvider("cohere");
        }
        
        if (modelId.StartsWith("huggingface.") || 
            modelId.Contains("/"))
        {
            return GetProvider("huggingface");
        }
        
        return null;
    }
}

// src/LLMGateway.Providers/OpenAI/OpenAIProvider.cs
namespace LLMGateway.Providers.OpenAI;

using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class OpenAIProvider : ILLMProvider
{
    public string ProviderName => "OpenAI";
    
    private readonly HttpClient _httpClient;
    private readonly IOptions<OpenAIOptions> _options;
    private readonly ILogger<OpenAIProvider> _logger;
    
    private readonly string _apiUrlBase;
    
    public OpenAIProvider(
        HttpClient httpClient,
        IOptions<OpenAIOptions> options,
        ILogger<OpenAIProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        
        _apiUrlBase = _options.Value.ApiUrl.TrimEnd('/');
        
        ConfigureHttpClient();
    }
    
    public async Task<List<ModelInfo>> GetAvailableModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<OpenAIListModelsResponse>($"{_apiUrlBase}/models");
            
            if (response == null || response.Data == null)
            {
                return new List<ModelInfo>();
            }
            
            return response.Data.Select(ConvertToModelInfo).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OpenAI models: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to retrieve available models", ex);
        }
    }
    
    public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request)
    {
        try
        {
            // Convert the request to OpenAI format
            var openAIRequest = ConvertToOpenAIRequest(request);
            
            var content = new StringContent(
                JsonSerializer.Serialize(openAIRequest, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiUrlBase}/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                var error = ParseErrorResponse(errorContent);
                throw new ProviderException(ProviderName, error.Message, error.Code, GetInnerException(error));
            }
            
            var openAIResponse = await response.Content.ReadFromJsonAsync<OpenAICompletionResponse>();
            
            if (openAIResponse == null)
            {
                throw new ProviderException(ProviderName, "Empty response received from OpenAI API");
            }
            
            return ConvertToCompletionResponse(openAIResponse, request.Model);
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating completion with OpenAI: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to create completion", ex);
        }
    }
    
    public async IAsyncEnumerable<CompletionChunk> StreamCompletionAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert the request to OpenAI format
            var openAIRequest = ConvertToOpenAIRequest(request);
            openAIRequest.Stream = true;
            
            var content = new StringContent(
                JsonSerializer.Serialize(openAIRequest, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiUrlBase}/chat/completions", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OpenAI API error: {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                var error = ParseErrorResponse(errorContent);
                throw new ProviderException(ProviderName, error.Message, error.Code, GetInnerException(error));
            }
            
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            
            string line;
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                if (!line.StartsWith("data: "))
                {
                    continue;
                }
                
                var data = line.Substring("data: ".Length).Trim();
                
                // Check for the [DONE] message that indicates the end of the stream
                if (data == "[DONE]")
                {
                    break;
                }
                
                try
                {
                    var chunkResponse = JsonSerializer.Deserialize<OpenAICompletionResponse>(data);
                    
                    if (chunkResponse != null)
                    {
                        yield return ConvertToCompletionChunk(chunkResponse, request.Model);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error parsing OpenAI streaming response: {ErrorMessage}", ex.Message);
                    // Skip malformed chunks and continue processing
                }
            }
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming completion with OpenAI: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to stream completion", ex);
        }
    }
    
    public async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request)
    {
        try
        {
            // Convert the request to OpenAI format
            var openAIRequest = new OpenAIEmbeddingRequest
            {
                Model = GetProviderModelId(request.Model),
                Input = request.Input,
                EncodingFormat = request.EncodingFormat,
                Dimensions = request.Dimensions,
                User = request.User
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(openAIRequest, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiUrlBase}/embeddings", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                var error = ParseErrorResponse(errorContent);
                throw new ProviderException(ProviderName, error.Message, error.Code, GetInnerException(error));
            }
            
            var openAIResponse = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>();
            
            if (openAIResponse == null)
            {
                throw new ProviderException(ProviderName, "Empty response received from OpenAI API");
            }
            
            return ConvertToEmbeddingResponse(openAIResponse, request.Model);
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating embedding with OpenAI: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to create embedding", ex);
        }
    }
    
    public bool SupportsModel(string modelId)
    {
        // Check our model mapping first
        foreach (var modelMapping in _options.Value.ModelMappings)
        {
            if (modelId.Equals(modelMapping.ModelId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        // Check if this follows OpenAI model naming conventions
        return modelId.StartsWith("openai.") || 
               modelId.StartsWith("gpt-") || 
               modelId.StartsWith("text-embedding-") ||
               modelId.StartsWith("text-davinci-") ||
               modelId.StartsWith("dall-e-");
    }
    
    public int CalculateTokenCount(string text, string modelId)
    {
        // Simple approximation: ~4 chars per token
        // In a production implementation, this would use proper tokenizers
        return text.Length / 4 + 1;
    }
    
    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var apiKey = _options.Value.ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        
        var orgId = _options.Value.OrganizationId;
        if (!string.IsNullOrEmpty(orgId))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", orgId);
        }
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.Value.TimeoutSeconds);
    }
    
    private ModelInfo ConvertToModelInfo(OpenAIModel model)
    {
        var modelId = model.Id;
        
        // Check if we have a mapping for this model
        var customMapping = _options.Value.ModelMappings
            .FirstOrDefault(m => m.ProviderModelId.Equals(model.Id, StringComparison.OrdinalIgnoreCase));
        
        if (customMapping != null)
        {
            modelId = customMapping.ModelId;
        }
        else if (!modelId.StartsWith("openai."))
        {
            modelId = $"openai.{modelId}";
        }
        
        var capabilities = new ModelCapabilities
        {
            SupportsCompletion = model.Id.Contains("gpt") || model.Id.Contains("davinci"),
            SupportsEmbedding = model.Id.Contains("embedding"),
            SupportsStreaming = model.Id.Contains("gpt") || model.Id.Contains("davinci"),
            SupportsFunctionCalling = model.Id.Contains("gpt-4") || model.Id.Contains("gpt-3.5"),
            SupportsVision = model.Id.Contains("vision") || model.Id.Contains("gpt-4-vision") || model.Id.Contains("gpt-4-turbo"),
            SupportsJSON = model.Id.Contains("gpt-4") || model.Id.Contains("gpt-3.5-turbo")
        };
        
        return new ModelInfo
        {
            Id = modelId,
            DisplayName = customMapping?.DisplayName ?? model.Id,
            Provider = ProviderName,
            Capabilities = capabilities,
            ProviderModelId = model.Id,
            ContextWindow = GetContextWindowForModel(model.Id),
            Properties = new Dictionary<string, string>
            {
                ["owned_by"] = model.OwnedBy
            }
        };
    }
    
    private int? GetContextWindowForModel(string modelId)
    {
        // Lookup context window size based on model ID
        if (modelId.Contains("gpt-4-turbo"))
            return 128000;
        if (modelId.Contains("gpt-4-32k"))
            return 32768;
        if (modelId.Contains("gpt-4"))
            return 8192;
        if (modelId.Contains("gpt-3.5-turbo-16k"))
            return 16384;
        if (modelId.Contains("gpt-3.5"))
            return 4096;
        
        return null;
    }
    
    private string GetProviderModelId(string modelId)
    {
        // Remove provider prefix if present
        if (modelId.StartsWith("openai."))
        {
            modelId = modelId.Substring("openai.".Length);
        }
        
        // Check if we have a mapping for this model
        var customMapping = _options.Value.ModelMappings
            .FirstOrDefault(m => m.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
        
        return customMapping?.ProviderModelId ?? modelId;
    }
    
    private OpenAICompletionRequest ConvertToOpenAIRequest(CompletionRequest request)
    {
        // Convert to provider-specific model ID
        string providerModelId = GetProviderModelId(request.Model);
        
        // Map messages
        var messages = request.Messages.Select(m => new OpenAIMessage
        {
            Role = m.Role,
            Content = m.Content,
            Name = m.Name,
            FunctionCall = m.ToolCalls?.FirstOrDefault()?.Function != null ? 
                new OpenAIFunctionCall
                {
                    Name = m.ToolCalls.First().Function.Name,
                    Arguments = m.ToolCalls.First().Function.Arguments
                } : null
        }).ToList();
        
        // Map functions to tools if present
        List<OpenAITool>? tools = null;
        
        if (request.Tools != null && request.Tools.Any())
        {
            tools = request.Tools.Select(t => new OpenAITool
            {
                Type = t.Type,
                Function = new OpenAIFunctionDefinition
                {
                    Name = t.Function.Name,
                    Description = t.Function.Description,
                    Parameters = t.Function.Parameters
                }
            }).ToList();
        }
        
        // Create OpenAI request
        return new OpenAICompletionRequest
        {
            Model = providerModelId,
            Messages = messages,
            Temperature = request.Temperature,
            TopP = request.TopP,
            MaxTokens = request.MaxTokens,
            FrequencyPenalty = request.FrequencyPenalty,
            PresencePenalty = request.PresencePenalty,
            Stop = request.Stop,
            Stream = request.Stream,
            Functions = null,  // Deprecated in favor of tools
            FunctionCall = null,  // Deprecated in favor of tool_choice
            Tools = tools,
            ToolChoice = request.ToolChoice,
            ResponseFormat = request.ResponseFormat != null ? new OpenAIResponseFormat
            {
                Type = request.ResponseFormat.Type
            } : null,
            User = request.User
        };
    }
    
    private CompletionResponse ConvertToCompletionResponse(OpenAICompletionResponse response, string requestedModel)
    {
        return new CompletionResponse
        {
            Id = response.Id,
            Object = "chat.completion",
            Created = response.Created,
            Model = requestedModel,
            Provider = ProviderName,
            Choices = response.Choices.Select(c => new CompletionChoice
            {
                Index = c.Index,
                Message = new MessageResponse
                {
                    Role = c.Message.Role,
                    Content = c.Message.Content,
                    ToolCalls = c.Message.ToolCalls?.Select(tc => new ToolCall
                    {
                        Id = tc.Id,
                        Type = tc.Type,
                        Function = new FunctionCall
                        {
                            Name = tc.Function.Name,
                            Arguments = tc.Function.Arguments
                        }
                    }).ToList()
                },
                FinishReason = c.FinishReason
            }).ToList(),
            Usage = new UsageInfo
            {
                PromptTokens = response.Usage.PromptTokens,
                CompletionTokens = response.Usage.CompletionTokens
            },
            SystemFingerprint = response.SystemFingerprint
        };
    }
    
    private CompletionChunk ConvertToCompletionChunk(OpenAICompletionResponse response, string requestedModel)
    {
        return new CompletionChunk
        {
            Id = response.Id,
            Object = "chat.completion.chunk",
            Created = response.Created,
            Model = requestedModel,
            Provider = ProviderName,
            Choices = response.Choices.Select(c => new ChunkChoice
            {
                Index = c.Index,
                Delta = new DeltaMessage
                {
                    Role = c.Delta?.Role,
                    Content = c.Delta?.Content,
                    ToolCalls = c.Delta?.ToolCalls?.Select(tc => new ToolCall
                    {
                        Id = tc.Id,
                        Type = tc.Type,
                        Function = new FunctionCall
                        {
                            Name = tc.Function.Name,
                            Arguments = tc.Function.Arguments
                        }
                    }).ToList()
                },
                FinishReason = c.FinishReason
            }).ToList()
        };
    }
    
    private EmbeddingResponse ConvertToEmbeddingResponse(OpenAIEmbeddingResponse response, string requestedModel)
    {
        return new EmbeddingResponse
        {
            Object = response.Object,
            Data = response.Data.Select(d => new EmbeddingData
            {
                Object = d.Object,
                Index = d.Index,
                Embedding = d.Embedding
            }).ToList(),
            Model = requestedModel,
            Provider = ProviderName,
            Usage = new UsageInfo
            {
                PromptTokens = response.Usage.PromptTokens,
                CompletionTokens = 0
            }
        };
    }
    
    private OpenAIError ParseErrorResponse(string errorContent)
    {
        try
        {
            var errorResponse = JsonSerializer.Deserialize<OpenAIErrorResponse>(errorContent);
            
            if (errorResponse?.Error != null)
            {
                return errorResponse.Error;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OpenAI error response: {ErrorContent}", errorContent);
        }
        
        return new OpenAIError
        {
            Message = "Unknown error occurred",
            Type = "unknown_error",
            Code = "unknown"
        };
    }
    
    private Exception GetInnerException(OpenAIError error)
    {
        return new Exception($"{error.Type}: {error.Message}");
    }
}

// OpenAI API Models
#region OpenAI Models

public class OpenAIModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    [JsonPropertyName("created")]
    public long Created { get; set; }
    
    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; set; } = string.Empty;
}

public class OpenAIListModelsResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public List<OpenAIModel> Data { get; set; } = new();
}

public class OpenAICompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("messages")]
    public List<OpenAIMessage> Messages { get; set; } = new();
    
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }
    
    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }
    
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
    
    [JsonPropertyName("frequency_penalty")]
    public float? FrequencyPenalty { get; set; }
    
    [JsonPropertyName("presence_penalty")]
    public float? PresencePenalty { get; set; }
    
    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }
    
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
    
    [JsonPropertyName("logit_bias")]
    public Dictionary<string, int>? LogitBias { get; set; }
    
    [JsonPropertyName("user")]
    public string? User { get; set; }
    
    // Deprecated in favor of tools
    [JsonPropertyName("functions")]
    public List<OpenAIFunctionDefinition>? Functions { get; set; }
    
    // Deprecated in favor of tool_choice
    [JsonPropertyName("function_call")]
    public object? FunctionCall { get; set; }
    
    [JsonPropertyName("tools")]
    public List<OpenAITool>? Tools { get; set; }
    
    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }
    
    [JsonPropertyName("response_format")]
    public OpenAIResponseFormat? ResponseFormat { get; set; }
}

public class OpenAIMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("function_call")]
    public OpenAIFunctionCall? FunctionCall { get; set; }
}

public class OpenAIFunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } = new();
}

public class OpenAIFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

public class OpenAITool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public OpenAIFunctionDefinition Function { get; set; } = new();
}

public class OpenAIToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public OpenAIFunctionCall Function { get; set; } = new();
}

public class OpenAIResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
}

public class OpenAIDeltaMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("function_call")]
    public OpenAIFunctionCall? FunctionCall { get; set; }
    
    [JsonPropertyName("tool_calls")]
    public List<OpenAIToolCall>? ToolCalls { get; set; }
}

public class OpenAICompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    [JsonPropertyName("created")]
    public long Created { get; set; }
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public List<OpenAIChoice> Choices { get; set; } = new();
    
    [JsonPropertyName("usage")]
    public OpenAIUsage Usage { get; set; } = new();
    
    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }
}

public class OpenAIChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("message")]
    public OpenAIMessage Message { get; set; } = new();
    
    [JsonPropertyName("delta")]
    public OpenAIDeltaMessage? Delta { get; set; }
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class OpenAIUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class OpenAIEmbeddingRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("input")]
    public object Input { get; set; } = new();
    
    [JsonPropertyName("encoding_format")]
    public string? EncodingFormat { get; set; }
    
    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; }
    
    [JsonPropertyName("user")]
    public string? User { get; set; }
}

public class OpenAIEmbeddingResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public List<OpenAIEmbeddingData> Data { get; set; } = new();
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("usage")]
    public OpenAIUsage Usage { get; set; } = new();
}

public class OpenAIEmbeddingData
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; } = new();
}

public class OpenAIErrorResponse
{
    [JsonPropertyName("error")]
    public OpenAIError? Error { get; set; }
}

public class OpenAIError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("param")]
    public string? Param { get; set; }
    
    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

#endregion

// src/LLMGateway.Providers/OpenAI/OpenAIOptions.cs
namespace LLMGateway.Providers.OpenAI;

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://api.openai.com/v1";
    public string OrganizationId { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public List<ModelMapping> ModelMappings { get; set; } = new();
}

// src/LLMGateway.Providers/Anthropic/AnthropicProvider.cs
namespace LLMGateway.Providers.Anthropic;

using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class AnthropicProvider : ILLMProvider
{
    public string ProviderName => "Anthropic";
    
    private readonly HttpClient _httpClient;
    private readonly IOptions<AnthropicOptions> _options;
    private readonly ILogger<AnthropicProvider> _logger;
    
    private readonly string _apiUrlBase;
    
    public AnthropicProvider(
        HttpClient httpClient,
        IOptions<AnthropicOptions> options,
        ILogger<AnthropicProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        
        _apiUrlBase = _options.Value.ApiUrl.TrimEnd('/');
        
        ConfigureHttpClient();
    }
    
    public Task<List<ModelInfo>> GetAvailableModelsAsync()
    {
        // Anthropic doesn't have a models endpoint, so we use a predefined list
        var models = new List<ModelInfo>();
        
        foreach (var modelMapping in _options.Value.ModelMappings)
        {
            models.Add(new ModelInfo
            {
                Id = modelMapping.ModelId,
                DisplayName = modelMapping.DisplayName ?? modelMapping.ModelId,
                Provider = ProviderName,
                Capabilities = new ModelCapabilities
                {
                    SupportsCompletion = true,
                    SupportsEmbedding = false,
                    SupportsStreaming = true,
                    SupportsFunctionCalling = modelMapping.ProviderModelId.Contains("claude-3"),
                    SupportsVision = modelMapping.ProviderModelId.Contains("claude-3"),
                    SupportsJSON = modelMapping.ProviderModelId.Contains("claude-3")
                },
                ProviderModelId = modelMapping.ProviderModelId,
                ContextWindow = modelMapping.ContextWindow,
                Properties = modelMapping.Properties ?? new Dictionary<string, string>()
            });
        }
        
        return Task.FromResult(models);
    }
    
    public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request)
    {
        try
        {
            // Convert the request to Anthropic format
            var anthropicRequest = ConvertToAnthropicRequest(request);
            
            var content = new StringContent(
                JsonSerializer.Serialize(anthropicRequest, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiUrlBase}/messages", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Anthropic API error: {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                var error = ParseErrorResponse(errorContent);
                throw new ProviderException(ProviderName, error.Message, error.Type, GetInnerException(error));
            }
            
            var anthropicResponse = await response.Content.ReadFromJsonAsync<AnthropicResponse>();
            
            if (anthropicResponse == null)
            {
                throw new ProviderException(ProviderName, "Empty response received from Anthropic API");
            }
            
            return ConvertToCompletionResponse(anthropicResponse, request.Model);
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating completion with Anthropic: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to create completion", ex);
        }
    }
    
    public async IAsyncEnumerable<CompletionChunk> StreamCompletionAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert the request to Anthropic format
            var anthropicRequest = ConvertToAnthropicRequest(request);
            anthropicRequest.Stream = true;
            
            var content = new StringContent(
                JsonSerializer.Serialize(anthropicRequest, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiUrlBase}/messages", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Anthropic API error: {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                var error = ParseErrorResponse(errorContent);
                throw new ProviderException(ProviderName, error.Message, error.Type, GetInnerException(error));
            }
            
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            
            string line;
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                if (!line.StartsWith("data: "))
                {
                    continue;
                }
                
                var data = line.Substring("data: ".Length).Trim();
                
                // Claude sends a [DONE] message at the end of the stream
                if (data == "[DONE]")
                {
                    break;
                }
                
                try
                {
                    var eventResponse = JsonSerializer.Deserialize<AnthropicStreamEvent>(data);
                    
                    if (eventResponse != null && eventResponse.Type == "content_block_delta")
                    {
                        // Convert content block delta to a completion chunk
                        yield return ConvertToCompletionChunk(eventResponse, request.Model);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error parsing Anthropic streaming response: {ErrorMessage}", ex.Message);
                    // Skip malformed chunks and continue processing
                }
            }
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming completion with Anthropic: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to stream completion", ex);
        }
    }
    
    public Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request)
    {
        // Anthropic doesn't currently support embeddings in their API
        throw new ProviderException(ProviderName, "Embedding is not supported by Anthropic");
    }
    
    public bool SupportsModel(string modelId)
    {
        // Check our model mapping first
        foreach (var modelMapping in _options.Value.ModelMappings)
        {
            if (modelId.Equals(modelMapping.ModelId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        // Check if this follows Anthropic model naming conventions
        return modelId.StartsWith("anthropic.") || 
               modelId.StartsWith("claude-");
    }
    
    public int CalculateTokenCount(string text, string modelId)
    {
        // Simple approximation: ~4 chars per token
        // In a production implementation, this would use proper tokenizers
        return text.Length / 4 + 1;
    }
    
    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var apiKey = _options.Value.ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", _options.Value.ApiVersion);
        }
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.Value.TimeoutSeconds);
    }
    
    private string GetProviderModelId(string modelId)
    {
        // Remove provider prefix if present
        if (modelId.StartsWith("anthropic."))
        {
            modelId = modelId.Substring("anthropic.".Length);
        }
        
        // Check if we have a mapping for this model
        var customMapping = _options.Value.ModelMappings
            .FirstOrDefault(m => m.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
        
        return customMapping?.ProviderModelId ?? modelId;
    }
    
    private AnthropicRequest ConvertToAnthropicRequest(CompletionRequest request)
    {
        // Convert to provider-specific model ID
        string providerModelId = GetProviderModelId(request.Model);
        
        // Construct Claude messages
        var messages = new List<AnthropicMessage>();
        string? systemPrompt = null;
        
        foreach (var message in request.Messages)
        {
            if (message.Role == "system")
            {
                systemPrompt = message.Content;
            }
            else
            {
                messages.Add(new AnthropicMessage
                {
                    Role = message.Role == "assistant" ? "assistant" : "user",
                    Content = message.Content
                });
            }
        }
        
        // Map tools to tool use
        List<AnthropicTool>? tools = null;
        
        if (request.Tools != null && request.Tools.Any())
        {
            tools = request.Tools
                .Where(t => t.Type == "function")
                .Select(t => new AnthropicTool
                {
                    Type = "function",
                    Function = new AnthropicFunction
                    {
                        Name = t.Function.Name,
                        Description = t.Function.Description,
                        Parameters = t.Function.Parameters
                    }
                }).ToList();
        }
        
        // Create Anthropic request
        return new AnthropicRequest
        {
            Model = providerModelId,
            Messages = messages,
            System = systemPrompt,
            MaxTokens = request.MaxTokens ?? 1024,
            Temperature = request.Temperature,
            TopP = request.TopP,
            TopK = null,
            StopSequences = request.Stop,
            Stream = request.Stream,
            Tools = tools,
            ToolChoice = request.ToolChoice != null ? "auto" : null,
            Metadata = new Dictionary<string, string>
            {
                ["user_id"] = request.User ?? "unknown"
            }
        };
    }
    
    private CompletionResponse ConvertToCompletionResponse(AnthropicResponse response, string requestedModel)
    {
        var content = string.Empty;
        
        if (response.Content != null && response.Content.Count > 0)
        {
            var textBlocks = response.Content
                .Where(c => c.Type == "text")
                .Select(c => c.Text);
            
            content = string.Join("", textBlocks);
        }
        
        var toolCalls = new List<ToolCall>();
        
        // Extract tool calls if present
        var toolUseBlocks = response.Content?.Where(c => c.Type == "tool_use").ToList();
        
        if (toolUseBlocks != null && toolUseBlocks.Any())
        {
            foreach (var toolUse in toolUseBlocks)
            {
                if (toolUse.Id != null && toolUse.ToolUse != null)
                {
                    toolCalls.Add(new ToolCall
                    {
                        Id = toolUse.Id,
                        Type = "function",
                        Function = new FunctionCall
                        {
                            Name = toolUse.ToolUse.Name,
                            Arguments = toolUse.ToolUse.Input != null ? 
                                JsonSerializer.Serialize(toolUse.ToolUse.Input) : "{}"
                        }
                    });
                }
            }
        }
        
        return new CompletionResponse
        {
            Id = response.Id,
            Object = "chat.completion",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = requestedModel,
            Provider = ProviderName,
            Choices = new List<CompletionChoice>
            {
                new CompletionChoice
                {
                    Index = 0,
                    Message = new MessageResponse
                    {
                        Role = "assistant",
                        Content = content,
                        ToolCalls = toolCalls.Any() ? toolCalls : null
                    },
                    FinishReason = response.StopReason
                }
            },
            Usage = new UsageInfo
            {
                PromptTokens = response.Usage.InputTokens,
                CompletionTokens = response.Usage.OutputTokens
            }
        };
    }
    
    private CompletionChunk ConvertToCompletionChunk(AnthropicStreamEvent eventResponse, string requestedModel)
    {
        var deltaContent = string.Empty;
        List<ToolCall>? toolCalls = null;
        
        if (eventResponse.Delta?.Type == "text" && eventResponse.Delta.Text != null)
        {
            deltaContent = eventResponse.Delta.Text;
        }
        else if (eventResponse.Delta?.Type == "tool_use" && eventResponse.Delta.ToolUse != null)
        {
            // Handle tool use deltas
            toolCalls = new List<ToolCall>
            {
                new ToolCall
                {
                    Id = eventResponse.Delta.Id ?? Guid.NewGuid().ToString(),
                    Type = "function",
                    Function = new FunctionCall
                    {
                        Name = eventResponse.Delta.ToolUse.Name,
                        Arguments = eventResponse.Delta.ToolUse.Input != null ?
                            JsonSerializer.Serialize(eventResponse.Delta.ToolUse.Input) : "{}"
                    }
                }
            };
        }
        
        return new CompletionChunk
        {
            Id = eventResponse.Message?.Id ?? Guid.NewGuid().ToString(),
            Object = "chat.completion.chunk",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = requestedModel,
            Provider = ProviderName,
            Choices = new List<ChunkChoice>
            {
                new ChunkChoice
                {
                    Index = 0,
                    Delta = new DeltaMessage
                    {
                        Role = eventResponse.Index == 0 ? "assistant" : null,
                        Content = string.IsNullOrEmpty(deltaContent) ? null : deltaContent,
                        ToolCalls = toolCalls
                    },
                    FinishReason = eventResponse.StopReason
                }
            }
        };
    }
    
    private AnthropicError ParseErrorResponse(string errorContent)
    {
        try
        {
            var errorResponse = JsonSerializer.Deserialize<AnthropicErrorResponse>(errorContent);
            
            if (errorResponse != null)
            {
                return new AnthropicError
                {
                    Type = errorResponse.Type,
                    Message = errorResponse.Error?.Message ?? errorResponse.Message ?? "Unknown error"
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Anthropic error response: {ErrorContent}", errorContent);
        }
        
        return new AnthropicError
        {
            Type = "unknown_error",
            Message = "Unknown error occurred"
        };
    }
    
    private Exception GetInnerException(AnthropicError error)
    {
        return new Exception($"{error.Type}: {error.Message}");
    }
}

// Anthropic API Models
#region Anthropic Models

public class AnthropicRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("messages")]
    public List<AnthropicMessage> Messages { get; set; } = new();
    
    [JsonPropertyName("system")]
    public string? System { get; set; }
    
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1024;
    
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }
    
    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }
    
    [JsonPropertyName("top_k")]
    public int? TopK { get; set; }
    
    [JsonPropertyName("stop_sequences")]
    public List<string>? StopSequences { get; set; }
    
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
    
    [JsonPropertyName("tools")]
    public List<AnthropicTool>? Tools { get; set; }
    
    [JsonPropertyName("tool_choice")]
    public string? ToolChoice { get; set; }
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

public class AnthropicMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class AnthropicTool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public AnthropicFunction Function { get; set; } = new();
}

public class AnthropicFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } = new();
}

public class AnthropicResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public List<AnthropicContentBlock>? Content { get; set; }
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }
    
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
    
    [JsonPropertyName("usage")]
    public AnthropicUsage Usage { get; set; } = new();
}

public class AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("tool_use")]
    public AnthropicToolUse? ToolUse { get; set; }
}

public class AnthropicToolUse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("input")]
    public object? Input { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class AnthropicUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }
    
    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}

public class AnthropicStreamEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public AnthropicStreamMessage? Message { get; set; }
    
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("content_block")]
    public AnthropicContentBlock? ContentBlock { get; set; }
    
    [JsonPropertyName("delta")]
    public AnthropicDelta? Delta { get; set; }
    
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }
    
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
}

public class AnthropicStreamMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}

public class AnthropicDelta
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("tool_use")]
    public AnthropicToolUse? ToolUse { get; set; }
}

public class AnthropicErrorResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("error")]
    public AnthropicErrorDetail? Error { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class AnthropicErrorDetail
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class AnthropicError
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

#endregion

// src/LLMGateway.Providers/Anthropic/AnthropicOptions.cs
namespace LLMGateway.Providers.Anthropic;

public class AnthropicOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://api.anthropic.com/v1";
    public string ApiVersion { get; set; } = "2023-06-01";
    public int TimeoutSeconds { get; set; } = 120;
    public List<ModelMapping> ModelMappings { get; set; } = new();
}

// src/LLMGateway.Infrastructure/Caching/RedisCacheExtensions.cs
namespace LLMGateway.Infrastructure.Caching;

using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

public static class RedisCacheExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConfig = configuration.GetSection("Redis");
        
        if (redisConfig.Exists())
        {
            var connectionString = redisConfig["ConnectionString"];
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(connectionString));
                
                services.AddSingleton<IRedisCacheService, RedisCacheService>();
            }
            else
            {
                // Fall back to memory cache if Redis is not configured
                services.AddMemoryCache();
                services.AddSingleton<IRedisCacheService, MemoryCacheService>();
            }
        }
        else
        {
            // Fall back to memory cache if Redis is not configured
            services.AddMemoryCache();
            services.AddSingleton<IRedisCacheService, MemoryCacheService>();
        }
        
        return services;
    }
}

// src/LLMGateway.Infrastructure/Caching/IRedisCacheService.cs
namespace LLMGateway.Infrastructure.Caching;

public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}

// src/LLMGateway.Infrastructure/Caching/RedisCacheService.cs
namespace LLMGateway.Infrastructure.Caching;

using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    
    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }
    
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            
            if (value.IsNull)
            {
                return default;
            }
            
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from Redis cache: {ErrorMessage}", ex.Message);
            return default;
        }
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var db = _redis.GetDatabase();
            var serializedValue = JsonSerializer.Serialize(value);
            
            await db.StringSetAsync(key, serializedValue, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving to Redis cache: {ErrorMessage}", ex.Message);
        }
    }
    
    public async Task RemoveAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from Redis cache: {ErrorMessage}", ex.Message);
        }
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence in Redis cache: {ErrorMessage}", ex.Message);
            return false;
        }
    }
}

// src/LLMGateway.Infrastructure/Caching/MemoryCacheService.cs
namespace LLMGateway.Infrastructure.Caching;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class MemoryCacheService : IRedisCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    
    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }
    
    public Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out T? cachedValue))
            {
                return Task.FromResult(cachedValue);
            }
            
            return Task.FromResult<T?>(default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from memory cache: {ErrorMessage}", ex.Message);
            return Task.FromResult<T?>(default);
        }
    }
    
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry;
            }
            
            _memoryCache.Set(key, value, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving to memory cache: {ErrorMessage}", ex.Message);
        }
        
        return Task.CompletedTask;
    }
    
    public Task RemoveAsync(string key)
    {
        try
        {
            _memoryCache.Remove(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from memory cache: {ErrorMessage}", ex.Message);
        }
        
        return Task.CompletedTask;
    }
    
    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(_memoryCache.TryGetValue(key, out _));
    }
}

// src/LLMGateway.Infrastructure/Telemetry/TelemetryExtensions.cs
namespace LLMGateway.Infrastructure.Telemetry;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Options;

public static class TelemetryExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var telemetryOptions = new TelemetryOptions();
        configuration.GetSection("Telemetry").Bind(telemetryOptions);
        
        if (telemetryOptions.EnableTelemetry)
        {
            if (!string.IsNullOrEmpty(telemetryOptions.ApplicationInsightsConnectionString))
            {
                services.AddApplicationInsightsTelemetry(options =>
                {
                    options.ConnectionString = telemetryOptions.ApplicationInsightsConnectionString;
                });
                
                services.AddSingleton<ITelemetryInitializer, LLMGatewayTelemetryInitializer>();
                
                if (!telemetryOptions.TrackDependencies)
                {
                    services.Configure<TelemetryConfiguration>((config) =>
                    {
                        var dependencyModules = config.TelemetryProcessors
                            .Where(x => x.GetType().Name == "DependencyTrackingTelemetryModule")
                            .ToList();
                            
                        foreach (var module in dependencyModules)
                        {
                            config.TelemetryProcessors.Remove(module);
                        }
                    });
                }
            }
            else
            {
                services.AddSingleton<ITelemetryService, ConsoleTelemetryService>();
            }
        }
        else
        {
            services.AddSingleton<ITelemetryService, NullTelemetryService>();
        }
        
        return services;
    }
}

// src/LLMGateway.Infrastructure/Telemetry/LLMGatewayTelemetryInitializer.cs
namespace LLMGateway.Infrastructure.Telemetry;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class LLMGatewayTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public LLMGatewayTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        var context = _httpContextAccessor.HttpContext;
        
        if (context == null)
        {
            return;
        }
        
        // Add user ID if available
        var userId = context.User?.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            telemetry.Context.User.Id = userId;
            telemetry.Context.User.AuthenticatedUserId = userId;
        }
        
        // Add API key ID if available
        var apiKeyId = context.User?.FindFirst("ApiKeyId")?.Value;
        if (!string.IsNullOrEmpty(apiKeyId))
        {
            telemetry.Context.GlobalProperties["ApiKeyId"] = apiKeyId;
        }
        
        // Add additional context info
        telemetry.Context.Cloud.RoleName = "LLMGateway";
        telemetry.Context.Component.Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
    }
}

// src/LLMGateway.Infrastructure/Telemetry/ITelemetryService.cs
namespace LLMGateway.Infrastructure.Telemetry;

public interface ITelemetryService
{
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null);
    void TrackException(Exception exception, IDictionary<string, string>? properties = null);
    void TrackDependency(string dependencyType, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success);
    void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null);
}

// src/LLMGateway.Infrastructure/Telemetry/ConsoleTelemetryService.cs
namespace LLMGateway.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;

public class ConsoleTelemetryService : ITelemetryService
{
    private readonly ILogger<ConsoleTelemetryService> _logger;
    
    public ConsoleTelemetryService(ILogger<ConsoleTelemetryService> logger)
    {
        _logger = logger;
    }
    
    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        _logger.LogInformation("Event: {EventName} - Properties: {@Properties}", eventName, properties);
    }
    
    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        _logger.LogError(exception, "Exception: {ExceptionMessage} - Properties: {@Properties}", 
            exception.Message, properties);
    }
    
    public void TrackDependency(string dependencyType, string dependencyName, string data, 
        DateTimeOffset startTime, TimeSpan duration, bool success)
    {
        _logger.LogInformation("Dependency: {DependencyType} {DependencyName} - Duration: {DurationMs}ms - Success: {Success}",
            dependencyType, dependencyName, duration.TotalMilliseconds, success);
    }
    
    public void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null)
    {
        _logger.LogInformation("Metric: {MetricName} = {MetricValue} - Properties: {@Properties}",
            metricName, value, properties);
    }
}

// src/LLMGateway.Infrastructure/Telemetry/NullTelemetryService.cs
namespace LLMGateway.Infrastructure.Telemetry;

public class NullTelemetryService : ITelemetryService
{
    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null) { }
    
    public void TrackException(Exception exception, IDictionary<string, string>? properties = null) { }
    
    public void TrackDependency(string dependencyType, string dependencyName, string data, 
        DateTimeOffset startTime, TimeSpan duration, bool success) { }
    
    public void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null) { }
}

// src/LLMGateway.Providers/Cohere/CohereProvider.cs
namespace LLMGateway.Providers.Cohere;

using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class CohereProvider : ILLMProvider
{
    public string ProviderName => "Cohere";
    
    private readonly HttpClient _httpClient;
    private readonly IOptions<CohereOptions> _options;
    private readonly ILogger<CohereProvider> _logger;
    
    private readonly string _apiUrlBase;
    
    public CohereProvider(
        HttpClient httpClient,
        IOptions<CohereOptions> options,
        ILogger<CohereProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        
        _apiUrlBase = _options.Value.ApiUrl.TrimEnd('/');
        
        ConfigureHttpClient();
    }
    
    public async Task<List<ModelInfo>> GetAvailableModelsAsync()
    {
        try
        {
            // Cohere doesn't have a models endpoint, so we use a predefined list from options
            var models = new List<ModelInfo>();
            
            foreach (var modelMapping in _options.Value.ModelMappings)
            {
                models.Add(new ModelInfo
                {
                    Id = modelMapping.ModelId,
                    DisplayName = modelMapping.DisplayName ?? modelMapping.ModelId,
                    Provider = ProviderName,
                    Capabilities = new ModelCapabilities
                    {
                        SupportsCompletion = true,
                        SupportsEmbedding = modelMapping.ModelId.Contains("embed"),
                        SupportsStreaming = true,
                        SupportsFunctionCalling = modelMapping.ModelId.Contains("command"),
                        SupportsVision = false,
                        SupportsJSON = true
                    },
                    ProviderModelId = modelMapping.ProviderModelId,
                    ContextWindow = modelMapping.ContextWindow,
                    Properties = modelMapping.Properties ?? new Dictionary<string, string>()
                });
            }
            
            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Cohere models: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to retrieve available models", ex);
        }
    }
    
    public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request)
    {
        try
        {
            // Convert to Cohere format
            var cohereRequest = ConvertToCohereRequest(request);
            
            var content = new StringContent(
                JsonSerializer.Serialize(cohereRequest, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiUrlBase}/chat", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Cohere API error: {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                var error = ParseErrorResponse(errorContent);
                throw new ProviderException(ProviderName, error.Message, error.Type, new Exception(errorContent));
            }
            
            var cohereResponse = await response.Content.ReadFromJsonAsync<CohereChatResponse>();
            
            if (cohereResponse == null)
            {
                throw new ProviderException(ProviderName, "Empty response received from Cohere API");
            }
            
            return ConvertToCompletionResponse(cohereResponse, request.Model);
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating completion with Cohere: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to create completion", ex);
        }
    }
    
    public async IAsyncEnumerable<CompletionChunk> StreamCompletionAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert to Cohere format
            var cohereRequest = ConvertToCohereRequest(request);
            cohereRequest.Stream = true;
            
            var content = new StringContent(
                JsonSerializer.Serialize(cohereRequest, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiUrlBase}/chat", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Cohere API error: {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                var error = ParseErrorResponse(errorContent);
                throw new ProviderException(ProviderName, error.Message, error.Type, new Exception(errorContent));
            }
            
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            
            string line;
            CohereChatResponse? lastResponse = null;
            
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                if (!line.StartsWith("data: "))
                {
                    continue;
                }
                
                var data = line.Substring("data: ".Length).Trim();
                
                if (data == "[DONE]")
                {
                    break;
                }
                
                try
                {
                    var chunkResponse = JsonSerializer.Deserialize<CohereChatResponse>(data);
                    
                    if (chunkResponse != null)
                    {
                        var completionChunk = ConvertToCompletionChunk(chunkResponse, request.Model, lastResponse);
                        lastResponse = chunkResponse;
                        yield return completionChunk;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error parsing Cohere streaming response: {ErrorMessage}", ex.Message);
                }
            }
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming completion with Cohere: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to stream completion", ex);
        }
    }
    
    public async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request)
    {
        try
        {
            // Determine if this is single input or batch
            List<string> inputs = new();
            
            if (request.Input is string singleInput)
            {
                inputs.Add(singleInput);
            }
            else if (request.Input is IEnumerable<string> stringInputs)
            {
                inputs.AddRange(stringInputs);
            }
            else if (request.Input is object[] arrayInput)
            {
                foreach (var item in arrayInput)
                {
                    if (item is string stringItem)
                    {
                        inputs.Add(stringItem);
                    }
                }
            }
            
            // Convert to Cohere format
            var cohereRequest = new CohereEmbedRequest
            {
                Texts = inputs,
                Model = GetProviderModelId(request.Model),
                InputType = "search_document",
                Truncate = "END"
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(cohereRequest, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiUrlBase}/embed", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Cohere API error: {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                var error = ParseErrorResponse(errorContent);
                throw new ProviderException(ProviderName, error.Message, error.Type, new Exception(errorContent));
            }
            
            var cohereResponse = await response.Content.ReadFromJsonAsync<CohereEmbedResponse>();
            
            if (cohereResponse == null)
            {
                throw new ProviderException(ProviderName, "Empty response received from Cohere API");
            }
            
            return ConvertToEmbeddingResponse(cohereResponse, request.Model, inputs.Count);
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating embedding with Cohere: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to create embedding", ex);
        }
    }
    
    public bool SupportsModel(string modelId)
    {
        // Check our model mapping first
        foreach (var modelMapping in _options.Value.ModelMappings)
        {
            if (modelId.Equals(modelMapping.ModelId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        // Check if this follows Cohere model naming conventions
        return modelId.StartsWith("cohere.") || 
               modelId.StartsWith("command-") ||
               modelId.StartsWith("embed-");
    }
    
    public int CalculateTokenCount(string text, string modelId)
    {
        // Simple approximation: ~4 chars per token
        // In a production implementation, this would use proper tokenizers
        return text.Length / 4 + 1;
    }
    
    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var apiKey = _options.Value.ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.Value.TimeoutSeconds);
    }
    
    private string GetProviderModelId(string modelId)
    {
        // Remove provider prefix if present
        if (modelId.StartsWith("cohere."))
        {
            modelId = modelId.Substring("cohere.".Length);
        }
        
        // Check if we have a mapping for this model
        var customMapping = _options.Value.ModelMappings
            .FirstOrDefault(m => m.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
        
        return customMapping?.ProviderModelId ?? modelId;
    }
    
    private CohereChatRequest ConvertToCohereRequest(CompletionRequest request)
    {
        // Convert to provider-specific model ID
        string providerModelId = GetProviderModelId(request.Model);
        
        // Map messages to Cohere chat history format
        var chatHistory = new List<CohereMessage>();
        string? systemPrompt = null;
        
        foreach (var message in request.Messages.SkipLast(1)) // All except the last one
        {
            if (message.Role == "system")
            {
                systemPrompt = message.Content;
            }
            else
            {
                chatHistory.Add(new CohereMessage
                {
                    Role = message.Role == "assistant" ? "CHATBOT" : "USER",
                    Message = message.Content
                });
            }
        }
        
        // The last message is the current message
        var lastMessage = request.Messages.LastOrDefault();
        var message = lastMessage?.Content ?? string.Empty;
        
        // Convert tools to Cohere tools
        List<CohereTool>? tools = null;
        
        if (request.Tools != null && request.Tools.Any())
        {
            tools = request.Tools
                .Where(t => t.Type == "function")
                .Select(t => new CohereTool
                {
                    Name = t.Function.Name,
                    Description = t.Function.Description,
                    ParameterDefinitions = ConvertParameterDefinitions(t.Function.Parameters)
                }).ToList();
        }
        
        return new CohereChatRequest
        {
            Message = message,
            Model = providerModelId,
            ChatHistory = chatHistory,
            Preamble = systemPrompt,
            Temperature = request.Temperature,
            P = request.TopP,
            MaxTokens = request.MaxTokens,
            Tools = tools,
            Stream = request.Stream,
            StopSequences = request.Stop
        };
    }
    
    private List<CohereParameterDefinition> ConvertParameterDefinitions(object parameters)
    {
        var result = new List<CohereParameterDefinition>();
        
        try
        {
            var jsonString = JsonSerializer.Serialize(parameters);
            var jsonDoc = JsonDocument.Parse(jsonString);
            
            if (jsonDoc.RootElement.TryGetProperty("properties", out var properties))
            {
                foreach (var property in properties.EnumerateObject())
                {
                    var paramDef = new CohereParameterDefinition
                    {
                        Name = property.Name,
                        Type = GetParameterType(property.Value),
                        Description = property.Value.TryGetProperty("description", out var desc) ? 
                            desc.GetString() : null,
                        Required = IsParameterRequired(jsonDoc.RootElement, property.Name)
                    };
                    
                    result.Add(paramDef);
                }
            }
        }
        catch (Exception ex)
        {
            // If parsing fails, return an empty list
            _logger.LogError(ex, "Error parsing parameter definitions: {ErrorMessage}", ex.Message);
        }
        
        return result;
    }
    
    private string GetParameterType(JsonElement element)
    {
        if (element.TryGetProperty("type", out var typeElement))
        {
            var type = typeElement.GetString();
            
            if (type == "string")
                return "string";
            else if (type == "number" || type == "integer")
                return "number";
            else if (type == "boolean")
                return "boolean";
            else if (type == "array")
                return "array";
            else if (type == "object")
                return "object";
        }
        
        return "string"; // Default type
    }
    
    private bool IsParameterRequired(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var req in required.EnumerateArray())
            {
                if (req.ValueKind == JsonValueKind.String && req.GetString() == propertyName)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private CompletionResponse ConvertToCompletionResponse(CohereChatResponse response, string requestedModel)
    {
        var toolCalls = new List<ToolCall>();
        
        // Convert tool calls if present
        if (response.ToolCalls != null && response.ToolCalls.Any())
        {
            foreach (var toolCall in response.ToolCalls)
            {
                toolCalls.Add(new ToolCall
                {
                    Id = toolCall.Id ?? Guid.NewGuid().ToString(),
                    Type = "function",
                    Function = new FunctionCall
                    {
                        Name = toolCall.Name,
                        Arguments = toolCall.Parameters != null ? 
                            JsonSerializer.Serialize(toolCall.Parameters) : "{}"
                    }
                });
            }
        }
        
        // Estimate token counts
        var promptTokens = CalculateTokenCount(response.Message ?? "", requestedModel);
        var completionTokens = CalculateTokenCount(response.Text ?? "", requestedModel);
        
        return new CompletionResponse
        {
            Id = response.GenerationId,
            Object = "chat.completion",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = requestedModel,
            Provider = ProviderName,
            Choices = new List<CompletionChoice>
            {
                new CompletionChoice
                {
                    Index = 0,
                    Message = new MessageResponse
                    {
                        Role = "assistant",
                        Content = response.Text,
                        ToolCalls = toolCalls.Any() ? toolCalls : null
                    },
                    FinishReason = response.FinishReason
                }
            },
            Usage = new UsageInfo
            {
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            }
        };
    }
    
    private CompletionChunk ConvertToCompletionChunk(
        CohereChatResponse response, 
        string requestedModel,
        CohereChatResponse? previousResponse)
    {
        // In streaming mode, Cohere sends the full response each time
        // We need to compute the delta between this response and the previous one
        var currentText = response.Text ?? string.Empty;
        var previousText = previousResponse?.Text ?? string.Empty;
        
        string deltaText = string.Empty;
        
        if (currentText.Length > previousText.Length)
        {
            deltaText = currentText.Substring(previousText.Length);
        }
        
        List<ToolCall>? toolCalls = null;
        
        // Only include tool calls in the last chunk
        if (response.FinishReason != null && response.ToolCalls != null && response.ToolCalls.Any())
        {
            toolCalls = response.ToolCalls.Select(tc => new ToolCall
            {
                Id = tc.Id ?? Guid.NewGuid().ToString(),
                Type = "function",
                Function = new FunctionCall
                {
                    Name = tc.Name,
                    Arguments = tc.Parameters != null ? 
                        JsonSerializer.Serialize(tc.Parameters) : "{}"
                }
            }).ToList();
        }
        
        return new CompletionChunk
        {
            Id = response.GenerationId,
            Object = "chat.completion.chunk",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = requestedModel,
            Provider = ProviderName,
            Choices = new List<ChunkChoice>
            {
                new ChunkChoice
                {
                    Index = 0,
                    Delta = new DeltaMessage
                    {
                        Role = previousResponse == null ? "assistant" : null,
                        Content = string.IsNullOrEmpty(deltaText) ? null : deltaText,
                        ToolCalls = toolCalls
                    },
                    FinishReason = response.FinishReason
                }
            }
        };
    }
    
    private EmbeddingResponse ConvertToEmbeddingResponse(CohereEmbedResponse response, string requestedModel, int inputCount)
    {
        var embeddingResponse = new EmbeddingResponse
        {
            Object = "list",
            Model = requestedModel,
            Provider = ProviderName,
            Data = new List<EmbeddingData>(),
            Usage = new UsageInfo
            {
                PromptTokens = response.Meta?.BilledTokenCount ?? 0,
                CompletionTokens = 0
            }
        };
        
        // Map each embedding
        for (int i = 0; i < response.Embeddings.Count; i++)
        {
            embeddingResponse.Data.Add(new EmbeddingData
            {
                Object = "embedding",
                Index = i,
                Embedding = response.Embeddings[i]
            });
        }
        
        return embeddingResponse;
    }
    
    private CohereError ParseErrorResponse(string errorContent)
    {
        try
        {
            var errorResponse = JsonSerializer.Deserialize<CohereErrorResponse>(errorContent);
            
            if (errorResponse != null)
            {
                return new CohereError
                {
                    Message = errorResponse.Message,
                    Type = errorResponse.Type ?? "unknown"
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Cohere error response: {ErrorContent}", errorContent);
        }
        
        return new CohereError
        {
            Message = "Unknown error occurred",
            Type = "unknown_error"
        };
    }
}

// Cohere API Models
#region Cohere Models

public class CohereChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("chat_history")]
    public List<CohereMessage>? ChatHistory { get; set; }
    
    [JsonPropertyName("preamble")]
    public string? Preamble { get; set; }
    
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }
    
    [JsonPropertyName("p")]
    public float? P { get; set; }
    
    [JsonPropertyName("k")]
    public int? K { get; set; }
    
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
    
    [JsonPropertyName("stop_sequences")]
    public List<string>? StopSequences { get; set; }
    
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
    
    [JsonPropertyName("tools")]
    public List<CohereTool>? Tools { get; set; }
}

public class CohereMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class CohereTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("parameter_definitions")]
    public List<CohereParameterDefinition> ParameterDefinitions { get; set; } = new();
}

public class CohereParameterDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";
    
    [JsonPropertyName("required")]
    public bool Required { get; set; }
}

public class CohereChatResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("generation_id")]
    public string GenerationId { get; set; } = string.Empty;
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
    
    [JsonPropertyName("tool_calls")]
    public List<CohereToolCall>? ToolCalls { get; set; }
}

public class CohereToolCall
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}

public class CohereEmbedRequest
{
    [JsonPropertyName("texts")]
    public List<string> Texts { get; set; } = new();
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("input_type")]
    public string? InputType { get; set; }
    
    [JsonPropertyName("truncate")]
    public string? Truncate { get; set; }
}

public class CohereEmbedResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("embeddings")]
    public List<List<float>> Embeddings { get; set; } = new();
    
    [JsonPropertyName("meta")]
    public CohereEmbedMeta? Meta { get; set; }
}

public class CohereEmbedMeta
{
    [JsonPropertyName("billed_units")]
    public CohereEmbedBilledUnits? BilledUnits { get; set; }
    
    [JsonPropertyName("api_version")]
    public Dictionary<string, string>? ApiVersion { get; set; }
    
    [JsonPropertyName("billed_token_count")]
    public int? BilledTokenCount { get; set; }
}

public class CohereEmbedBilledUnits
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }
}

public class CohereErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class CohereError
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

#endregion

// src/LLMGateway.Providers/Cohere/CohereOptions.cs
namespace LLMGateway.Providers.Cohere;

public class CohereOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://api.cohere.ai/v1";
    public int TimeoutSeconds { get; set; } = 60;
    public List<ModelMapping> ModelMappings { get; set; } = new();
}

// src/LLMGateway.Providers/HuggingFace/HuggingFaceProvider.cs
namespace LLMGateway.Providers.HuggingFace;

using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class HuggingFaceProvider : ILLMProvider
{
    public string ProviderName => "HuggingFace";
    
    private readonly HttpClient _httpClient;
    private readonly IOptions<HuggingFaceOptions> _options;
    private readonly ILogger<HuggingFaceProvider> _logger;
    
    private readonly string _apiUrlBase;
    
    public HuggingFaceProvider(
        HttpClient httpClient,
        IOptions<HuggingFaceOptions> options,
        ILogger<HuggingFaceProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        
        _apiUrlBase = _options.Value.ApiUrl.TrimEnd('/');
        
        ConfigureHttpClient();
    }
    
    public Task<List<ModelInfo>> GetAvailableModelsAsync()
    {
        // HuggingFace doesn't have a standard models endpoint for inference API
        // We use a predefined list from options
        var models = new List<ModelInfo>();
        
        foreach (var modelMapping in _options.Value.ModelMappings)
        {
            models.Add(new ModelInfo
            {
                Id = modelMapping.ModelId,
                DisplayName = modelMapping.DisplayName ?? modelMapping.ModelId,
                Provider = ProviderName,
                Capabilities = new ModelCapabilities
                {
                    SupportsCompletion = true,
                    SupportsEmbedding = modelMapping.ModelId.Contains("embedding"),
                    SupportsStreaming = false, // HF Inference API doesn't standardize streaming
                    SupportsFunctionCalling = false,
                    SupportsVision = false,
                    SupportsJSON = false
                },
                ProviderModelId = modelMapping.ProviderModelId,
                ContextWindow = modelMapping.ContextWindow,
                Properties = modelMapping.Properties ?? new Dictionary<string, string>()
            });
        }
        
        return Task.FromResult(models);
    }
    
    public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request)
    {
        try
        {
            // Get the model repository ID
            string modelId = GetProviderModelId(request.Model);
            
            // Create request based on format
            var inputText = ConvertMessagesToText(request.Messages);
            
            var hfRequest = new HuggingFaceRequest
            {
                Inputs = inputText,
                Parameters = new HuggingFaceParameters
                {
                    Temperature = request.Temperature,
                    TopP = request.TopP,
                    MaxNewTokens = request.MaxTokens,
                    StopSequences = request.Stop
                }
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(hfRequest, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                Encoding.UTF8,
                "application/json");
            
            // HuggingFace Inference API uses the model ID in the URL
            var response = await _httpClient.PostAsync($"{_apiUrlBase}/models/{modelId}", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("HuggingFace API error: {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                throw new ProviderException(ProviderName, $"Error from HuggingFace API: {errorContent}");
            }
            
            // HuggingFace has different response formats based on the model
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return ConvertToCompletionResponse(responseContent, request.Model, inputText);
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating completion with HuggingFace: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to create completion", ex);
        }
    }
    
    public IAsyncEnumerable<CompletionChunk> StreamCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        // HuggingFace Inference API doesn't standardize streaming
        throw new ProviderException(ProviderName, "Streaming is not supported by the HuggingFace provider");
    }
    
    public async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request)
    {
        try
        {
            // Get the embedding model repository ID
            string modelId = GetProviderModelId(request.Model);
            
            // Convert input to the format expected by HuggingFace
            object inputContent;
            
            if (request.Input is string singleInput)
            {
                inputContent = new { inputs = singleInput };
            }
            else if (request.Input is IEnumerable<string> stringInputs)
            {
                inputContent = new { inputs = stringInputs };
            }
            else
            {
                throw new ValidationException("Input must be a string or an array of strings");
            }
            
            var content = new StringContent(
                JsonSerializer.Serialize(inputContent),
                Encoding.UTF8,
                "application/json");
            
            // HuggingFace Embedding API uses the model ID in the URL
            var response = await _httpClient.PostAsync($"{_apiUrlBase}/models/{modelId}", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("HuggingFace API error: {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                throw new ProviderException(ProviderName, $"Error from HuggingFace API: {errorContent}");
            }
            
            // Parse the response
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return ConvertToEmbeddingResponse(responseContent, request.Model, request.Input);
        }
        catch (ProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating embedding with HuggingFace: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to create embedding", ex);
        }
    }
    
    public bool SupportsModel(string modelId)
    {
        // Check our model mapping first
        foreach (var modelMapping in _options.Value.ModelMappings)
        {
            if (modelId.Equals(modelMapping.ModelId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        // Check if this follows HuggingFace model naming conventions
        return modelId.StartsWith("huggingface.") || modelId.Contains("/");
    }
    
    public int CalculateTokenCount(string text, string modelId)
    {
        // Simple approximation: ~4 chars per token
        // In a production implementation, this would use proper tokenizers
        return text.Length / 4 + 1;
    }
    
    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var apiKey = _options.Value.ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.Value.TimeoutSeconds);
    }
    
    private string GetProviderModelId(string modelId)
    {
        // Remove provider prefix if present
        if (modelId.StartsWith("huggingface."))
        {
            modelId = modelId.Substring("huggingface.".Length);
        }
        
        // Check if we have a mapping for this model
        var customMapping = _options.Value.ModelMappings
            .FirstOrDefault(m => m.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
        
        return customMapping?.ProviderModelId ?? modelId;
    }
    
    private string ConvertMessagesToText(List<Message> messages)
    {
        var stringBuilder = new StringBuilder();
        
        foreach (var message in messages)
        {
            switch (message.Role.ToLower())
            {
                case "system":
                    stringBuilder.AppendLine($"System: {message.Content}");
                    break;
                case "user":
                    stringBuilder.AppendLine($"Human: {message.Content}");
                    break;
                case "assistant":
                    stringBuilder.AppendLine($"Assistant: {message.Content}");
                    break;
                default:
                    stringBuilder.AppendLine($"{message.Role}: {message.Content}");
                    break;
            }
        }
        
        // Add the expected assistant prefix for the response
        stringBuilder.Append("Assistant: ");
        
        return stringBuilder.ToString();
    }
    
    private CompletionResponse ConvertToCompletionResponse(string responseContent, string requestedModel, string promptText)
    {
        try
        {
            string generatedText = string.Empty;
            
            // Try to parse as standard HF text generation response
            try
            {
                var textGenerationResponse = JsonSerializer.Deserialize<List<HuggingFaceTextGenerationResponse>>(responseContent);
                
                if (textGenerationResponse != null && textGenerationResponse.Count > 0)
                {
                    generatedText = textGenerationResponse[0].GeneratedText;
                }
            }
            catch
            {
                // Try to parse as a chat completion response
                try
                {
                    var chatResponse = JsonSerializer.Deserialize<HuggingFaceChatResponse>(responseContent);
                    
                    if (chatResponse != null && !string.IsNullOrEmpty(chatResponse.GeneratedText))
                    {
                        generatedText = chatResponse.GeneratedText;
                    }
                }
                catch
                {
                    // Finally, just use the raw response
                    generatedText = responseContent;
                }
            }
            
            // Estimate token counts
            var promptTokens = CalculateTokenCount(promptText, requestedModel);
            var completionTokens = CalculateTokenCount(generatedText, requestedModel);
            
            return new CompletionResponse
            {
                Id = Guid.NewGuid().ToString(),
                Object = "chat.completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = requestedModel,
                Provider = ProviderName,
                Choices = new List<CompletionChoice>
                {
                    new CompletionChoice
                    {
                        Index = 0,
                        Message = new MessageResponse
                        {
                            Role = "assistant",
                            Content = generatedText
                        },
                        FinishReason = "stop"
                    }
                },
                Usage = new UsageInfo
                {
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing HuggingFace response: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to parse response", ex);
        }
    }
    
    private EmbeddingResponse ConvertToEmbeddingResponse(string responseContent, string requestedModel, object input)
    {
        try
        {
            var embeddings = new List<List<float>>();
            
            // Try to parse different embedding response formats
            try
            {
                // Try as a direct array
                var singleEmbedding = JsonSerializer.Deserialize<List<float>>(responseContent);
                
                if (singleEmbedding != null)
                {
                    embeddings.Add(singleEmbedding);
                }
            }
            catch
            {
                try
                {
                    // Try as a list of arrays
                    var multipleEmbeddings = JsonSerializer.Deserialize<List<List<float>>>(responseContent);
                    
                    if (multipleEmbeddings != null)
                    {
                        embeddings = multipleEmbeddings;
                    }
                }
                catch
                {
                    // Try as an object with embeddings property
                    try
                    {
                        var embeddingResponse = JsonSerializer.Deserialize<HuggingFaceEmbeddingResponse>(responseContent);
                        
                        if (embeddingResponse?.Embeddings != null)
                        {
                            embeddings = embeddingResponse.Embeddings;
                        }
                    }
                    catch
                    {
                        // As a last resort, parse as JSON document
                        var jsonDoc = JsonDocument.Parse(responseContent);
                        var root = jsonDoc.RootElement;
                        
                        // Look for array properties that might contain embeddings
                        foreach (var property in root.EnumerateObject())
                        {
                            if (property.Value.ValueKind == JsonValueKind.Array)
                            {
                                var potentialEmbedding = new List<float>();
                                var isEmbedding = true;
                                
                                foreach (var item in property.Value.EnumerateArray())
                                {
                                    if (item.ValueKind == JsonValueKind.Number)
                                    {
                                        potentialEmbedding.Add(item.GetSingle());
                                    }
                                    else
                                    {
                                        isEmbedding = false;
                                        break;
                                    }
                                }
                                
                                if (isEmbedding && potentialEmbedding.Count > 0)
                                {
                                    embeddings.Add(potentialEmbedding);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            
            if (embeddings.Count == 0)
            {
                throw new ProviderException(ProviderName, "Could not parse embedding response");
            }
            
            // Create the embedding response
            var embeddingResponse = new EmbeddingResponse
            {
                Object = "list",
                Model = requestedModel,
                Provider = ProviderName,
                Data = new List<EmbeddingData>(),
                Usage = new UsageInfo
                {
                    PromptTokens = CalculateInputTokens(input),
                    CompletionTokens = 0
                }
            };
            
            // Map each embedding
            for (int i = 0; i < embeddings.Count; i++)
            {
                embeddingResponse.Data.Add(new EmbeddingData
                {
                    Object = "embedding",
                    Index = i,
                    Embedding = embeddings[i]
                });
            }
            
            return embeddingResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing HuggingFace embedding response: {ErrorMessage}", ex.Message);
            throw new ProviderException(ProviderName, "Failed to parse embedding response", ex);
        }
    }
    
    private int CalculateInputTokens(object input)
    {
        int totalTokens = 0;
        
        if (input is string singleInput)
        {
            totalTokens = CalculateTokenCount(singleInput, "");
        }
        else if (input is IEnumerable<string> stringInputs)
        {
            foreach (var text in stringInputs)
            {
                totalTokens += CalculateTokenCount(text, "");
            }
        }
        
        return totalTokens;
    }
}

// HuggingFace API Models
#region HuggingFace Models

public class HuggingFaceRequest
{
    [JsonPropertyName("inputs")]
    public string Inputs { get; set; } = string.Empty;
    
    [JsonPropertyName("parameters")]
    public HuggingFaceParameters? Parameters { get; set; }
}

public class HuggingFaceParameters
{
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }
    
    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }
    
    [JsonPropertyName("max_new_tokens")]
    public int? MaxNewTokens { get; set; }
    
    [JsonPropertyName("stop")]
    public List<string>? StopSequences { get; set; }
}

public class HuggingFaceTextGenerationResponse
{
    [JsonPropertyName("generated_text")]
    public string GeneratedText { get; set; } = string.Empty;
}

public class HuggingFaceChatResponse
{
    [JsonPropertyName("generated_text")]
    public string GeneratedText { get; set; } = string.Empty;
}

public class HuggingFaceEmbeddingResponse
{
    [JsonPropertyName("embeddings")]
    public List<List<float>> Embeddings { get; set; } = new();
}

#endregion

// src/LLMGateway.Providers/HuggingFace/HuggingFaceOptions.cs
namespace LLMGateway.Providers.HuggingFace;

public class HuggingFaceOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://api-inference.huggingface.co";
    public int TimeoutSeconds { get; set; } = 60;
    public List<ModelMapping> ModelMappings { get; set; } = new();
}

// src/LLMGateway.Infrastructure/Logging/LoggingExtensions.cs
namespace LLMGateway.Infrastructure.Logging;

using Microsoft.Extensions.DependencyInjection;
using Serilog;

public static class LoggingExtensions
{
    public static IServiceCollection AddLLMGatewayLogging(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .CreateLogger();
        
        services.AddSingleton(Log.Logger);
        
        return services;
    }
}
