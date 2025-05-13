using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.Provider;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace LLMGateway.Providers.Base;

/// <summary>
/// Base class for LLM providers
/// </summary>
public abstract class BaseLLMProvider : ILLMProvider
{
    /// <summary>
    /// Logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    protected BaseLLMProvider(ILogger logger)
    {
        Logger = logger;
    }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public virtual bool SupportsMultiModal => false;

    /// <inheritdoc/>
    public virtual bool SupportsStreaming => true;

    /// <inheritdoc/>
    public abstract Task<IEnumerable<ModelInfo>> GetModelsAsync();

    /// <inheritdoc/>
    public abstract Task<ModelInfo> GetModelAsync(string modelId);

    /// <inheritdoc/>
    public abstract Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<CompletionResponse> CreateCompletionStreamAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<bool> IsAvailableAsync();

    /// <inheritdoc/>
    public virtual Task<CompletionResponse> CreateMultiModalCompletionAsync(MultiModalCompletionRequest request, CancellationToken cancellationToken = default)
    {
        // Default implementation for providers that don't support multi-modal inputs
        throw new NotSupportedException($"Provider {Name} does not support multi-modal inputs");
    }

    /// <inheritdoc/>
    public virtual IAsyncEnumerable<CompletionChunk> CreateStreamingMultiModalCompletionAsync(
        MultiModalCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Default implementation for providers that don't support multi-modal inputs
        throw new NotSupportedException($"Provider {Name} does not support multi-modal inputs");
    }

    /// <summary>
    /// Create a default streaming completion for providers that don't natively support streaming
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of completion responses</returns>
    protected async IAsyncEnumerable<CompletionResponse> CreateDefaultCompletionStreamAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Buffer for response to avoid yield within try-catch
        CompletionResponse? bufferedResponse = null;

        try
        {
            // For providers that don't support streaming natively, we can simulate it
            // by returning the full response as a single chunk
            bufferedResponse = await CreateCompletionAsync(request, cancellationToken);

            // Convert the response to a streaming format
            foreach (var choice in bufferedResponse.Choices)
            {
                // For streaming, we need to set the delta
                choice.Delta = choice.Message;
            }
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, "Failed to create streaming completion");
        }

        // Return the response outside the try-catch block
        if (bufferedResponse != null)
        {
            yield return bufferedResponse;
        }
    }

    /// <summary>
    /// Handle an exception from a provider
    /// </summary>
    /// <param name="ex">Exception</param>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Provider exception</returns>
    protected ProviderException HandleProviderException(Exception ex, string errorMessage)
    {
        Logger.LogError(ex, "{Provider}: {ErrorMessage}", Name, errorMessage);

        // Map common exception types to provider exceptions
        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return new RateLimitExceededException(Name, $"{errorMessage}: Rate limit exceeded");
            }

            if (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new ProviderAuthenticationException(Name, $"{errorMessage}: Authentication failed");
            }

            if (httpEx.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                httpEx.StatusCode == System.Net.HttpStatusCode.GatewayTimeout ||
                httpEx.StatusCode == System.Net.HttpStatusCode.BadGateway ||
                httpEx.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                return new ProviderUnavailableException(Name, $"{errorMessage}: Service unavailable");
            }

            if (httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return new ProviderException(Name, $"{errorMessage}: Bad request", "bad_request", ex);
            }

            if (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ProviderException(Name, $"{errorMessage}: Resource not found", "not_found", ex);
            }
        }

        if (ex is TaskCanceledException or OperationCanceledException)
        {
            return new ProviderException(Name, $"{errorMessage}: Request timed out", "timeout", ex);
        }

        // If the exception is already a ProviderException, just return it
        if (ex is ProviderException providerEx)
        {
            return providerEx;
        }

        // Default to a generic provider exception
        return new ProviderException(Name, $"{errorMessage}: {ex.Message}", ex);
    }
}
