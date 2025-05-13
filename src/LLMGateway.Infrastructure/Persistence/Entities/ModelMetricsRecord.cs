namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Model metrics record entity
/// </summary>
public class ModelMetricsRecord
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Request count
    /// </summary>
    public int RequestCount { get; set; }
    
    /// <summary>
    /// Success count
    /// </summary>
    public int SuccessCount { get; set; }
    
    /// <summary>
    /// Failure count
    /// </summary>
    public int FailureCount { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
    
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public long AverageResponseTimeMs { get; set; }
    
    /// <summary>
    /// Total cost in USD
    /// </summary>
    public decimal TotalCostUsd { get; set; }
}
