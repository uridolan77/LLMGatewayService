using System.Text.Json.Serialization;

namespace LLMGateway.Core.Models.Embedding;

/// <summary>
/// Request for an embedding
/// </summary>
public class EmbeddingRequest
{
    /// <summary>
    /// Model ID to use for the embedding
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Input text to embed
    /// </summary>
    public object Input { get; set; } = new();

    /// <summary>
    /// User identifier
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Project identifier
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Tags for cost tracking and analytics
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Dimensions of the embedding
    /// </summary>
    public int? Dimensions { get; set; }

    /// <summary>
    /// Encoding format
    /// </summary>
    public string? EncodingFormat { get; set; }

    /// <summary>
    /// Additional provider-specific parameters
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}
