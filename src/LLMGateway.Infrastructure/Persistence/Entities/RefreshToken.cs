using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// RefreshToken entity
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Token ID
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Token value
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Token expiration date
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
    
    /// <summary>
    /// Date the token was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Date the token was revoked
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
    
    /// <summary>
    /// IP address that created the token
    /// </summary>
    public string CreatedByIp { get; set; } = string.Empty;
    
    /// <summary>
    /// IP address that revoked the token
    /// </summary>
    public string? RevokedByIp { get; set; }
    
    /// <summary>
    /// Reason the token was revoked
    /// </summary>
    public string? ReasonRevoked { get; set; }
    
    /// <summary>
    /// User that owns this token
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// Whether the token is expired
    /// </summary>
    [NotMapped]
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    
    /// <summary>
    /// Whether the token is revoked
    /// </summary>
    [NotMapped]
    public bool IsRevoked => RevokedAt != null;
    
    /// <summary>
    /// Whether the token is active
    /// </summary>
    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;
}
