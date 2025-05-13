using LLMGateway.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Monitoring;

/// <summary>
/// Hosted service for monitoring
/// </summary>
public class MonitoringHostedService : IHostedService
{
    private readonly IProviderHealthMonitor _providerHealthMonitor;
    private readonly IModelPerformanceMonitor _modelPerformanceMonitor;
    private readonly ILogger<MonitoringHostedService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="providerHealthMonitor">Provider health monitor</param>
    /// <param name="modelPerformanceMonitor">Model performance monitor</param>
    /// <param name="logger">Logger</param>
    public MonitoringHostedService(
        IProviderHealthMonitor providerHealthMonitor,
        IModelPerformanceMonitor modelPerformanceMonitor,
        ILogger<MonitoringHostedService> logger)
    {
        _providerHealthMonitor = providerHealthMonitor;
        _modelPerformanceMonitor = modelPerformanceMonitor;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting monitoring hosted service");
        
        _providerHealthMonitor.Start();
        _modelPerformanceMonitor.Start();
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping monitoring hosted service");
        
        _providerHealthMonitor.Stop();
        _modelPerformanceMonitor.Stop();
        
        return Task.CompletedTask;
    }
}
