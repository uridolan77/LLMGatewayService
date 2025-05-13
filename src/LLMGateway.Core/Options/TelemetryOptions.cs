namespace LLMGateway.Core.Options;

/// <summary>
/// Options for telemetry
/// </summary>
public class TelemetryOptions
{
    /// <summary>
    /// Whether to enable telemetry
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;
    
    /// <summary>
    /// Application Insights connection string
    /// </summary>
    public string ApplicationInsightsConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to track performance
    /// </summary>
    public bool TrackPerformance { get; set; } = true;
    
    /// <summary>
    /// Whether to track exceptions
    /// </summary>
    public bool TrackExceptions { get; set; } = true;
    
    /// <summary>
    /// Whether to track dependencies
    /// </summary>
    public bool TrackDependencies { get; set; } = true;
    
    /// <summary>
    /// Whether to enrich telemetry with user information
    /// </summary>
    public bool EnrichWithUserInfo { get; set; } = true;
}
