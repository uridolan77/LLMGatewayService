namespace LLMGateway.Core.Options;

/// <summary>
/// Global options for the LLM Gateway
/// </summary>
public class GlobalOptions
{
    /// <summary>
    /// Whether to enable caching
    /// </summary>
    public bool EnableCaching { get; init; } = true;

    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; init; } = 60;

    /// <summary>
    /// Whether to track token usage
    /// </summary>
    public bool TrackTokenUsage { get; init; } = true;

    /// <summary>
    /// Whether to track cost
    /// </summary>
    public bool EnableCostTracking { get; init; } = true;

    /// <summary>
    /// Whether to enforce budgets
    /// </summary>
    public bool EnableBudgetEnforcement { get; init; } = true;

    /// <summary>
    /// Whether to enable provider discovery
    /// </summary>
    public bool EnableProviderDiscovery { get; init; } = true;

    /// <summary>
    /// Default timeout for requests in seconds
    /// </summary>
    public int DefaultTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Default timeout for streaming requests in seconds
    /// </summary>
    public int DefaultStreamTimeoutSeconds { get; init; } = 120;
}
