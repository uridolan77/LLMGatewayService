using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Persistence;
using LLMGateway.Infrastructure.Persistence.Entities;
using LLMGateway.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LLMGateway.Infrastructure.Monitoring;

/// <summary>
/// Provider health monitor
/// </summary>
public class ProviderHealthMonitor : IProviderHealthMonitor
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IAlertService? _alertService;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<ProviderHealthMonitor> _logger;
    private readonly MonitoringOptions _options;
    private readonly ConcurrentDictionary<string, bool> _providerHealthStatus = new();
    private readonly ConcurrentDictionary<string, int> _consecutiveFailures = new();
    private Timer? _timer;
    private bool _isRunning;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="providerFactory">Provider factory</param>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="alertService">Alert service</param>
    /// <param name="telemetryService">Telemetry service</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Monitoring options</param>
    public ProviderHealthMonitor(
        ILLMProviderFactory providerFactory,
        IServiceScopeFactory serviceScopeFactory,
        IAlertService? alertService,
        ITelemetryService telemetryService,
        ILogger<ProviderHealthMonitor> logger,
        IOptions<MonitoringOptions> options)
    {
        _providerFactory = providerFactory;
        _serviceScopeFactory = serviceScopeFactory;
        _alertService = alertService;
        _telemetryService = telemetryService;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public void Start()
    {
        if (_isRunning)
        {
            return;
        }
        
        _logger.LogInformation("Starting provider health monitoring");
        
        _timer = new Timer(
            CheckProvidersCallback,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(_options.HealthCheckIntervalMinutes));
        
        _isRunning = true;
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }
        
        _logger.LogInformation("Stopping provider health monitoring");
        
        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();
        _timer = null;
        
        _isRunning = false;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, bool>> CheckProvidersAsync()
    {
        using var operation = _telemetryService.TrackOperation("ProviderHealthMonitor.CheckProvidersAsync");
        
        _logger.LogInformation("Checking provider health");
        
        var providers = _providerFactory.GetAllProviders();
        var healthStatus = new Dictionary<string, bool>();
        
        foreach (var provider in providers)
        {
            var stopwatch = Stopwatch.StartNew();
            bool isAvailable = false;
            string? errorMessage = null;
            
            try
            {
                isAvailable = await provider.IsAvailableAsync();
                stopwatch.Stop();
                
                _logger.LogInformation("Provider {Provider} is {Status} (response time: {ResponseTime}ms)",
                    provider.Name, isAvailable ? "available" : "unavailable", stopwatch.ElapsedMilliseconds);
                
                healthStatus[provider.Name] = isAvailable;
                _providerHealthStatus[provider.Name] = isAvailable;
                
                // Track consecutive failures
                if (!isAvailable)
                {
                    _consecutiveFailures.AddOrUpdate(
                        provider.Name,
                        1,
                        (_, count) => count + 1);
                    
                    // Send alert if consecutive failures exceed threshold
                    if (_options.EnableAlerts && _alertService != null && 
                        _consecutiveFailures[provider.Name] >= _options.ConsecutiveFailuresBeforeAlert)
                    {
                        await _alertService.SendProviderUnavailableAlertAsync(provider.Name);
                    }
                }
                else
                {
                    _consecutiveFailures[provider.Name] = 0;
                }
                
                // Track telemetry
                _telemetryService.TrackEvent("ProviderHealthCheck",
                    new Dictionary<string, string>
                    {
                        ["Provider"] = provider.Name,
                        ["Status"] = isAvailable ? "Available" : "Unavailable"
                    },
                    new Dictionary<string, double>
                    {
                        ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
                    });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex, "Failed to check health for provider {Provider}", provider.Name);
                
                healthStatus[provider.Name] = false;
                _providerHealthStatus[provider.Name] = false;
                errorMessage = ex.Message;
                
                // Track consecutive failures
                _consecutiveFailures.AddOrUpdate(
                    provider.Name,
                    1,
                    (_, count) => count + 1);
                
                // Send alert if consecutive failures exceed threshold
                if (_options.EnableAlerts && _alertService != null && 
                    _consecutiveFailures[provider.Name] >= _options.ConsecutiveFailuresBeforeAlert)
                {
                    await _alertService.SendProviderUnavailableAlertAsync(provider.Name, ex.Message);
                }
                
                // Track telemetry
                _telemetryService.TrackException(ex,
                    new Dictionary<string, string>
                    {
                        ["Provider"] = provider.Name,
                        ["Operation"] = "HealthCheck"
                    });
            }
            
            // Store health record in database if enabled
            if (_options.TrackProviderAvailability)
            {
                try
                {
                    // Create a new scope to use the DbContext
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<LLMGatewayDbContext>();
                        
                        var record = new ProviderHealthRecord
                        {
                            Provider = provider.Name,
                            IsAvailable = isAvailable,
                            Timestamp = DateTimeOffset.UtcNow,
                            ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                            ErrorMessage = errorMessage
                        };
                        
                        await dbContext.ProviderHealthRecords.AddAsync(record);
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to store provider health record for {Provider}", provider.Name);
                }
            }
        }
        
        return healthStatus;
    }

    /// <inheritdoc/>
    public Dictionary<string, bool> GetProviderHealthStatus()
    {
        return new Dictionary<string, bool>(_providerHealthStatus);
    }

    private async void CheckProvidersCallback(object? state)
    {
        try
        {
            await CheckProvidersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check provider health");
        }
    }
}
