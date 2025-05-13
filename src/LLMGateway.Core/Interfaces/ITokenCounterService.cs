using LLMGateway.Core.Models.Completion;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for token counting service
/// </summary>
public interface ITokenCounterService
{
    /// <summary>
    /// Count tokens in a text
    /// </summary>
    /// <param name="text">Text to count tokens in</param>
    /// <param name="modelId">Optional model ID to use for counting</param>
    /// <returns>Number of tokens</returns>
    Task<int> CountTokensAsync(string text, string? modelId = null);
    
    /// <summary>
    /// Count tokens in a completion request
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>Number of tokens</returns>
    Task<int> CountTokensAsync(CompletionRequest request);
}
