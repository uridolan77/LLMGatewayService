using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

        // Add OpenTelemetry
        services.AddOpenTelemetryServices(configuration);

        return services;
    }

    /// <summary>
    /// Add OpenTelemetry services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddOpenTelemetryServices(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "LLMGateway";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["service.instance.id"] = Environment.MachineName,
                    ["service.namespace"] = "LLMGateway"
                }))
            .WithTracing(builder =>
            {
                builder
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            // Filter out health check requests
                            return !httpContext.Request.Path.StartsWithSegments("/health");
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.FilterHttpRequestMessage = request =>
                        {
                            // Filter out health check requests
                            return !request.RequestUri?.AbsolutePath.Contains("/health") == true;
                        };
                    })
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.RecordException = true;
                    })
                    .AddRedisInstrumentation()
                    .AddSource("LLMGateway")
                    .AddSource("LLMGateway.Providers")
                    .AddSource("LLMGateway.Core");

                // Add OTLP exporter if configured
                var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    builder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithMetrics(builder =>
            {
                builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("LLMGateway")
                    .AddMeter("LLMGateway.Providers")
                    .AddMeter("LLMGateway.Core");

                // Add OTLP exporter if configured
                var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    builder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });

        // Register custom metrics service
        services.AddSingleton<IMetricsService, MetricsService>();

        return services;
    }
}
