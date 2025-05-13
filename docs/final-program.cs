// src/LLMGateway.API/Program.cs (final version)
using LLMGateway.API.Extensions;
using LLMGateway.API.Middleware;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Routing;
using LLMGateway.Core.Services;
using LLMGateway.Infrastructure.Caching;
using LLMGateway.Infrastructure.Jobs;
using LLMGateway.Infrastructure.Logging;
using LLMGateway.Infrastructure.Monitoring.Extensions;
using LLMGateway.Infrastructure.Persistence.Extensions;
using LLMGateway.Infrastructure.Telemetry;
using LLMGateway.Providers.Factory;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://yourdomain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Pagination");
    });
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

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API key authentication.",
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    // Add XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add configuration options
builder.Services.AddLLMGatewayOptions(builder.Configuration);

// Add core services
builder.Services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<ICompletionService, CompletionService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IModelService, ModelService>();

// Add persistence
builder.Services.AddPersistence(builder.Configuration);

// Add routing services
builder.Services.AddScoped<IModelRouter, SmartModelRouter>();

// Add monitoring
builder.Services.AddMonitoring(builder.Configuration);

// Add background jobs
builder.Services.AddBackgroundJobs(builder.Configuration);

// Add infrastructure services
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddTelemetry(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks();

// Add database health checks if using a database
if (builder.Configuration.GetValue<bool>("Persistence:UseDatabase"))
{
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<LLMGateway.Infrastructure.Persistence.LLMGatewayDbContext>();
}

// Add Redis health checks if configured
if (!string.IsNullOrEmpty(builder.Configuration.GetValue<string>("Redis:ConnectionString")))
{
    builder.Services.AddHealthChecks()
        .AddRedis(builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379", "redis");
}

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
app.UseCors("DefaultPolicy");
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
