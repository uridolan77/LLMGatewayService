namespace LLMGateway.Core.Models.Provider;

/// <summary>
/// Model information
/// </summary>
public class Model
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
    public bool SupportsEmbeddings { get; set; }
    
    /// <summary>
    /// Whether the model supports streaming
    /// </summary>
    public bool SupportsStreaming { get; set; }
    
    /// <summary>
    /// Whether the model supports function calling
    /// </summary>
    public bool SupportsFunctionCalling { get; set; }
    
    /// <summary>
    /// Whether the model supports vision
    /// </summary>
    public bool SupportsVision { get; set; }
    
    /// <summary>
    /// Cost per 1K prompt tokens in USD
    /// </summary>
    public decimal CostPer1kPromptTokensUsd { get; set; }
    
    /// <summary>
    /// Cost per 1K completion tokens in USD
    /// </summary>
    public decimal CostPer1kCompletionTokensUsd { get; set; }
}
