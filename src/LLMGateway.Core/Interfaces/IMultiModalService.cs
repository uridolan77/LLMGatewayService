using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Provider;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for multi-modal service
/// </summary>
public interface IMultiModalService
{
    /// <summary>
    /// Create multi-modal completion
    /// </summary>
    /// <param name="request">Multi-modal completion request</param>
    /// <returns>Completion response</returns>
    Task<CompletionResponse> CreateMultiModalCompletionAsync(MultiModalCompletionRequest request);

    /// <summary>
    /// Create streaming multi-modal completion
    /// </summary>
    /// <param name="request">Multi-modal completion request</param>
    /// <returns>Stream of completion chunks</returns>
    IAsyncEnumerable<CompletionChunk> CreateStreamingMultiModalCompletionAsync(MultiModalCompletionRequest request);

    /// <summary>
    /// Get models that support multi-modal inputs
    /// </summary>
    /// <returns>List of multi-modal models</returns>
    Task<IEnumerable<Model>> GetMultiModalModelsAsync();

    /// <summary>
    /// Check if a model supports multi-modal inputs
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>True if the model supports multi-modal inputs</returns>
    Task<bool> SupportsMultiModalAsync(string modelId);
}
