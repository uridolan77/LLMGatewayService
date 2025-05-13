namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Provider health record entity
/// </summary>
public class ProviderHealthRecord
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the provider is available
    /// </summary>
    public bool IsAvailable { get; set; }
    
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; set; }
    
    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
}
