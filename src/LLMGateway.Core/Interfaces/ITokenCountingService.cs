using LLMGateway.Core.Models.Tokenization;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for token counting service
/// </summary>
public interface ITokenCountingService
{
    /// <summary>
    /// Count tokens in text
    /// </summary>
    /// <param name="text">Text to count tokens in</param>
    /// <param name="modelId">Model ID</param>
    /// <returns>Number of tokens</returns>
    int CountTokens(string text, string modelId);
    
    /// <summary>
    /// Estimate tokens for a completion request
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>Token count estimate</returns>
    Task<TokenCountEstimate> EstimateTokensAsync(Models.Completion.CompletionRequest request);
    
    /// <summary>
    /// Estimate tokens for an embedding request
    /// </summary>
    /// <param name="request">Embedding request</param>
    /// <returns>Token count estimate</returns>
    Task<TokenCountEstimate> EstimateTokensAsync(Models.Embedding.EmbeddingRequest request);
}
