namespace LLMGateway.Core.Options;

/// <summary>
/// Options for retry policies
/// </summary>
public class RetryPolicyOptions
{
    /// <summary>
    /// Maximum number of retry attempts for general operations
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;
    
    /// <summary>
    /// Maximum number of retry attempts for provider operations
    /// </summary>
    public int MaxProviderRetryAttempts { get; init; } = 2;
    
    /// <summary>
    /// Base retry interval in seconds
    /// </summary>
    public double BaseRetryIntervalSeconds { get; init; } = 1.0;
}
