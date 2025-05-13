using System.Text.Json.Serialization;

namespace LLMGateway.Core.Models.Routing;

/// <summary>
/// Mapping between a model ID and a provider
/// </summary>
public class ModelMapping
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider-specific model ID
    /// </summary>
    public string ProviderModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name of the model
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Context window size
    /// </summary>
    public int ContextWindow { get; set; }
    
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
