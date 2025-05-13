using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace LLMGateway.Infrastructure.Jobs;

/// <summary>
/// Job for checking provider health
/// </summary>
[DisallowConcurrentExecution]
public class ProviderHealthCheckJob : IJob
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<ProviderHealthCheckJob> _logger;
    private readonly MonitoringOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="providerFactory">Provider factory</param>
    /// <param name="telemetryService">Telemetry service</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Monitoring options</param>
    public ProviderHealthCheckJob(
        ILLMProviderFactory providerFactory,
        ITelemetryService telemetryService,
        ILogger<ProviderHealthCheckJob> logger,
        IOptions<MonitoringOptions> options)
    {
        _providerFactory = providerFactory;
        _telemetryService = telemetryService;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task Execute(IJobExecutionContext context)
    {
        using var operation = _telemetryService.TrackOperation("ProviderHealthCheckJob.Execute");
        
        try
        {
            _logger.LogInformation("Checking provider health");
            
            var providers = _providerFactory.GetAllProviders();
            var healthStatus = new Dictionary<string, bool>();
            
            foreach (var provider in providers)
            {
                try
                {
                    var isAvailable = await provider.IsAvailableAsync();
                    healthStatus[provider.Name] = isAvailable;
                    
                    _logger.LogInformation("Provider {Provider} is {Status}", 
                        provider.Name, isAvailable ? "available" : "unavailable");
                    
                    _telemetryService.TrackEvent("ProviderHealthCheck", 
                        new Dictionary<string, string>
                        {
                            ["Provider"] = provider.Name,
                            ["Status"] = isAvailable ? "Available" : "Unavailable"
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check health for provider {Provider}", provider.Name);
                    healthStatus[provider.Name] = false;
                    
                    _telemetryService.TrackException(ex, 
                        new Dictionary<string, string>
                        {
                            ["Provider"] = provider.Name,
                            ["Operation"] = "HealthCheck"
                        });
                }
            }
            
            // In a real implementation, this would send alerts if providers are unavailable
            if (_options.EnableAlerts)
            {
                var unavailableProviders = healthStatus.Where(p => !p.Value).Select(p => p.Key).ToList();
                if (unavailableProviders.Any())
                {
                    _logger.LogWarning("Providers unavailable: {Providers}", string.Join(", ", unavailableProviders));
                    
                    // Send alert
                    // This would be implemented in a real system
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check provider health");
            _telemetryService.TrackException(ex);
            throw;
        }
    }
}
