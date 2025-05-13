// src/LLMGateway.Core/Jobs/IBackgroundJobService.cs
namespace LLMGateway.Core.Jobs;

public interface IBackgroundJobService
{
    Task ScheduleJobAsync<TJob>(TimeSpan initialDelay, TimeSpan interval) where TJob : IJob;
    Task ScheduleJobAsync<TJob>(string cronExpression) where TJob : IJob;
    Task CancelJobAsync<TJob>() where TJob : IJob;
    Task CancelAllJobsAsync();
    Task TriggerJobAsync<TJob>() where TJob : IJob;
}

// src/LLMGateway.Core/Jobs/IJob.cs
namespace LLMGateway.Core.Jobs;

public interface IJob
{
    string JobName { get; }
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

// src/LLMGateway.Infrastructure/Jobs/BackgroundJobService.cs
using Cronos;
using LLMGateway.Core.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LLMGateway.Infrastructure.Jobs;

public class BackgroundJobService : IBackgroundJobService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly ConcurrentDictionary<string, Timer> _timers = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsSources = new();
    
    public BackgroundJobService(
        IServiceProvider serviceProvider,
        ILogger<BackgroundJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public Task ScheduleJobAsync<TJob>(TimeSpan initialDelay, TimeSpan interval) where TJob : IJob
    {
        var jobName = typeof(TJob).Name;
        
        _logger.LogInformation("Scheduling job {JobName} with interval {Interval}", jobName, interval);
        
        var cts = new CancellationTokenSource();
        _ctsSources[jobName] = cts;
        
        var timer = new Timer(
            async _ => await ExecuteJobAsync<TJob>(cts.Token),
            null,
            initialDelay,
            interval);
        
        _timers[jobName] = timer;
        
        return Task.CompletedTask;
    }
    
    public Task ScheduleJobAsync<TJob>(string cronExpression) where TJob : IJob
    {
        var jobName = typeof(TJob).Name;
        
        _logger.LogInformation("Scheduling job {JobName} with cron expression {CronExpression}", jobName, cronExpression);
        
        var cts = new CancellationTokenSource();
        _ctsSources[jobName] = cts;
        
        var timer = new Timer(
            _ => ScheduleNextCronExecution<TJob>(cronExpression, cts.Token),
            null,
            CalculateNextCronDelay(cronExpression),
            Timeout.InfiniteTimeSpan); // We'll manually set the next interval
        
        _timers[jobName] = timer;
        
        return Task.CompletedTask;
    }
    
    public Task CancelJobAsync<TJob>() where TJob : IJob
    {
        var jobName = typeof(TJob).Name;
        
        if (_timers.TryRemove(jobName, out var timer))
        {
            timer.Change(Timeout.Infinite, 0);
            timer.Dispose();
            
            _logger.LogInformation("Cancelled job {JobName}", jobName);
        }
        
        if (_ctsSources.TryRemove(jobName, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
        
        return Task.CompletedTask;
    }
    
    public async Task CancelAllJobsAsync()
    {
        foreach (var jobName in _timers.Keys.ToList())
        {
            if (_timers.TryRemove(jobName, out var timer))
            {
                timer.Change(Timeout.Infinite, 0);
                timer.Dispose();
            }
            
            if (_ctsSources.TryRemove(jobName, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }
        
        _logger.LogInformation("Cancelled all jobs");
        
        await Task.CompletedTask;
    }
    
    public async Task TriggerJobAsync<TJob>() where TJob : IJob
    {
        var jobName = typeof(TJob).Name;
        _logger.LogInformation("Manually triggering job {JobName}", jobName);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<TJob>();
            await job.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing job {JobName}: {ErrorMessage}", jobName, ex.Message);
        }
    }
    
    private async Task ExecuteJobAsync<TJob>(CancellationToken cancellationToken) where TJob : IJob
    {
        var jobName = typeof(TJob).Name;
        
        _logger.LogInformation("Executing job {JobName}", jobName);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<TJob>();
            await job.ExecuteAsync(cancellationToken);
            
            _logger.LogInformation("Job {JobName} executed successfully", jobName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Job {JobName} was cancelled", jobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing job {JobName}: {ErrorMessage}", jobName, ex.Message);
        }
    }
    
    private void ScheduleNextCronExecution<TJob>(string cronExpression, CancellationToken cancellationToken) where TJob : IJob
    {
        // First execute the job
        _ = Task.Run(async () => await ExecuteJobAsync<TJob>(cancellationToken), cancellationToken);
        
        // Then schedule the next execution
        var delay = CalculateNextCronDelay(cronExpression);
        
        if (_timers.TryGetValue(typeof(TJob).Name, out var timer))
        {
            timer.Change(delay, Timeout.InfiniteTimeSpan);
        }
    }
    
    private TimeSpan CalculateNextCronDelay(string cronExpression)
    {
        try
        {
            var expression = CronExpression.Parse(cronExpression);
            var nextOccurrence = expression.GetNextOccurrence(DateTime.UtcNow);
            
            if (nextOccurrence.HasValue)
            {
                return nextOccurrence.Value - DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing cron expression {CronExpression}: {ErrorMessage}", 
                cronExpression, ex.Message);
        }
        
        // Default to 1 hour if we can't parse the expression or calculate the next occurrence
        return TimeSpan.FromHours(1);
    }
    
    public void Dispose()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Dispose();
        }
        
        foreach (var cts in _ctsSources.Values)
        {
            cts.Dispose();
        }
        
        _timers.Clear();
        _ctsSources.Clear();
    }
}

// src/LLMGateway.Infrastructure/Jobs/BackgroundJobExtensions.cs
using LLMGateway.Core.Jobs;
using LLMGateway.Infrastructure.Jobs.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LLMGateway.Infrastructure.Jobs;

public static class BackgroundJobExtensions
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the background job service
        services.AddSingleton<IBackgroundJobService, BackgroundJobService>();
        
        // Register the background job runner
        services.AddHostedService<BackgroundJobRunner>();
        
        // Register jobs
        services.AddTransient<TokenUsageReportJob>();
        services.AddTransient<ProviderHealthCheckJob>();
        services.AddTransient<ModelMetricsAggregationJob>();
        services.AddTransient<DatabaseMaintenanceJob>();
        services.AddTransient<CostReportJob>();
        
        return services;
    }
}

// src/LLMGateway.Infrastructure/Jobs/BackgroundJobRunner.cs
using LLMGateway.Core.Jobs;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Jobs.Implementations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Infrastructure.Jobs;

public class BackgroundJobRunner : BackgroundService
{
    private readonly IBackgroundJobService _jobService;
    private readonly IOptions<BackgroundJobOptions> _jobOptions;
    private readonly ILogger<BackgroundJobRunner> _logger;
    
    public BackgroundJobRunner(
        IBackgroundJobService jobService,
        IOptions<BackgroundJobOptions> jobOptions,
        ILogger<BackgroundJobRunner> logger)
    {
        _jobService = jobService;
        _jobOptions = jobOptions;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Job Runner is starting");
        
        try
        {
            // Schedule all configured jobs
            await ScheduleJobsAsync();
            
            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
            _logger.LogInformation("Background Job Runner was stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Background Job Runner: {ErrorMessage}", ex.Message);
        }
    }
    
    private async Task ScheduleJobsAsync()
    {
        var options = _jobOptions.Value;
        
        // Token Usage Report Job
        if (options.EnableTokenUsageReports && !string.IsNullOrEmpty(options.TokenUsageReportSchedule))
        {
            await _jobService.ScheduleJobAsync<TokenUsageReportJob>(options.TokenUsageReportSchedule);
            _logger.LogInformation("Scheduled Token Usage Report Job with cron: {Schedule}", 
                options.TokenUsageReportSchedule);
        }
        
        // Provider Health Check Job
        if (options.EnableProviderHealthChecks)
        {
            var healthCheckInterval = TimeSpan.FromMinutes(options.ProviderHealthCheckIntervalMinutes);
            await _jobService.ScheduleJobAsync<ProviderHealthCheckJob>(TimeSpan.Zero, healthCheckInterval);
            _logger.LogInformation("Scheduled Provider Health Check Job with interval: {Interval} minutes", 
                options.ProviderHealthCheckIntervalMinutes);
        }
        
        // Model Metrics Aggregation Job
        if (options.EnableModelMetricsAggregation && !string.IsNullOrEmpty(options.ModelMetricsAggregationSchedule))
        {
            await _jobService.ScheduleJobAsync<ModelMetricsAggregationJob>(options.ModelMetricsAggregationSchedule);
            _logger.LogInformation("Scheduled Model Metrics Aggregation Job with cron: {Schedule}", 
                options.ModelMetricsAggregationSchedule);
        }
        
        // Database Maintenance Job
        if (options.EnableDatabaseMaintenance && !string.IsNullOrEmpty(options.DatabaseMaintenanceSchedule))
        {
            await _jobService.ScheduleJobAsync<DatabaseMaintenanceJob>(options.DatabaseMaintenanceSchedule);
            _logger.LogInformation("Scheduled Database Maintenance Job with cron: {Schedule}", 
                options.DatabaseMaintenanceSchedule);
        }
        
        // Cost Report Job
        if (options.EnableCostReports && !string.IsNullOrEmpty(options.CostReportSchedule))
        {
            await _jobService.ScheduleJobAsync<CostReportJob>(options.CostReportSchedule);
            _logger.LogInformation("Scheduled Cost Report Job with cron: {Schedule}", 
                options.CostReportSchedule);
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background Job Runner is stopping");
        
        await _jobService.CancelAllJobsAsync();
        
        await base.StopAsync(cancellationToken);
    }
}

// src/LLMGateway.Core/Options/BackgroundJobOptions.cs
namespace LLMGateway.Core.Options;

public class BackgroundJobOptions
{
    public bool EnableTokenUsageReports { get; set; } = true;
    public string TokenUsageReportSchedule { get; set; } = "0 0 * * *"; // Daily at midnight
    
    public bool EnableProviderHealthChecks { get; set; } = true;
    public int ProviderHealthCheckIntervalMinutes { get; set; } = 5;
    
    public bool EnableModelMetricsAggregation { get; set; } = true;
    public string ModelMetricsAggregationSchedule { get; set; } = "0 * * * *"; // Hourly
    
    public bool EnableDatabaseMaintenance { get; set; } = true;
    public string DatabaseMaintenanceSchedule { get; set; } = "0 1 * * 0"; // Weekly on Sunday at 1 AM
    
    public bool EnableCostReports { get; set; } = true;
    public string CostReportSchedule { get; set; } = "0 0 1 * *"; // Monthly on the 1st
    
    public List<string> ReportRecipients { get; set; } = new();
    public string ReportEmailSubjectPrefix { get; set; } = "[LLM Gateway] ";
    public bool IncludeAttachments { get; set; } = true;
}

// src/LLMGateway.Infrastructure/Jobs/Implementations/TokenUsageReportJob.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Jobs;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Infrastructure.Jobs.Implementations;

public class TokenUsageReportJob : IJob
{
    public string JobName => "TokenUsageReport";
    
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IOptions<BackgroundJobOptions> _jobOptions;
    private readonly ILogger<TokenUsageReportJob> _logger;
    
    public TokenUsageReportJob(
        ITokenUsageService tokenUsageService,
        IOptions<BackgroundJobOptions> jobOptions,
        ILogger<TokenUsageReportJob> logger)
    {
        _tokenUsageService = tokenUsageService;
        _jobOptions = jobOptions;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Token Usage Report job");
        
        try
        {
            // Get the time range for the report (last day)
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-1);
            
            // Generate the report
            var tokenUsage = await _tokenUsageService.GetTokenUsageAsync(startDate, endDate);
            
            _logger.LogInformation("Token Usage Report generated for {StartDate} to {EndDate}: " +
                "Total tokens: {TotalTokens}, Prompt tokens: {PromptTokens}, Completion tokens: {CompletionTokens}",
                startDate, endDate, tokenUsage.TotalTokens, tokenUsage.TotalPromptTokens, tokenUsage.TotalCompletionTokens);
            
            // If there are recipients configured, send the report via email
            if (_jobOptions.Value.ReportRecipients.Any())
            {
                // In a real implementation, we would call a notification service here
                _logger.LogInformation("Would send token usage report to {RecipientCount} recipients",
                    _jobOptions.Value.ReportRecipients.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token usage report: {ErrorMessage}", ex.Message);
        }
    }
}

// src/LLMGateway.Infrastructure/Jobs/Implementations/ProviderHealthCheckJob.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Jobs;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Jobs.Implementations;

public class ProviderHealthCheckJob : IJob
{
    public string JobName => "ProviderHealthCheck";
    
    private readonly IProviderHealthMonitorService _healthMonitorService;
    private readonly ILogger<ProviderHealthCheckJob> _logger;
    
    public ProviderHealthCheckJob(
        IProviderHealthMonitorService healthMonitorService,
        ILogger<ProviderHealthCheckJob> logger)
    {
        _healthMonitorService = healthMonitorService;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Provider Health Check job");
        
        try
        {
            var healthStatus = await _healthMonitorService.RunHealthChecksAsync();
            
            var healthyCount = healthStatus.Count(s => s.Status == "Healthy");
            var unhealthyCount = healthStatus.Count - healthyCount;
            
            _logger.LogInformation("Provider Health Check completed: {HealthyCount} healthy, {UnhealthyCount} unhealthy providers",
                healthyCount, unhealthyCount);
            
            if (unhealthyCount > 0)
            {
                foreach (var provider in healthStatus.Where(s => s.Status != "Healthy"))
                {
                    _logger.LogWarning("Unhealthy provider: {ProviderName}, Error: {ErrorMessage}",
                        provider.ProviderName, provider.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running provider health check: {ErrorMessage}", ex.Message);
        }
    }
}

// src/LLMGateway.Infrastructure/Jobs/Implementations/ModelMetricsAggregationJob.cs
using LLMGateway.Core.Jobs;
using LLMGateway.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Jobs.Implementations;

public class ModelMetricsAggregationJob : IJob
{
    public string JobName => "ModelMetricsAggregation";
    
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<ModelMetricsAggregationJob> _logger;
    
    public ModelMetricsAggregationJob(
        LLMGatewayDbContext dbContext,
        ILogger<ModelMetricsAggregationJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Model Metrics Aggregation job");
        
        try
        {
            // Get the time range for aggregation (last hour)
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddHours(-1);
            
            // Get all request logs from the last hour
            var recentLogs = await _dbContext.RequestLogs
                .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
                .ToListAsync(cancellationToken);
            
            // Group by model
            var modelGroups = recentLogs
                .GroupBy(r => r.ModelId)
                .ToList();
            
            _logger.LogInformation("Aggregating metrics for {ModelCount} models with {RequestCount} requests",
                modelGroups.Count, recentLogs.Count);
            
            foreach (var group in modelGroups)
            {
                var modelId = group.Key;
                
                // Find existing metrics for this model
                var metrics = await _dbContext.ModelMetrics
                    .FirstOrDefaultAsync(m => m.ModelId == modelId, cancellationToken);
                
                if (metrics == null)
                {
                    // Model not found in metrics table, skip
                    continue;
                }
                
                // Calculate new metrics
                var successCount = group.Count(r => r.IsSuccess);
                var errorCount = group.Count(r => !r.IsSuccess);
                var averageLatency = group.Average(r => r.LatencyMs);
                
                // Calculate throughput (requests per minute)
                var periodMinutes = (endDate - startDate).TotalMinutes;
                var throughput = group.Count() / periodMinutes;
                
                // Update the metrics
                metrics.AverageLatencyMs = (metrics.AverageLatencyMs * 0.7) + (averageLatency * 0.3); // Weighted average
                metrics.SuccessCount += successCount;
                metrics.ErrorCount += errorCount;
                metrics.ThroughputPerMinute = throughput;
                metrics.LastUpdated = DateTime.UtcNow;
                
                _logger.LogInformation("Updated metrics for model {ModelId}: " +
                    "Success rate: {SuccessRate}%, Average latency: {LatencyMs}ms, Throughput: {Throughput} req/min",
                    modelId,
                    successCount / (double)(successCount + errorCount) * 100,
                    averageLatency,
                    throughput);
            }
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Model Metrics Aggregation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating model metrics: {ErrorMessage}", ex.Message);
        }
    }
}

// src/LLMGateway.Infrastructure/Jobs/Implementations/DatabaseMaintenanceJob.cs
using LLMGateway.Core.Jobs;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Infrastructure.Jobs.Implementations;

public class DatabaseMaintenanceJob : IJob
{
    public string JobName => "DatabaseMaintenance";
    
    private readonly LLMGatewayDbContext _dbContext;
    private readonly IOptions<PersistenceOptions> _persistenceOptions;
    private readonly ILogger<DatabaseMaintenanceJob> _logger;
    
    public DatabaseMaintenanceJob(
        LLMGatewayDbContext dbContext,
        IOptions<PersistenceOptions> persistenceOptions,
        ILogger<DatabaseMaintenanceJob> logger)
    {
        _dbContext = dbContext;
        _persistenceOptions = persistenceOptions;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Database Maintenance job");
        
        try
        {
            // Clean up old data based on retention policy
            var retentionDate = DateTime.UtcNow - _persistenceOptions.Value.DataRetentionPeriod;
            
            // Clean up old token usage records
            var oldTokenUsage = await _dbContext.TokenUsage
                .Where(t => t.Timestamp < retentionDate)
                .ToListAsync(cancellationToken);
            
            if (oldTokenUsage.Any())
            {
                _dbContext.TokenUsage.RemoveRange(oldTokenUsage);
                _logger.LogInformation("Removed {Count} old token usage records", oldTokenUsage.Count);
            }
            
            // Clean up old routing history
            var oldRoutingHistory = await _dbContext.RoutingHistory
                .Where(r => r.Timestamp < retentionDate)
                .ToListAsync(cancellationToken);
            
            if (oldRoutingHistory.Any())
            {
                _dbContext.RoutingHistory.RemoveRange(oldRoutingHistory);
                _logger.LogInformation("Removed {Count} old routing history records", oldRoutingHistory.Count);
            }
            
            // Clean up old provider health checks
            var oldHealthChecks = await _dbContext.ProviderHealthChecks
                .Where(h => h.Timestamp < retentionDate)
                .ToListAsync(cancellationToken);
            
            if (oldHealthChecks.Any())
            {
                _dbContext.ProviderHealthChecks.RemoveRange(oldHealthChecks);
                _logger.LogInformation("Removed {Count} old provider health check records", oldHealthChecks.Count);
            }
            
            // Clean up old request logs
            var oldRequestLogs = await _dbContext.RequestLogs
                .Where(r => r.Timestamp < retentionDate)
                .ToListAsync(cancellationToken);
            
            if (oldRequestLogs.Any())
            {
                _dbContext.RequestLogs.RemoveRange(oldRequestLogs);
                _logger.LogInformation("Removed {Count} old request log records", oldRequestLogs.Count);
            }
            
            // Save all changes
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Database Maintenance completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing database maintenance: {ErrorMessage}", ex.Message);
        }
    }
}

// src/LLMGateway.Infrastructure/Jobs/Implementations/CostReportJob.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Jobs;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Infrastructure.Jobs.Implementations;

public class CostReportJob : IJob
{
    public string JobName => "CostReport";
    
    private readonly ITokenUsageService _tokenUsageService;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IOptions<BackgroundJobOptions> _jobOptions;
    private readonly ILogger<CostReportJob> _logger;
    
    public CostReportJob(
        ITokenUsageService tokenUsageService,
        ILLMProviderFactory providerFactory,
        IOptions<BackgroundJobOptions> jobOptions,
        ILogger<CostReportJob> logger)
    {
        _tokenUsageService = tokenUsageService;
        _providerFactory = providerFactory;
        _jobOptions = jobOptions;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Cost Report job");
        
        try
        {
            // Get the time range for the report (last month)
            var now = DateTime.UtcNow;
            var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            var endDate = new DateTime(now.Year, now.Month, 1).AddTicks(-1);
            
            // Generate the report
            var tokenUsage = await _tokenUsageService.GetTokenUsageAsync(startDate, endDate);
            
            // Calculate costs for each model
            var costByModel = new Dictionary<string, decimal>();
            decimal totalCost = 0;
            
            foreach (var model in tokenUsage.UsageByModel)
            {
                var modelInfo = await _providerFactory.GetModelAsync(model.Key);
                
                if (modelInfo != null &&
                    modelInfo.Properties.TryGetValue("TokenPriceInput", out string? inputPrice) &&
                    modelInfo.Properties.TryGetValue("TokenPriceOutput", out string? outputPrice) &&
                    decimal.TryParse(inputPrice, out decimal inputPriceValue) &&
                    decimal.TryParse(outputPrice, out decimal outputPriceValue))
                {
                    var cost = (inputPriceValue * model.Value.PromptTokens) + 
                               (outputPriceValue * model.Value.CompletionTokens);
                    
                    costByModel[model.Key] = cost;
                    totalCost += cost;
                }
            }
            
            // Calculate costs by provider
            var costByProvider = costByModel
                .GroupBy(kvp => tokenUsage.UsageByModel[kvp.Key].Provider)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(kvp => kvp.Value));
            
            _logger.LogInformation("Cost Report generated for {StartDate} to {EndDate}: " +
                "Total cost: ${TotalCost}, Models: {ModelCount}, Providers: {ProviderCount}",
                startDate.ToString("yyyy-MM-dd"),
                endDate.ToString("yyyy-MM-dd"),
                totalCost.ToString("F2"),
                costByModel.Count,
                costByProvider.Count);
            
            // If there are recipients configured, send the report via email
            if (_jobOptions.Value.ReportRecipients.Any())
            {
                // In a real implementation, we would call a notification service here
                _logger.LogInformation("Would send cost report to {RecipientCount} recipients",
                    _jobOptions.Value.ReportRecipients.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cost report: {ErrorMessage}", ex.Message);
        }
    }
}
