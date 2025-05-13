// src/LLMGateway.Infrastructure/Persistence/LLMGatewayDbContext.cs
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLMGateway.Infrastructure.Persistence;

public class LLMGatewayDbContext : DbContext
{
    public LLMGatewayDbContext(DbContextOptions<LLMGatewayDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<TokenUsageEntity> TokenUsage { get; set; } = null!;
    public DbSet<RoutingHistoryEntity> RoutingHistory { get; set; } = null!;
    public DbSet<ProviderHealthCheckEntity> ProviderHealthChecks { get; set; } = null!;
    public DbSet<ModelMetricsEntity> ModelMetrics { get; set; } = null!;
    public DbSet<RequestLogEntity> RequestLogs { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Token Usage
        modelBuilder.Entity<TokenUsageEntity>()
            .HasIndex(e => e.UserId);
            
        modelBuilder.Entity<TokenUsageEntity>()
            .HasIndex(e => e.ModelId);
            
        modelBuilder.Entity<TokenUsageEntity>()
            .HasIndex(e => e.Provider);
            
        modelBuilder.Entity<TokenUsageEntity>()
            .HasIndex(e => e.Timestamp);
        
        // Routing History
        modelBuilder.Entity<RoutingHistoryEntity>()
            .HasIndex(e => e.OriginalModelId);
            
        modelBuilder.Entity<RoutingHistoryEntity>()
            .HasIndex(e => e.SelectedModelId);
            
        modelBuilder.Entity<RoutingHistoryEntity>()
            .HasIndex(e => e.RoutingStrategy);
            
        modelBuilder.Entity<RoutingHistoryEntity>()
            .HasIndex(e => e.Timestamp);
        
        // Provider Health Checks
        modelBuilder.Entity<ProviderHealthCheckEntity>()
            .HasIndex(e => e.ProviderName);
            
        modelBuilder.Entity<ProviderHealthCheckEntity>()
            .HasIndex(e => e.Status);
            
        modelBuilder.Entity<ProviderHealthCheckEntity>()
            .HasIndex(e => e.Timestamp);
        
        // Model Metrics
        modelBuilder.Entity<ModelMetricsEntity>()
            .HasIndex(e => e.ModelId);
            
        modelBuilder.Entity<ModelMetricsEntity>()
            .HasIndex(e => e.Provider);
            
        modelBuilder.Entity<ModelMetricsEntity>()
            .HasIndex(e => e.LastUpdated);
        
        // Request Logs
        modelBuilder.Entity<RequestLogEntity>()
            .HasIndex(e => e.RequestType);
            
        modelBuilder.Entity<RequestLogEntity>()
            .HasIndex(e => e.ModelId);
            
        modelBuilder.Entity<RequestLogEntity>()
            .HasIndex(e => e.UserId);
            
        modelBuilder.Entity<RequestLogEntity>()
            .HasIndex(e => e.Timestamp);
    }
}

// src/LLMGateway.Infrastructure/Persistence/Entities/TokenUsageEntity.cs
namespace LLMGateway.Infrastructure.Persistence.Entities;

public class TokenUsageEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// src/LLMGateway.Infrastructure/Persistence/Entities/RoutingHistoryEntity.cs
namespace LLMGateway.Infrastructure.Persistence.Entities;

public class RoutingHistoryEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OriginalModelId { get; set; } = string.Empty;
    public string SelectedModelId { get; set; } = string.Empty;
    public string RoutingStrategy { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? RequestContent { get; set; }
    public int? RequestTokenCount { get; set; }
    public bool IsFallback { get; set; }
    public string? FallbackReason { get; set; }
    public int LatencyMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// src/LLMGateway.Infrastructure/Persistence/Entities/ProviderHealthCheckEntity.cs
namespace LLMGateway.Infrastructure.Persistence.Entities;

public class ProviderHealthCheckEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProviderName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// src/LLMGateway.Infrastructure/Persistence/Entities/ModelMetricsEntity.cs
namespace LLMGateway.Infrastructure.Persistence.Entities;

public class ModelMetricsEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ModelId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public double AverageLatencyMs { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRate => ErrorCount > 0 ? (double)ErrorCount / (SuccessCount + ErrorCount) : 0;
    public double ThroughputPerMinute { get; set; }
    public double CostPerRequest { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

// src/LLMGateway.Infrastructure/Persistence/Entities/RequestLogEntity.cs
namespace LLMGateway.Infrastructure.Persistence.Entities;

public class RequestLogEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RequestType { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int LatencyMs { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RequestId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// src/LLMGateway.Infrastructure/Persistence/Repositories/ITokenUsageRepository.cs
using LLMGateway.Core.Models;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

public interface ITokenUsageRepository
{
    Task AddTokenUsageAsync(TokenUsageInfo usage);
    Task<TokenUsageReport> GetTokenUsageAsync(DateTime startDate, DateTime endDate);
    Task<TokenUsageReport> GetTokenUsageByUserAsync(string userId, DateTime startDate, DateTime endDate);
    Task<TokenUsageReport> GetTokenUsageByModelAsync(string modelId, DateTime startDate, DateTime endDate);
    Task<TokenUsageReport> GetTokenUsageByProviderAsync(string provider, DateTime startDate, DateTime endDate);
}

// src/LLMGateway.Infrastructure/Persistence/Repositories/TokenUsageRepository.cs
using LLMGateway.Core.Models;
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

public class TokenUsageRepository : ITokenUsageRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<TokenUsageRepository> _logger;
    
    public TokenUsageRepository(
        LLMGatewayDbContext dbContext,
        ILogger<TokenUsageRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task AddTokenUsageAsync(TokenUsageInfo usage)
    {
        try
        {
            var entity = new TokenUsageEntity
            {
                UserId = usage.UserId,
                ModelId = usage.ModelId,
                Provider = usage.Provider,
                PromptTokens = usage.PromptTokens,
                CompletionTokens = usage.CompletionTokens,
                RequestType = usage.RequestType,
                Timestamp = usage.Timestamp
            };
            
            _dbContext.TokenUsage.Add(entity);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving token usage to database: {ErrorMessage}", ex.Message);
            throw;
        }
    }
    
    public async Task<TokenUsageReport> GetTokenUsageAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var entities = await _dbContext.TokenUsage
                .Where(u => u.Timestamp >= startDate && u.Timestamp <= endDate)
                .ToListAsync();
            
            return GenerateReport(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token usage from database: {ErrorMessage}", ex.Message);
            throw;
        }
    }
    
    public async Task<TokenUsageReport> GetTokenUsageByUserAsync(string userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var entities = await _dbContext.TokenUsage
                .Where(u => u.UserId == userId && u.Timestamp >= startDate && u.Timestamp <= endDate)
                .ToListAsync();
            
            return GenerateReport(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token usage by user from database: {ErrorMessage}", ex.Message);
            throw;
        }
    }
    
    public async Task<TokenUsageReport> GetTokenUsageByModelAsync(string modelId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var entities = await _dbContext.TokenUsage
                .Where(u => u.ModelId == modelId && u.Timestamp >= startDate && u.Timestamp <= endDate)
                .ToListAsync();
            
            return GenerateReport(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token usage by model from database: {ErrorMessage}", ex.Message);
            throw;
        }
    }
    
    public async Task<TokenUsageReport> GetTokenUsageByProviderAsync(string provider, DateTime startDate, DateTime endDate)
    {
        try
        {
            var entities = await _dbContext.TokenUsage
                .Where(u => u.Provider == provider && u.Timestamp >= startDate && u.Timestamp <= endDate)
                .ToListAsync();
            
            return GenerateReport(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token usage by provider from database: {ErrorMessage}", ex.Message);
            throw;
        }
    }
    
    private TokenUsageReport GenerateReport(List<TokenUsageEntity> entities)
    {
        var report = new TokenUsageReport
        {
            TotalPromptTokens = entities.Sum(e => e.PromptTokens),
            TotalCompletionTokens = entities.Sum(e => e.CompletionTokens)
        };
        
        // Generate usage by provider
        foreach (var providerGroup in entities.GroupBy(e => e.Provider))
        {
            report.UsageByProvider[providerGroup.Key] = new ProviderUsage
            {
                Provider = providerGroup.Key,
                PromptTokens = providerGroup.Sum(e => e.PromptTokens),
                CompletionTokens = providerGroup.Sum(e => e.CompletionTokens)
            };
        }
        
        // Generate usage by model
        foreach (var modelGroup in entities.GroupBy(e => e.ModelId))
        {
            report.UsageByModel[modelGroup.Key] = new ModelUsage
            {
                ModelId = modelGroup.Key,
                Provider = modelGroup.First().Provider,
                PromptTokens = modelGroup.Sum(e => e.PromptTokens),
                CompletionTokens = modelGroup.Sum(e => e.CompletionTokens)
            };
        }
        
        // Generate usage by user
        foreach (var userGroup in entities.GroupBy(e => e.UserId))
        {
            var userUsage = new UserUsage
            {
                UserId = userGroup.Key,
                PromptTokens = userGroup.Sum(e => e.PromptTokens),
                CompletionTokens = userGroup.Sum(e => e.CompletionTokens)
            };
            
            // Calculate tokens by model for this user
            foreach (var modelGroup in userGroup.GroupBy(e => e.ModelId))
            {
                userUsage.TokensByModel[modelGroup.Key] = 
                    modelGroup.Sum(e => e.PromptTokens + e.CompletionTokens);
            }
            
            report.UsageByUser[userGroup.Key] = userUsage;
        }
        
        // Generate usage by request type
        foreach (var requestTypeGroup in entities.GroupBy(e => e.RequestType))
        {
            report.UsageByRequestType[requestTypeGroup.Key] = 
                requestTypeGroup.Sum(e => e.PromptTokens + e.CompletionTokens);
        }
        
        // Generate daily usage
        foreach (var dateGroup in entities.GroupBy(e => e.Timestamp.Date))
        {
            report.DailyUsage.Add(new DailyUsage
            {
                Date = dateGroup.Key,
                PromptTokens = dateGroup.Sum(e => e.PromptTokens),
                CompletionTokens = dateGroup.Sum(e => e.CompletionTokens)
            });
        }
        
        // Sort daily usage by date
        report.DailyUsage = report.DailyUsage.OrderBy(d => d.Date).ToList();
        
        return report;
    }
}

// src/LLMGateway.Infrastructure/Persistence/Services/SqlTokenUsageService.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Persistence.Services;

public class SqlTokenUsageService : ITokenUsageService
{
    private readonly ITokenUsageRepository _repository;
    private readonly ILogger<SqlTokenUsageService> _logger;
    
    public SqlTokenUsageService(
        ITokenUsageRepository repository,
        ILogger<SqlTokenUsageService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task TrackTokenUsageAsync(TokenUsageInfo usage)
    {
        try
        {
            await _repository.AddTokenUsageAsync(usage);
            
            _logger.LogDebug("Tracked token usage in database: {PromptTokens} prompt, {CompletionTokens} completion for model {ModelId}",
                usage.PromptTokens, usage.CompletionTokens, usage.ModelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking token usage: {ErrorMessage}", ex.Message);
        }
    }
    
    public Task<TokenUsageReport> GetTokenUsageAsync(DateTime startDate, DateTime endDate)
    {
        return _repository.GetTokenUsageAsync(startDate, endDate);
    }
    
    public Task<TokenUsageReport> GetTokenUsageByUserAsync(string userId, DateTime startDate, DateTime endDate)
    {
        return _repository.GetTokenUsageByUserAsync(userId, startDate, endDate);
    }
    
    public Task<TokenUsageReport> GetTokenUsageByModelAsync(string modelId, DateTime startDate, DateTime endDate)
    {
        return _repository.GetTokenUsageByModelAsync(modelId, startDate, endDate);
    }
    
    public Task<TokenUsageReport> GetTokenUsageByProviderAsync(string provider, DateTime startDate, DateTime endDate)
    {
        return _repository.GetTokenUsageByProviderAsync(provider, startDate, endDate);
    }
}

// src/LLMGateway.Infrastructure/Persistence/Extensions/PersistenceExtensions.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Persistence.Repositories;
using LLMGateway.Infrastructure.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LLMGateway.Infrastructure.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var persistenceOptions = new PersistenceOptions();
        configuration.GetSection("Persistence").Bind(persistenceOptions);
        
        if (persistenceOptions.UseDatabase)
        {
            // Add DbContext
            services.AddDbContext<LLMGatewayDbContext>(options =>
            {
                switch (persistenceOptions.DatabaseProvider.ToLowerInvariant())
                {
                    case "sqlserver":
                        options.UseSqlServer(
                            persistenceOptions.ConnectionString,
                            sqlOptions =>
                            {
                                sqlOptions.EnableRetryOnFailure(
                                    maxRetryCount: 10,
                                    maxRetryDelay: TimeSpan.FromSeconds(30),
                                    errorNumbersToAdd: null);
                            });
                        break;
                        
                    case "postgresql":
                        options.UseNpgsql(
                            persistenceOptions.ConnectionString,
                            npgsqlOptions =>
                            {
                                npgsqlOptions.EnableRetryOnFailure(
                                    maxRetryCount: 10,
                                    maxRetryDelay: TimeSpan.FromSeconds(30),
                                    errorCodesToAdd: null);
                            });
                        break;
                        
                    case "sqlite":
                        options.UseSqlite(persistenceOptions.ConnectionString);
                        break;
                        
                    default:
                        throw new ArgumentException($"Unsupported database provider: {persistenceOptions.DatabaseProvider}");
                }
            });
            
            // Register repositories
            services.AddScoped<ITokenUsageRepository, TokenUsageRepository>();
            
            // Register services
            services.AddScoped<ITokenUsageService, SqlTokenUsageService>();
        }
        
        return services;
    }
}

// src/LLMGateway.Core/Options/PersistenceOptions.cs
namespace LLMGateway.Core.Options;

public class PersistenceOptions
{
    public bool UseDatabase { get; set; } = false;
    public string DatabaseProvider { get; set; } = "SQLServer";
    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableMigrations { get; set; } = true;
    public bool AutoMigrateOnStartup { get; set; } = true;
}
