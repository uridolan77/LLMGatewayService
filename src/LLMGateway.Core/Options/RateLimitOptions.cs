namespace LLMGateway.Core.Options;

/// <summary>
/// Options for rate limiting
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Token limit for the token bucket algorithm
    /// </summary>
    public int TokenLimit { get; set; } = 100;
    
    /// <summary>
    /// Tokens per period for the token bucket algorithm
    /// </summary>
    public int TokensPerPeriod { get; set; } = 10;
    
    /// <summary>
    /// Replenishment period in seconds for the token bucket algorithm
    /// </summary>
    public int ReplenishmentPeriodSeconds { get; set; } = 1;
    
    /// <summary>
    /// Queue limit for the token bucket algorithm
    /// </summary>
    public int QueueLimit { get; set; } = 50;
}
