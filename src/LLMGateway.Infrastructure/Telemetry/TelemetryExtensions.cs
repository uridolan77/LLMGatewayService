using LLMGateway.Core.Options;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LLMGateway.Infrastructure.Telemetry;

/// <summary>
/// Extensions for telemetry
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Add telemetry
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var telemetryOptions = new TelemetryOptions();
        configuration.GetSection("Telemetry").Bind(telemetryOptions);
        
        if (!telemetryOptions.EnableTelemetry)
        {
            services.AddSingleton<ITelemetryService, NullTelemetryService>();
            return services;
        }
        
        // Configure Application Insights
        if (!string.IsNullOrEmpty(telemetryOptions.ApplicationInsightsConnectionString))
        {
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = telemetryOptions.ApplicationInsightsConnectionString;
            });
            
            services.AddSingleton<ITelemetryInitializer, LLMGatewayTelemetryInitializer>();
            services.AddSingleton<ITelemetryService, TelemetryService>();
        }
        else
        {
            // When no valid connection string is provided, register an empty ApplicationInsights setup
            // and use the null implementation for our service
            services.AddApplicationInsightsTelemetry(); // This creates a TelemetryClient with default config
            services.AddSingleton<ITelemetryService, NullTelemetryService>();
        }
        
        return services;
    }
}
