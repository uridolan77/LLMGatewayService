using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLMGateway.Core.Models.Auth;

/// <summary>
/// Refresh token entity
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
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Date the token was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Date the token was revoked
    /// </summary>
    public DateTime? RevokedAt { get; set; }
    
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
    /// Whether the token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    
    /// <summary>
    /// Whether the token is revoked
    /// </summary>
    public bool IsRevoked => RevokedAt != null;
    
    /// <summary>
    /// Whether the token is active
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;
}
