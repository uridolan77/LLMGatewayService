using LLMGateway.Core.Models.Completion;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Service for creating completions
/// </summary>
public interface ICompletionService
{
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
}
