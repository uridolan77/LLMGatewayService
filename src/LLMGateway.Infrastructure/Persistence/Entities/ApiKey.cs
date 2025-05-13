namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// API key entity
/// </summary>
public class ApiKey
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// API key
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Created at
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Expires at
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether the API key is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Permissions
    /// </summary>
    public string Permissions { get; set; } = string.Empty;
    
    /// <summary>
    /// Daily token limit
    /// </summary>
    public int DailyTokenLimit { get; set; } = 100000;
    
    /// <summary>
    /// Monthly token limit
    /// </summary>
    public int MonthlyTokenLimit { get; set; } = 2000000;
    
    /// <summary>
    /// User
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// Token usage records
    /// </summary>
    public virtual ICollection<TokenUsageRecord> TokenUsageRecords { get; set; } = new List<TokenUsageRecord>();
}
