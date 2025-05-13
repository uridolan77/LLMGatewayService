namespace LLMGateway.Core.Options;

/// <summary>
/// Options for token usage tracking
/// </summary>
public class TokenUsageOptions
{
    /// <summary>
    /// Whether to enable token counting
    /// </summary>
    public bool EnableTokenCounting { get; set; } = true;
    
    /// <summary>
    /// Storage provider for token usage data
    /// </summary>
    public string StorageProvider { get; set; } = "InMemory";
    
    /// <summary>
    /// Data retention period
    /// </summary>
    public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromDays(90);
    
    /// <summary>
    /// Whether to enable alerts
    /// </summary>
    public bool EnableAlerts { get; set; } = false;
    
    /// <summary>
    /// Alert threshold percentage
    /// </summary>
    public int AlertThresholdPercentage { get; set; } = 80;
}
