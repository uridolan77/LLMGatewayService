namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Base entity for all entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
}
