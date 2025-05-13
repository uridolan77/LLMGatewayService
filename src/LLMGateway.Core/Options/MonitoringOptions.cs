namespace LLMGateway.Core.Options;

/// <summary>
/// Options for monitoring
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// Whether to enable health monitoring
    /// </summary>
    public bool EnableHealthMonitoring { get; set; } = true;
    
    /// <summary>
    /// Health check interval in minutes
    /// </summary>
    public int HealthCheckIntervalMinutes { get; set; } = 5;
    
    /// <summary>
    /// Whether to automatically start monitoring
    /// </summary>
    public bool AutoStartMonitoring { get; set; } = true;
    
    /// <summary>
    /// Whether to track provider availability
    /// </summary>
    public bool TrackProviderAvailability { get; set; } = true;
    
    /// <summary>
    /// Whether to track model performance
    /// </summary>
    public bool TrackModelPerformance { get; set; } = true;
    
    /// <summary>
    /// Whether to enable alerts
    /// </summary>
    public bool EnableAlerts { get; set; } = true;
    
    /// <summary>
    /// Email addresses to send alerts to
    /// </summary>
    public List<string> AlertEmails { get; set; } = new();
    
    /// <summary>
    /// Number of consecutive failures before sending an alert
    /// </summary>
    public int ConsecutiveFailuresBeforeAlert { get; set; } = 3;
}
