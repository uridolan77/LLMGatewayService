// src/LLMGateway.API/Program.cs (updated with new services)
using LLMGateway.API.Extensions;
using LLMGateway.API.Middleware;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Routing;
using LLMGateway.Core.Services;
using LLMGateway.Infrastructure.Caching;
using LLMGateway.Infrastructure.Logging;
using LLMGateway.Infrastructure.Persistence.Extensions;
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

// Add configuration options
builder.Services.AddLLMGatewayOptions(builder.Configuration);

// Add core services
builder.Services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<ICompletionService, CompletionService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IModelService, ModelService>();

// Add persistence and routing services
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddScoped<IModelRouter, SmartModelRouter>();

// Conditionally register token usage service based on configuration
var tokenUsageOptions = builder.Configuration.GetSection("TokenUsage");
if (tokenUsageOptions.GetValue<string>("StorageProvider") == "Database" 
    && builder.Configuration.GetValue<bool>("Persistence:UseDatabase"))
{
    // SQL-based token usage service already registered in AddPersistence
}
else
{
    // In-memory token usage service
    builder.Services.AddSingleton<ITokenUsageService, TokenUsageService>();
}

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

// Add database health checks if using a database
if (builder.Configuration.GetValue<bool>("Persistence:UseDatabase"))
{
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<LLMGateway.Infrastructure.Persistence.LLMGatewayDbContext>();
}

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

// Apply database migrations if enabled
if (builder.Configuration.GetValue<bool>("Persistence:UseDatabase") && 
    builder.Configuration.GetValue<bool>("Persistence:AutoMigrateOnStartup"))
{
    app.MigrateDatabase();
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

// src/LLMGateway.Infrastructure/Persistence/Extensions/MigrationExtensions.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace LLMGateway.Infrastructure.Persistence.Extensions;

public static class MigrationExtensions
{
    public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LLMGatewayDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LLMGatewayDbContext>>();
        
        try
        {
            logger.LogInformation("Applying database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }
        
        return app;
    }
}

// src/LLMGateway.API/Extensions/ServiceCollectionExtensions.cs (updated)
using LLMGateway.Core.Options;
using LLMGateway.Core.Routing;
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
        // Core options
        services.Configure<GlobalOptions>(configuration.GetSection("GlobalOptions"));
        services.Configure<LLMRoutingOptions>(configuration.GetSection("LLMRouting"));
        services.Configure<FallbackOptions>(configuration.GetSection("Fallbacks"));
        services.Configure<LoggingOptions>(configuration.GetSection("Logging"));
        services.Configure<TelemetryOptions>(configuration.GetSection("Telemetry"));
        services.Configure<ApiKeyOptions>(configuration.GetSection("ApiKeys"));
        services.Configure<TokenUsageOptions>(configuration.GetSection("TokenUsage"));
        
        // New options for routing and persistence
        services.Configure<RoutingOptions>(configuration.GetSection("Routing"));
        services.Configure<UserPreferencesOptions>(configuration.GetSection("UserPreferences"));
        services.Configure<PersistenceOptions>(configuration.GetSection("Persistence"));
        
        return services;
    }
    
    // Rest of the methods remain the same...
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

// src/LLMGateway.Core/Services/CompletionService.cs (updated to use IModelRouter)
using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;
using LLMGateway.Core.Options;
using LLMGateway.Core.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LLMGateway.Core.Services;

public class CompletionService : ICompletionService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IModelRouter _modelRouter;
    private readonly IOptions<FallbackOptions> _fallbackOptions;
    private readonly IOptions<GlobalOptions> _globalOptions;
    private readonly IOptions<RoutingOptions> _routingOptions;
    private readonly ILogger<CompletionService> _logger;
    
    public CompletionService(
        ILLMProviderFactory providerFactory,
        ITokenUsageService tokenUsageService,
        IModelRouter modelRouter,
        IOptions<FallbackOptions> fallbackOptions,
        IOptions<GlobalOptions> globalOptions,
        IOptions<RoutingOptions> routingOptions,
        ILogger<CompletionService> logger)
    {
        _providerFactory = providerFactory;
        _tokenUsageService = tokenUsageService;
        _modelRouter = modelRouter;
        _fallbackOptions = fallbackOptions;
        _globalOptions = globalOptions;
        _routingOptions = routingOptions;
        _logger = logger;
    }
    
    public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request, string userId)
    {
        ValidateRequest(request);
        
        var stopwatch = Stopwatch.StartNew();
        var originalModelId = request.Model;
        
        // Use model router to select the best model if smart routing is enabled
        if (_routingOptions.Value.EnableSmartRouting)
        {
            request.Model = await _modelRouter.SelectModelAsync(request.Model, request, userId);
            
            if (request.Model != originalModelId)
            {
                _logger.LogInformation("Model router selected {SelectedModel} instead of {OriginalModel}",
                    request.Model, originalModelId);
            }
        }
        
        var provider = _providerFactory.GetProviderForModel(request.Model);
        
        if (provider == null)
        {
            throw new NotFoundException("model", request.Model);
        }
        
        try
        {
            var response = await provider.CreateCompletionAsync(request);
            stopwatch.Stop();
            
            // Track token usage
            if (_globalOptions.Value.TrackTokenUsage)
            {
                await TrackTokenUsage(response, provider.ProviderName, request.Model, userId);
            }
            
            // Track model metrics if enabled
            if (_routingOptions.Value.TrackModelMetrics)
            {
                await _modelRouter.RecordModelMetricsAsync(new ModelMetrics
                {
                    ModelId = request.Model,
                    Provider = provider.ProviderName,
                    RequestTokens = response.Usage.PromptTokens,
                    ResponseTokens = response.Usage.CompletionTokens,
                    LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                    IsSuccess = true,
                    Cost = CalculateCost(request.Model, response.Usage.PromptTokens, response.Usage.CompletionTokens),
                    Timestamp = DateTime.UtcNow
                });
            }
            
            return response;
        }
        catch (ProviderException ex)
        {
            stopwatch.Stop();
            
            // Track model error metrics if enabled
            if (_routingOptions.Value.TrackModelMetrics)
            {
                await _modelRouter.RecordModelMetricsAsync(new ModelMetrics
                {
                    ModelId = request.Model,
                    Provider = provider.ProviderName,
                    RequestTokens = CalculatePromptTokens(request, provider),
                    ResponseTokens = 0,
                    LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Cost = 0,
                    Timestamp = DateTime.UtcNow
                });
            }
            
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
        
        var stopwatch = Stopwatch.StartNew();
        var originalModelId = request.Model;
        
        // Use model router to select the best model if smart routing is enabled
        if (_routingOptions.Value.EnableSmartRouting)
        {
            request.Model = await _modelRouter.SelectModelAsync(request.Model, request, userId);
            
            if (request.Model != originalModelId)
            {
                _logger.LogInformation("Model router selected {SelectedModel} instead of {OriginalModel} for streaming",
                    request.Model, originalModelId);
            }
        }
        
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
            bool isSuccess = true;
            
            await foreach (var chunk in streamingResponse.WithCancellation(cancellationToken))
            {
                if (_globalOptions.Value.TrackTokenUsage && chunk.Choices.Any(c => !string.IsNullOrEmpty(c.Delta.Content)))
                {
                    // Count completion tokens in each chunk
                    completionTokens += CountTokensInChunk(chunk, provider, request.Model);
                }
                
                yield return chunk;
            }
            
            stopwatch.Stop();
            
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
            
            // Track model metrics if enabled
            if (_routingOptions.Value.TrackModelMetrics)
            {
                await _modelRouter.RecordModelMetricsAsync(new ModelMetrics
                {
                    ModelId = request.Model,
                    Provider = provider.ProviderName,
                    RequestTokens = promptTokens,
                    ResponseTokens = completionTokens,
                    LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                    IsSuccess = isSuccess,
                    Cost = CalculateCost(request.Model, promptTokens, completionTokens),
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (ProviderException ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Error from provider {ProviderName} when streaming completion: {ErrorMessage}",
                provider.ProviderName, ex.Message);
            
            // Track model error metrics if enabled
            if (_routingOptions.Value.TrackModelMetrics)
            {
                await _modelRouter.RecordModelMetricsAsync(new ModelMetrics
                {
                    ModelId = request.Model,
                    Provider = provider.ProviderName,
                    RequestTokens = promptTokens,
                    ResponseTokens = 0,
                    LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Cost = 0,
                    Timestamp = DateTime.UtcNow
                });
            }
            
            if (_fallbackOptions.Value.EnableFallbacks)
            {
                var fallbackModel = GetFallbackModel(request.Model, ex.ErrorCode);
                
                if (fallbackModel != null)
                {
                    _logger.LogInformation("Falling back to model {FallbackModel} after error from {OriginalModel}",
                        fallbackModel, request.Model);
                    
                    request.Model = fallbackModel;
                    
                    // If we're falling back, record this as a routing decision
                    await _modelRouter.RecordRoutingDecisionAsync(new RoutingDecision
                    {
                        OriginalModelId = originalModelId,
                        SelectedModelId = fallbackModel,
                        RoutingStrategy = "Fallback",
                        UserId = userId,
                        RequestContent = GetContentSummary(request),
                        RequestTokenCount = promptTokens,
                        IsFallback = true,
                        FallbackReason = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                    
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
    
    // Other methods remain largely the same, just adding a few helper methods
    
    private string GetContentSummary(CompletionRequest request)
    {
        if (request.Messages.Count == 0)
        {
            return string.Empty;
        }
        
        var lastUserMessage = request.Messages
            .LastOrDefault(m => m.Role == "user");
            
        if (lastUserMessage == null || string.IsNullOrEmpty(lastUserMessage.Content))
        {
            return string.Empty;
        }
        
        // Return a truncated summary
        var content = lastUserMessage.Content;
        
        if (content.Length > 100)
        {
            return content.Substring(0, 97) + "...";
        }
        
        return content;
    }
    
    private double CalculateCost(string modelId, int promptTokens, int completionTokens)
    {
        // Get price per token from model mappings in LLMRouting
        var modelMapping = _globalOptions.Value.ModelMappings
            .FirstOrDefault(m => m.ModelId == modelId);
            
        if (modelMapping == null || 
            !modelMapping.Properties.TryGetValue("TokenPriceInput", out string? inputPrice) ||
            !modelMapping.Properties.TryGetValue("TokenPriceOutput", out string? outputPrice))
        {
            return 0;
        }
        
        if (double.TryParse(inputPrice, out double inputPriceValue) && 
            double.TryParse(outputPrice, out double outputPriceValue))
        {
            return (inputPriceValue * promptTokens) + (outputPriceValue * completionTokens);
        }
        
        return 0;
    }
    
    // Rest of the methods remain the same as in the original implementation
    private void ValidateRequest(CompletionRequest request)
    {
        // Implementation unchanged...
    }
    
    private async Task<CompletionResponse> HandleProviderExceptionAsync(
        ProviderException ex, CompletionRequest request, string userId)
    {
        // Implementation unchanged...
    }
    
    private string? GetFallbackModel(string modelId, string? errorCode)
    {
        // Implementation unchanged...
    }
    
    private async Task TrackTokenUsage(
        CompletionResponse response, string providerName, string modelId, string userId)
    {
        // Implementation unchanged...
    }
    
    private int CalculatePromptTokens(CompletionRequest request, ILLMProvider provider)
    {
        // Implementation unchanged...
    }
    
    private int CountTokensInChunk(CompletionChunk chunk, ILLMProvider provider, string modelId)
    {
        // Implementation unchanged...
    }
}
