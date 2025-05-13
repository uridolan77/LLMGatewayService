namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Routing decision entity
/// </summary>
public class RoutingDecision
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Requested model ID
    /// </summary>
    public string RequestedModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Selected model ID
    /// </summary>
    public string SelectedModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Routing strategy used
    /// </summary>
    public string Strategy { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool WasSuccessful { get; set; }
    
    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; set; }
    
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Additional details (JSON)
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Request content
    /// </summary>
    public string? RequestContent { get; set; }
    
    /// <summary>
    /// Request token count
    /// </summary>
    public int? RequestTokenCount { get; set; }
    
    /// <summary>
    /// Whether this is a fallback decision
    /// </summary>
    public bool IsFallback { get; set; }
    
    /// <summary>
    /// Fallback reason
    /// </summary>
    public string? FallbackReason { get; set; }
    
    /// <summary>
    /// Routing reason
    /// </summary>
    public string? RoutingReason { get; set; }
    
    /// <summary>
    /// User
    /// </summary>
    public virtual User User { get; set; } = null!;
}
