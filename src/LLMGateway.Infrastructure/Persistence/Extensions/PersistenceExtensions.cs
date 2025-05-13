using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Persistence.Entities;
using LLMGateway.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace LLMGateway.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extensions for persistence
/// </summary>
public static class PersistenceExtensions
{
    /// <summary>
    /// Add persistence
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var persistenceOptions = new PersistenceOptions();
        configuration.GetSection("Persistence").Bind(persistenceOptions);
        
        if (!persistenceOptions.UseDatabase)
        {
            return services;
        }
        
        // Add the database context
        services.AddDbContext<LLMGatewayDbContext>(options =>
        {
            switch (persistenceOptions.DatabaseProvider.ToLowerInvariant())
            {
                case "sqlserver":
                    options.UseSqlServer(persistenceOptions.ConnectionString);
                    break;
                    
                case "postgresql":
                    // Use dynamic approach to avoid direct reference
                    var npgsqlMethod = typeof(DbContextOptionsBuilder)
                        .GetMethod("UseNpgsql", new[] { typeof(string) });
                    
                    if (npgsqlMethod != null)
                    {
                        npgsqlMethod.Invoke(options, new object[] { persistenceOptions.ConnectionString });
                    }
                    else
                    {
                        throw new ArgumentException("Npgsql provider not available. Please install the Npgsql.EntityFrameworkCore.PostgreSQL package.");
                    }
                    break;
                    
                case "sqlite":
                    // Use dynamic approach to avoid direct reference
                    var sqliteMethod = typeof(DbContextOptionsBuilder)
                        .GetMethod("UseSqlite", new[] { typeof(string) });
                    
                    if (sqliteMethod != null)
                    {
                        sqliteMethod.Invoke(options, new object[] { persistenceOptions.ConnectionString });
                    }
                    else
                    {
                        throw new ArgumentException("SQLite provider not available. Please install the Microsoft.EntityFrameworkCore.Sqlite package.");
                    }
                    break;
                    
                default:
                    throw new ArgumentException($"Unsupported database provider: {persistenceOptions.DatabaseProvider}");
            }
        });
        
        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<ITokenUsageRepository, TokenUsageRepository>();
        services.AddScoped<IModelMetricsRepository, ModelMetricsRepository>();
        services.AddScoped<IProviderHealthRepository, ProviderHealthRepository>();
        services.AddScoped<IRoutingDecisionsRepository, RoutingDecisionsRepository>();
        services.AddScoped<IRequestLogRepository, RequestLogRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<IProviderConfigurationRepository, ProviderConfigurationRepository>();
        services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
        
        return services;
    }
    
    /// <summary>
    /// Migrate the database
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder</returns>
    public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LLMGatewayDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LLMGatewayDbContext>>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
        
        if (!options.UseDatabase || !options.EnableMigrations)
        {
            return app;
        }
        
        try
        {
            logger.LogInformation("Applying database migrations");
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations");
            throw;
        }
        
        return app;
    }
    
    /// <summary>
    /// Seed initial data
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder</returns>
    public static IApplicationBuilder SeedInitialData(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LLMGatewayDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LLMGatewayDbContext>>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
        
        if (!options.UseDatabase || !options.EnableSeeding)
        {
            return app;
        }
        
        try
        {
            logger.LogInformation("Seeding initial data");
            
            // Seed admin user if no users exist
            if (!dbContext.Users.Any())
            {
                var adminUser = new Entities.User
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    Role = "Administrator",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                
                dbContext.Users.Add(adminUser);
                dbContext.SaveChanges();
                
                // Create an API key for the admin user
                var apiKey = new Entities.ApiKey
                {
                    UserId = adminUser.Id,
                    Key = Guid.NewGuid().ToString("N"),
                    Name = "Default Admin Key",
                    CreatedAt = DateTimeOffset.UtcNow,
                    IsActive = true,
                    Permissions = "admin",
                    DailyTokenLimit = 100000,
                    MonthlyTokenLimit = 3000000
                };
                
                dbContext.ApiKeys.Add(apiKey);
                
                // Add user permissions
                var adminPermission = new Entities.UserPermission
                {
                    UserId = adminUser.Id,
                    Permission = "FullAccess",
                    IsGranted = true,
                    GrantedAt = DateTimeOffset.UtcNow
                };
                
                dbContext.UserPermissions.Add(adminPermission);
                dbContext.SaveChanges();
                
                logger.LogInformation("Created admin user with API key: {ApiKey}", apiKey.Key);
            }
            
            // Seed default models if no models exist
            if (!dbContext.Models.Any())
            {
                var defaultModels = new List<Entities.Model>
                {
                    new Entities.Model
                    {
                        Id = Guid.NewGuid().ToString(),
                        DisplayName = "GPT-4",
                        Provider = "OpenAI",
                        ProviderModelId = "gpt-4",
                        IsActive = true,
                        ContextWindow = 8192,
                        SupportsCompletions = true,
                        SupportsStreaming = true,
                        SupportsFunctionCalling = true,
                        CostPer1kPromptTokensUsd = 0.01m,
                        CostPer1kCompletionTokensUsd = 0.03m,
                        CreatedAt = DateTimeOffset.UtcNow
                    },
                    new Entities.Model
                    {
                        Id = Guid.NewGuid().ToString(),
                        DisplayName = "GPT-3.5 Turbo",
                        Provider = "OpenAI",
                        ProviderModelId = "gpt-3.5-turbo",
                        IsActive = true,
                        ContextWindow = 4096,
                        SupportsCompletions = true,
                        SupportsStreaming = true,
                        SupportsFunctionCalling = true,
                        CostPer1kPromptTokensUsd = 0.001m,
                        CostPer1kCompletionTokensUsd = 0.002m,
                        CreatedAt = DateTimeOffset.UtcNow
                    },
                    new Entities.Model
                    {
                        Id = Guid.NewGuid().ToString(),
                        DisplayName = "Claude 3 Opus",
                        Provider = "Anthropic",
                        ProviderModelId = "claude-3-opus",
                        IsActive = true,
                        ContextWindow = 200000,
                        SupportsCompletions = true,
                        SupportsStreaming = true,
                        SupportsFunctionCalling = true,
                        SupportsVision = true,
                        CostPer1kPromptTokensUsd = 0.015m,
                        CostPer1kCompletionTokensUsd = 0.075m,
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                };
                
                dbContext.Models.AddRange(defaultModels);
                dbContext.SaveChanges();
                
                logger.LogInformation("Created default model definitions");
            }
            
            // Seed provider configurations if none exist
            if (!dbContext.ProviderConfigurations.Any())
            {
                var defaultProviders = new List<Entities.ProviderConfiguration>
                {
                    new Entities.ProviderConfiguration
                    {
                        Provider = "OpenAI",
                        ApiKey = "YOUR_API_KEY_HERE",
                        ApiUrl = "https://api.openai.com/v1",
                        IsActive = true,
                        TimeoutSeconds = 30,
                        AdditionalConfiguration = "{ \"organization\": \"\" }",
                        CreatedAt = DateTimeOffset.UtcNow,
                        LastModified = DateTimeOffset.UtcNow
                    },
                    new Entities.ProviderConfiguration
                    {
                        Provider = "Anthropic",
                        ApiKey = "YOUR_API_KEY_HERE",
                        ApiUrl = "https://api.anthropic.com",
                        IsActive = true,
                        TimeoutSeconds = 60,
                        AdditionalConfiguration = "{ }",
                        CreatedAt = DateTimeOffset.UtcNow,
                        LastModified = DateTimeOffset.UtcNow
                    }
                };
                
                dbContext.ProviderConfigurations.AddRange(defaultProviders);
                dbContext.SaveChanges();
                
                logger.LogInformation("Created default provider configurations");
            }
            
            // Seed default settings if none exist
            if (!dbContext.Settings.Any())
            {
                var defaultSettings = new List<Entities.Setting>
                {
                    new Entities.Setting
                    {
                        Key = "DefaultModel",
                        Value = "gpt-3.5-turbo",
                        Description = "Default model to use when none is specified",
                        Category = "Routing",
                        LastModified = DateTimeOffset.UtcNow
                    },
                    new Entities.Setting
                    {
                        Key = "DefaultMaxTokens",
                        Value = "1000",
                        Description = "Default maximum number of tokens for responses",
                        Category = "Limits",
                        LastModified = DateTimeOffset.UtcNow
                    },
                    new Entities.Setting
                    {
                        Key = "EnableMetricsCollection",
                        Value = "true",
                        Description = "Whether to collect performance metrics",
                        Category = "Monitoring",
                        LastModified = DateTimeOffset.UtcNow
                    }
                };
                
                dbContext.Settings.AddRange(defaultSettings);
                dbContext.SaveChanges();
                
                logger.LogInformation("Created default application settings");
            }
            
            logger.LogInformation("Initial data seeded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding initial data");
            throw;
        }
        
        return app;
    }
}
