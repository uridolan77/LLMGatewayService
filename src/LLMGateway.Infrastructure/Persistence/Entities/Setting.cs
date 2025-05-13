namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Setting entity
/// </summary>
public class Setting
{
    /// <summary>
    /// Gets or sets the ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the value is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp
    /// </summary>
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets who modified the setting
    /// </summary>
    public string? ModifiedBy { get; set; }
}
