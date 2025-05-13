using System.Text.Json.Serialization;

namespace LLMGateway.Core.Models.Embedding;

/// <summary>
/// Response from an embedding request
/// </summary>
public class EmbeddingResponse
{
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = "list";
    
    /// <summary>
    /// Model used for the embedding
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider used for the embedding
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Embedding data
    /// </summary>
    public List<EmbeddingData> Data { get; set; } = new();
    
    /// <summary>
    /// Usage statistics
    /// </summary>
    public EmbeddingUsage Usage { get; set; } = new();
    
    /// <summary>
    /// Additional provider-specific parameters
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}

/// <summary>
/// Embedding data
/// </summary>
public class EmbeddingData
{
    /// <summary>
    /// Index of the embedding
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = "embedding";
    
    /// <summary>
    /// Embedding vector
    /// </summary>
    public List<float> Embedding { get; set; } = new();
}

/// <summary>
/// Usage statistics for an embedding
/// </summary>
public class EmbeddingUsage
{
    /// <summary>
    /// Prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
}
