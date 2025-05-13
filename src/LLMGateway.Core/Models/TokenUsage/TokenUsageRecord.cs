namespace LLMGateway.Core.Models.TokenUsage;

/// <summary>
/// Record of token usage
/// </summary>
public class TokenUsageRecord
{
    /// <summary>
    /// ID of the record
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Timestamp of the record
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// API key ID
    /// </summary>
    public string ApiKeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Request ID
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
    
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider name
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of request
    /// </summary>
    public string RequestType { get; set; } = string.Empty;
    
    /// <summary>
    /// Prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Completion tokens
    /// </summary>
    public int CompletionTokens { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
    
    /// <summary>
    /// Estimated cost in USD
    /// </summary>
    public decimal EstimatedCostUsd { get; set; }
}
