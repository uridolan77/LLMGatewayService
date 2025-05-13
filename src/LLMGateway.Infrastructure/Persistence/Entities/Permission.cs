using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Permission entity
/// </summary>
public class Permission
{
    /// <summary>
    /// ID
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// User that owns this permission
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
