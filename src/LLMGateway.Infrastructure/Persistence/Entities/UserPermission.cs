namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// User permission entity
/// </summary>
public class UserPermission
{
    /// <summary>
    /// Gets or sets the user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission
    /// </summary>
    public string Permission { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the permission is granted
    /// </summary>
    public bool IsGranted { get; set; } = true;

    /// <summary>
    /// Gets or sets who granted the permission
    /// </summary>
    public string? GrantedBy { get; set; }

    /// <summary>
    /// Gets or sets when the permission was granted
    /// </summary>
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who granted the permission
    /// </summary>
    public virtual User? GrantedByUser { get; set; }
}
