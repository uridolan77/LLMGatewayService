namespace LLMGateway.Core.Options;

/// <summary>
/// Options for logging
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Whether to log request and response bodies
    /// </summary>
    public bool LogRequestResponseBodies { get; set; } = false;
    
    /// <summary>
    /// Whether to log sensitive information
    /// </summary>
    public bool LogSensitiveInformation { get; set; } = false;
    
    /// <summary>
    /// Whether to log token usage
    /// </summary>
    public bool LogTokenUsage { get; set; } = true;
    
    /// <summary>
    /// Whether to log performance metrics
    /// </summary>
    public bool LogPerformanceMetrics { get; set; } = true;
    
    /// <summary>
    /// Whether to log routing decisions
    /// </summary>
    public bool LogRoutingDecisions { get; set; } = true;
}
