namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Model entity
/// </summary>
public class Model
{
    /// <summary>
    /// Gets or sets the ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider model ID
    /// </summary>
    public string ProviderModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the context window size
    /// </summary>
    public int ContextWindow { get; set; }

    /// <summary>
    /// Gets or sets whether the model supports completions
    /// </summary>
    public bool SupportsCompletions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the model supports embeddings
    /// </summary>
    public bool SupportsEmbeddings { get; set; }

    /// <summary>
    /// Gets or sets whether the model supports streaming
    /// </summary>
    public bool SupportsStreaming { get; set; }

    /// <summary>
    /// Gets or sets whether the model supports function calling
    /// </summary>
    public bool SupportsFunctionCalling { get; set; }

    /// <summary>
    /// Gets or sets whether the model supports vision
    /// </summary>
    public bool SupportsVision { get; set; }

    /// <summary>
    /// Gets or sets whether the model is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the created at timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the cost per 1K prompt tokens in USD
    /// </summary>
    public decimal CostPer1kPromptTokensUsd { get; set; }

    /// <summary>
    /// Gets or sets the cost per 1K completion tokens in USD
    /// </summary>
    public decimal CostPer1kCompletionTokensUsd { get; set; }
}
