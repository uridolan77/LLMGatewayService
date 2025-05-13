using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Core.Models.Routing;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Router for selecting the appropriate model for a request
/// </summary>
public interface IModelRouter
{
    /// <summary>
    /// Route a completion request to the appropriate model
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>Routing result</returns>
    Task<RoutingResult> RouteCompletionRequestAsync(CompletionRequest request);
    
    /// <summary>
    /// Route an embedding request to the appropriate model
    /// </summary>
    /// <param name="request">Embedding request</param>
    /// <returns>Routing result</returns>
    Task<RoutingResult> RouteEmbeddingRequestAsync(EmbeddingRequest request);
    
    /// <summary>
    /// Get fallback models for a model
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="errorCode">Error code</param>
    /// <returns>List of fallback models</returns>
    Task<IEnumerable<string>> GetFallbackModelsAsync(string modelId, string? errorCode = null);
}

/// <summary>
/// Result of routing a request
/// </summary>
public class RoutingResult
{
    /// <summary>
    /// Provider to use
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Model ID to use
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider-specific model ID
    /// </summary>
    public string ProviderModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Routing strategy used
    /// </summary>
    public string RoutingStrategy { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the routing was successful
    /// </summary>
    public bool Success { get; set; } = true;
    
    /// <summary>
    /// Error message if routing failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Reason for the routing decision
    /// </summary>
    public string RoutingReason { get; set; } = string.Empty;
}
