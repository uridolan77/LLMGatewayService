using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LLMGateway.Infrastructure.Logging;

/// <summary>
/// Extensions for configuring logging
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Add LLMGateway logging configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddLLMGatewayLogging(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .CreateLogger();
        
        services.AddSingleton(Log.Logger);
        
        return services;
    }
}
