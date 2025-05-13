// src/LLMGateway.Core/Interfaces/IProviderHealthMonitorService.cs
using LLMGateway.Core.Routing;

namespace LLMGateway.Core.Interfaces;

public interface IProviderHealthMonitorService
{
    Task<List<ProviderHealthStatus>> GetProviderHealthStatusAsync();
    Task<List<ProviderHealthStatus>> RunHealthChecksAsync();
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    Task StopMonitoringAsync();
}

// src/LLMGateway.Infrastructure/Monitoring/ProviderHealthMonitorService.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Options;
using LLMGateway.Core.Routing;
using LLMGateway.Infrastructure.Persistence.Entities;
using LLMGateway.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace LLMGateway.Infrastructure.Monitoring;

public class ProviderHealthMonitorService : IProviderHealthMonitorService, IDisposable
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly LLMGateway.Infrastructure.Persistence.LLMGatewayDbContext _dbContext;
    private readonly IOptions<MonitoringOptions> _monitoringOptions;
    private readonly ILogger<ProviderHealthMonitorService> _logger;
    
    private Timer? _monitoringTimer;
    private bool _isMonitoring;
    private readonly SemaphoreSlim _monitoringLock = new(1, 1);
    
    public ProviderHealthMonitorService(
        ILLMProviderFactory providerFactory,
        LLMGateway.Infrastructure.Persistence.LLMGatewayDbContext dbContext,
        IOptions<MonitoringOptions> monitoringOptions,
        ILogger<ProviderHealthMonitorService> logger)
    {
        _providerFactory = providerFactory;
        _dbContext = dbContext;
        _monitoringOptions = monitoringOptions;
        _logger = logger;
    }
    
    public async Task<List<ProviderHealthStatus>> GetProviderHealthStatusAsync()
    {
        try
        {
            // Group by provider name and get the latest status for each provider
            var latestHealthChecks = await _dbContext.ProviderHealthChecks
                .GroupBy(h => h.ProviderName)
                .Select(g => g.OrderByDescending(h => h.Timestamp).FirstOrDefault())
                .ToListAsync();
            
            if (!latestHealthChecks.Any())
            {
                // If no health checks have been run yet, run them now
                return await RunHealthChecksAsync();
            }
            
            return latestHealthChecks.Select(e => new ProviderHealthStatus
            {
                ProviderName = e.ProviderName,
                Status = e.Status,
                LatencyMs = e.LatencyMs,
                ErrorMessage = e.ErrorMessage,
                LastChecked = e.Timestamp
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provider health status from database: {ErrorMessage}", ex.Message);
            return new List<ProviderHealthStatus>();
        }
    }
    
    public async Task<List<ProviderHealthStatus>> RunHealthChecksAsync()
    {
        var providers = _providerFactory.GetAllProviders().ToList();
        var healthStatuses = new List<ProviderHealthStatus>();
        
        foreach (var provider in providers)
        {
            var healthStatus = await CheckProviderHealthAsync(provider);
            healthStatuses.Add(healthStatus);
            
            // Save the health check to the database
            var entity = new ProviderHealthCheckEntity
            {
                ProviderName = healthStatus.ProviderName,
                Status = healthStatus.Status,
                LatencyMs = healthStatus.LatencyMs,
                ErrorMessage = healthStatus.ErrorMessage,
                Timestamp = DateTime.UtcNow
            };
            
            _dbContext.ProviderHealthChecks.Add(entity);
        }
        
        await _dbContext.SaveChangesAsync();
        
        return healthStatuses;
    }
    
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        await _monitoringLock.WaitAsync(cancellationToken);
        
        try
        {
            if (_isMonitoring)
            {
                return;
            }
            
            _logger.LogInformation("Starting provider health monitoring");
            
            // Run initial health checks
            await RunHealthChecksAsync();
            
            // Set up the timer for periodic health checks
            var interval = TimeSpan.FromMinutes(_monitoringOptions.Value.HealthCheckIntervalMinutes);
            _monitoringTimer = new Timer(
                async _ => await CheckAllProvidersHealthAsync(),
                null,
                interval,
                interval);
            
            _isMonitoring = true;
            
            _logger.LogInformation("Provider health monitoring started with interval of {IntervalMinutes} minutes",
                _monitoringOptions.Value.HealthCheckIntervalMinutes);
        }
        finally
        {
            _monitoringLock.Release();
        }
    }
    
    public async Task StopMonitoringAsync()
    {
        await _monitoringLock.WaitAsync();
        
        try
        {
            if (!_isMonitoring)
            {
                return;
            }
            
            _logger.LogInformation("Stopping provider health monitoring");
            
            _monitoringTimer?.Change(Timeout.Infinite, 0);
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
            
            _isMonitoring = false;
            
            _logger.LogInformation("Provider health monitoring stopped");
        }
        finally
        {
            _monitoringLock.Release();
        }
    }
    
    private async Task CheckAllProvidersHealthAsync()
    {
        try
        {
            _logger.LogInformation("Running scheduled provider health checks");
            await RunHealthChecksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled provider health checks: {ErrorMessage}", ex.Message);
        }
    }
    
    private async Task<ProviderHealthStatus> CheckProviderHealthAsync(ILLMProvider provider)
    {
        _logger.LogInformation("Checking health of provider: {ProviderName}", provider.ProviderName);
        
        var healthStatus = new ProviderHealthStatus
        {
            ProviderName = provider.ProviderName,
            LastChecked = DateTime.UtcNow
        };
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Attempt to get available models as a health check
            await provider.GetAvailableModelsAsync();
            
            // If we get here, the provider is healthy
            healthStatus.Status = "Healthy";
            stopwatch.Stop();
            healthStatus.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
            
            _logger.LogInformation("Provider {ProviderName} is healthy. Latency: {LatencyMs}ms",
                provider.ProviderName, healthStatus.LatencyMs);
        }
        catch (Exception ex)
        {
            // Provider is unhealthy
            stopwatch.Stop();
            healthStatus.Status = "Unhealthy";
            healthStatus.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
            healthStatus.ErrorMessage = ex.Message;
            
            _logger.LogWarning("Provider {ProviderName} is unhealthy: {ErrorMessage}",
                provider.ProviderName, ex.Message);
        }
        
        return healthStatus;
    }
    
    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _monitoringLock.Dispose();
    }
}

// src/LLMGateway.Core/Options/MonitoringOptions.cs
namespace LLMGateway.Core.Options;

public class MonitoringOptions
{
    public bool EnableHealthMonitoring { get; set; } = true;
    public int HealthCheckIntervalMinutes { get; set; } = 5;
    public bool AutoStartMonitoring { get; set; } = true;
    public bool TrackProviderAvailability { get; set; } = true;
    public bool TrackModelPerformance { get; set; } = true;
    public bool EnableAlerts { get; set; } = false;
    public List<string> AlertEmails { get; set; } = new();
    public int ConsecutiveFailuresBeforeAlert { get; set; } = 3;
}

// src/LLMGateway.Infrastructure/Monitoring/Extensions/MonitoringExtensions.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LLMGateway.Infrastructure.Monitoring.Extensions;

public static class MonitoringExtensions
{
    public static IServiceCollection AddMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        // Register options
        services.Configure<MonitoringOptions>(configuration.GetSection("Monitoring"));
        
        // Register services
        services.AddSingleton<IProviderHealthMonitorService, ProviderHealthMonitorService>();
        
        // Register background service
        services.AddHostedService<MonitoringBackgroundService>();
        
        return services;
    }
}

// src/LLMGateway.Infrastructure/Monitoring/MonitoringBackgroundService.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Infrastructure.Monitoring;

public class MonitoringBackgroundService : BackgroundService
{
    private readonly IProviderHealthMonitorService _healthMonitorService;
    private readonly IOptions<MonitoringOptions> _monitoringOptions;
    private readonly ILogger<MonitoringBackgroundService> _logger;
    
    public MonitoringBackgroundService(
        IProviderHealthMonitorService healthMonitorService,
        IOptions<MonitoringOptions> monitoringOptions,
        ILogger<MonitoringBackgroundService> logger)
    {
        _healthMonitorService = healthMonitorService;
        _monitoringOptions = monitoringOptions;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monitoring Background Service is starting");
        
        try
        {
            if (_monitoringOptions.Value.EnableHealthMonitoring && _monitoringOptions.Value.AutoStartMonitoring)
            {
                _logger.LogInformation("Auto-starting provider health monitoring");
                await _healthMonitorService.StartMonitoringAsync(stoppingToken);
            }
            
            // The actual monitoring is handled by the ProviderHealthMonitorService
            // This just keeps the background service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
            _logger.LogInformation("Monitoring Background Service was stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Monitoring Background Service: {ErrorMessage}", ex.Message);
        }
        finally
        {
            if (_monitoringOptions.Value.EnableHealthMonitoring)
            {
                await _healthMonitorService.StopMonitoringAsync();
            }
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Monitoring Background Service is stopping");
        
        if (_monitoringOptions.Value.EnableHealthMonitoring)
        {
            await _healthMonitorService.StopMonitoringAsync();
        }
        
        await base.StopAsync(cancellationToken);
    }
}
