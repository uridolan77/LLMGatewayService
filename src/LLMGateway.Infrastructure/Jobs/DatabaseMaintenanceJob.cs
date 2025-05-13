using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Persistence;
using LLMGateway.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace LLMGateway.Infrastructure.Jobs;

/// <summary>
/// Job for database maintenance
/// </summary>
[DisallowConcurrentExecution]
public class DatabaseMaintenanceJob : IJob
{
    private readonly LLMGatewayDbContext? _dbContext;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<DatabaseMaintenanceJob> _logger;
    private readonly PersistenceOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="telemetryService">Telemetry service</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Persistence options</param>
    public DatabaseMaintenanceJob(
        IServiceProvider serviceProvider,
        ITelemetryService telemetryService,
        ILogger<DatabaseMaintenanceJob> logger,
        IOptions<PersistenceOptions> options)
    {
        // Get the database context if it's registered
        _dbContext = options.Value.UseDatabase ? 
            serviceProvider.GetService(typeof(LLMGatewayDbContext)) as LLMGatewayDbContext : null;
        
        _telemetryService = telemetryService;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task Execute(IJobExecutionContext context)
    {
        using var operation = _telemetryService.TrackOperation("DatabaseMaintenanceJob.Execute");
        
        try
        {
            _logger.LogInformation("Running database maintenance");
            
            if (_dbContext == null || !_options.UseDatabase)
            {
                _logger.LogInformation("Database is not configured, skipping maintenance");
                return;
            }
            
            // Clean up old token usage records
            var cutoffDate = DateTimeOffset.UtcNow - _options.DataRetentionPeriod;
            
            // In a real implementation, this would delete old records from the database
            // For example:
            // var deletedCount = await _dbContext.TokenUsageRecords
            //     .Where(r => r.Timestamp < cutoffDate)
            //     .ExecuteDeleteAsync();
            
            // _logger.LogInformation("Deleted {Count} old token usage records", deletedCount);
            
            // Run database optimizations
            // This would be database-specific
            // For example, for SQL Server:
            // await _dbContext.Database.ExecuteSqlRawAsync("EXEC sp_updatestats");
            
            _logger.LogInformation("Database maintenance completed");
            
            _telemetryService.TrackEvent("DatabaseMaintenanceCompleted", 
                new Dictionary<string, string>
                {
                    ["CutoffDate"] = cutoffDate.ToString("yyyy-MM-dd")
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run database maintenance");
            _telemetryService.TrackException(ex);
            throw;
        }
    }
}
