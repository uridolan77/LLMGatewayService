using LLMGateway.Core.Models.Embedding;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Service for creating embeddings
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Create an embedding
    /// </summary>
    /// <param name="request">Embedding request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding response</returns>
    Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);
}
