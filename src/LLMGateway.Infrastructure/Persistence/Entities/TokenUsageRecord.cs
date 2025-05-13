namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Token usage record entity
/// </summary>
public class TokenUsageRecord
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Timestamp
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
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Request type
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
    
    /// <summary>
    /// User
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// API key
    /// </summary>
    public virtual ApiKey ApiKey { get; set; } = null!;
}
