using LLMGateway.Core.Options;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;

namespace LLMGateway.Infrastructure.Telemetry;

/// <summary>
/// Telemetry initializer for LLM Gateway
/// </summary>
public class LLMGatewayTelemetryInitializer : ITelemetryInitializer
{
    private readonly TelemetryOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options">Telemetry options</param>
    public LLMGatewayTelemetryInitializer(IOptions<TelemetryOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc/>
    public void Initialize(ITelemetry telemetry)
    {
        if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
        {
            telemetry.Context.Cloud.RoleName = "LLMGateway";
        }
        
        // Add common properties
        telemetry.Context.GlobalProperties["Application"] = "LLMGateway";
        telemetry.Context.GlobalProperties["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    }
}
