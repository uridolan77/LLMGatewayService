namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Provider configuration entity
/// </summary>
public class ProviderConfiguration
{
    /// <summary>
    /// Gets or sets the ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the API URL
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether the provider is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets additional configuration
    /// </summary>
    public string? AdditionalConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the created at timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last modified timestamp
    /// </summary>
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
}
