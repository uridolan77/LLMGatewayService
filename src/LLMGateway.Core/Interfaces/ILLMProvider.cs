using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.Provider;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for an LLM provider
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Name of the provider
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether the provider supports multi-modal inputs
    /// </summary>
    bool SupportsMultiModal { get; }

    /// <summary>
    /// Whether the provider supports streaming
    /// </summary>
    bool SupportsStreaming { get; }

    /// <summary>
    /// Get information about all models supported by the provider
    /// </summary>
    /// <returns>List of model information</returns>
    Task<IEnumerable<ModelInfo>> GetModelsAsync();

    /// <summary>
    /// Get information about a specific model
    /// </summary>
    /// <param name="modelId">ID of the model</param>
    /// <returns>Model information</returns>
    Task<ModelInfo> GetModelAsync(string modelId);

    /// <summary>
    /// Create a completion
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion response</returns>
    Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a streaming completion
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of completion responses</returns>
    IAsyncEnumerable<CompletionResponse> CreateCompletionStreamAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a multi-modal completion
    /// </summary>
    /// <param name="request">Multi-modal completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion response</returns>
    Task<CompletionResponse> CreateMultiModalCompletionAsync(MultiModalCompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a streaming multi-modal completion
    /// </summary>
    /// <param name="request">Multi-modal completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of completion chunks</returns>
    IAsyncEnumerable<CompletionChunk> CreateStreamingMultiModalCompletionAsync(MultiModalCompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an embedding
    /// </summary>
    /// <param name="request">Embedding request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding response</returns>
    Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is available
    /// </summary>
    /// <returns>True if the provider is available</returns>
    Task<bool> IsAvailableAsync();
}
