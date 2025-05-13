namespace LLMGateway.Core.Options;

/// <summary>
/// Options for fallback behavior
/// </summary>
public class FallbackOptions
{
    /// <summary>
    /// Whether to enable fallbacks
    /// </summary>
    public bool EnableFallbacks { get; init; } = true;
    
    /// <summary>
    /// Maximum number of fallback attempts
    /// </summary>
    public int MaxFallbackAttempts { get; init; } = 3;
    
    /// <summary>
    /// Fallback rules
    /// </summary>
    public List<FallbackRule> Rules { get; init; } = new();
}

/// <summary>
/// Rule for fallback behavior
/// </summary>
public class FallbackRule
{
    /// <summary>
    /// Model ID to apply the rule to
    /// </summary>
    public string ModelId { get; init; } = string.Empty;
    
    /// <summary>
    /// Fallback models to try in order
    /// </summary>
    public List<string> FallbackModels { get; init; } = new();
    
    /// <summary>
    /// Error codes that trigger fallback
    /// </summary>
    public List<string> ErrorCodes { get; init; } = new();
}
