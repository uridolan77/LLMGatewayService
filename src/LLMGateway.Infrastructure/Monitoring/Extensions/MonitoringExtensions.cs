using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LLMGateway.Infrastructure.Monitoring.Extensions;

/// <summary>
/// Extensions for monitoring
/// </summary>
public static class MonitoringExtensions
{
    /// <summary>
    /// Add monitoring
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        var monitoringOptions = new MonitoringOptions();
        configuration.GetSection("Monitoring").Bind(monitoringOptions);
        
        if (!monitoringOptions.EnableHealthMonitoring)
        {
            return services;
        }
        
        // Add health monitoring services
        services.AddSingleton<IProviderHealthMonitor, ProviderHealthMonitor>();
        services.AddSingleton<IModelPerformanceMonitor, ModelPerformanceMonitor>();
        
        // Add alerting service if enabled
        if (monitoringOptions.EnableAlerts)
        {
            services.AddSingleton<IAlertService, AlertService>();
        }
        
        // Add hosted service to start monitoring if auto-start is enabled
        if (monitoringOptions.AutoStartMonitoring)
        {
            services.AddHostedService<MonitoringHostedService>();
        }
        
        return services;
    }
}
