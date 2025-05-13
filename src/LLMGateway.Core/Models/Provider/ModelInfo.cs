using System.Text.Json.Serialization;

namespace LLMGateway.Core.Models.Provider;

/// <summary>
/// Information about a model
/// </summary>
public class ModelInfo
{
    /// <summary>
    /// ID of the model
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the model
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Provider of the model
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific model ID
    /// </summary>
    public string ProviderModelId { get; set; } = string.Empty;

    /// <summary>
    /// Context window size
    /// </summary>
    public int ContextWindow { get; set; }

    /// <summary>
    /// Whether the model supports completions
    /// </summary>
    public bool SupportsCompletions { get; set; } = true;

    /// <summary>
    /// Whether the model supports embeddings
    /// </summary>
    public bool SupportsEmbeddings { get; set; } = false;

    /// <summary>
    /// Whether the model supports streaming
    /// </summary>
    public bool SupportsStreaming { get; set; } = true;

    /// <summary>
    /// Whether the model supports function calling
    /// </summary>
    public bool SupportsFunctionCalling { get; set; } = false;

    /// <summary>
    /// Whether the model supports vision
    /// </summary>
    public bool SupportsVision { get; set; } = false;

    /// <summary>
    /// Cost per 1K input tokens in USD
    /// </summary>
    public decimal InputPricePerToken { get; set; }

    /// <summary>
    /// Cost per 1K output tokens in USD
    /// </summary>
    public decimal OutputPricePerToken { get; set; }

    /// <summary>
    /// Additional properties of the model
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Additional provider-specific parameters
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}
