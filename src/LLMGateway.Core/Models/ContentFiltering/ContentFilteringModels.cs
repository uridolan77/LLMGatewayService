namespace LLMGateway.Core.Models.ContentFiltering;

/// <summary>
/// Content filter result
/// </summary>
public class ContentFilterResult
{
    /// <summary>
    /// Whether the content is allowed
    /// </summary>
    public bool IsAllowed { get; set; } = true;
    
    /// <summary>
    /// Reason for filtering
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Categories that triggered the filter
    /// </summary>
    public List<string> Categories { get; set; } = new();
    
    /// <summary>
    /// Scores for each category
    /// </summary>
    public Dictionary<string, float> Scores { get; set; } = new();
    
    /// <summary>
    /// Create a result for allowed content
    /// </summary>
    /// <returns>Content filter result</returns>
    public static ContentFilterResult Allowed()
    {
        return new ContentFilterResult
        {
            IsAllowed = true
        };
    }
    
    /// <summary>
    /// Create a result for filtered content
    /// </summary>
    /// <param name="reason">Reason for filtering</param>
    /// <param name="categories">Categories that triggered the filter</param>
    /// <returns>Content filter result</returns>
    public static ContentFilterResult Filtered(string reason, params string[] categories)
    {
        return new ContentFilterResult
        {
            IsAllowed = false,
            Reason = reason,
            Categories = categories.ToList()
        };
    }
}

/// <summary>
/// Content filtering options
/// </summary>
public class ContentFilteringOptions
{
    /// <summary>
    /// Whether to enable content filtering
    /// </summary>
    public bool EnableContentFiltering { get; set; } = true;
    
    /// <summary>
    /// Whether to filter prompts
    /// </summary>
    public bool FilterPrompts { get; set; } = true;
    
    /// <summary>
    /// Whether to filter completions
    /// </summary>
    public bool FilterCompletions { get; set; } = true;
    
    /// <summary>
    /// Threshold for hate content
    /// </summary>
    public float HateThreshold { get; set; } = 0.8f;
    
    /// <summary>
    /// Threshold for harassment content
    /// </summary>
    public float HarassmentThreshold { get; set; } = 0.8f;
    
    /// <summary>
    /// Threshold for self-harm content
    /// </summary>
    public float SelfHarmThreshold { get; set; } = 0.8f;
    
    /// <summary>
    /// Threshold for sexual content
    /// </summary>
    public float SexualThreshold { get; set; } = 0.8f;
    
    /// <summary>
    /// Threshold for violence content
    /// </summary>
    public float ViolenceThreshold { get; set; } = 0.8f;
    
    /// <summary>
    /// List of blocked terms
    /// </summary>
    public List<string> BlockedTerms { get; set; } = new();
    
    /// <summary>
    /// List of blocked patterns (regex)
    /// </summary>
    public List<string> BlockedPatterns { get; set; } = new();
}
