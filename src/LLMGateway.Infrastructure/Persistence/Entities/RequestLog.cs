namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Request log entity
/// </summary>
public class RequestLog
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
    /// API key ID
    /// </summary>
    public string ApiKeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Request path
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// HTTP method
    /// </summary>
    public string Method { get; set; } = string.Empty;
    
    /// <summary>
    /// Status code
    /// </summary>
    public int StatusCode { get; set; }
    
    /// <summary>
    /// Request type
    /// </summary>
    public string RequestType { get; set; } = string.Empty;
    
    /// <summary>
    /// Model ID
    /// </summary>
    public string? ModelId { get; set; }
    
    /// <summary>
    /// Request size in bytes
    /// </summary>
    public long RequestSizeBytes { get; set; }
    
    /// <summary>
    /// Response size in bytes
    /// </summary>
    public long ResponseSizeBytes { get; set; }
    
    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
    
    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Request headers (JSON)
    /// </summary>
    public string? RequestHeaders { get; set; }
    
    /// <summary>
    /// Request body (may be truncated)
    /// </summary>
    public string? RequestBody { get; set; }
    
    /// <summary>
    /// Response headers (JSON)
    /// </summary>
    public string? ResponseHeaders { get; set; }
    
    /// <summary>
    /// Response body (may be truncated)
    /// </summary>
    public string? ResponseBody { get; set; }
    
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 400;
    
    /// <summary>
    /// User
    /// </summary>
    public virtual User? User { get; set; }
    
    /// <summary>
    /// API key
    /// </summary>
    public virtual ApiKey? ApiKey { get; set; }
}
