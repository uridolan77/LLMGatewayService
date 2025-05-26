using System.Diagnostics;
using System.Runtime.CompilerServices;
using LLMGateway.Core.Constants;
using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.Provider;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Providers.Base;

/// <summary>
/// Enhanced base class for LLM providers with integrated Phase 1 and Phase 2 capabilities
/// </summary>
public abstract class BaseLLMProvider : ILLMProvider
{
    /// <summary>
    /// Logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Circuit breaker service for provider-specific failure handling
    /// </summary>
    protected readonly ICircuitBreakerService CircuitBreakerService;

    /// <summary>
    /// Token counting service with model-specific tokenizers
    /// </summary>
    protected readonly ITokenCountingService TokenCountingService;

    /// <summary>
    /// Enhanced caching service with provider-aware cache keys
    /// </summary>
    protected readonly IEnhancedCacheService CacheService;

    /// <summary>
    /// Content filtering service for prompt and completion filtering
    /// </summary>
    protected readonly IContentFilteringService ContentFilteringService;

    /// <summary>
    /// Metrics service for comprehensive metrics collection
    /// </summary>
    protected readonly IMetricsService MetricsService;

    /// <summary>
    /// Constructor with enhanced services
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="circuitBreakerService">Circuit breaker service</param>
    /// <param name="tokenCountingService">Token counting service</param>
    /// <param name="cacheService">Enhanced cache service</param>
    /// <param name="contentFilteringService">Content filtering service</param>
    /// <param name="metricsService">Metrics service</param>
    protected BaseLLMProvider(
        ILogger logger,
        ICircuitBreakerService circuitBreakerService,
        ITokenCountingService tokenCountingService,
        IEnhancedCacheService cacheService,
        IContentFilteringService contentFilteringService,
        IMetricsService metricsService)
    {
        Logger = logger;
        CircuitBreakerService = circuitBreakerService;
        TokenCountingService = tokenCountingService;
        CacheService = cacheService;
        ContentFilteringService = contentFilteringService;
        MetricsService = metricsService;
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
    /// Enhanced completion method with integrated services
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion response</returns>
    protected async Task<CompletionResponse> CreateEnhancedCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = new Activity($"{Name}.CreateCompletion").Start();
        activity?.SetTag("provider", Name);
        activity?.SetTag("model", request.ModelId);

        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        using (Logger.BeginScope(new Dictionary<string, object>
        {
            ["Provider"] = Name,
            ["ModelId"] = request.ModelId,
            ["RequestId"] = requestId,
            ["Stream"] = request.Stream
        }))
        {
            try
            {
                Logger.LogInformation("Creating completion for model {ModelId}", request.ModelId);

                // 1. Content filtering for prompt
                await FilterPromptAsync(request);

                // 2. Check cache first
                var cacheKey = GenerateCacheKey(request);
                var cachedResponse = await CacheService.GetAsync<CompletionResponse>(cacheKey);
                if (cachedResponse != null)
                {
                    Logger.LogInformation("Returning cached completion for request {RequestId}", requestId);
                    MetricsService.RecordCacheAccess("completion", true);
                    return cachedResponse;
                }

                MetricsService.RecordCacheAccess("completion", false);

                // 3. Estimate token usage
                var tokenEstimate = await TokenCountingService.EstimateTokensAsync(request);
                Logger.LogDebug("Estimated tokens: {PromptTokens} prompt, {CompletionTokens} completion",
                    tokenEstimate.PromptTokens, tokenEstimate.EstimatedCompletionTokens);

                // 4. Execute with circuit breaker
                var response = await CircuitBreakerService.ExecuteAsync(Name, async () =>
                {
                    return await CreateCompletionInternalAsync(request, cancellationToken);
                });

                // 5. Content filtering for completion
                await FilterCompletionAsync(response);

                // 6. Cache the response if appropriate
                if (ShouldCacheResponse(request))
                {
                    var cacheExpiration = TimeSpan.FromMinutes(GetCacheDurationMinutes(request));
                    await CacheService.SetWithSlidingExpirationAsync(cacheKey, response, cacheExpiration);
                    Logger.LogDebug("Cached completion response for {Duration} minutes", cacheExpiration.TotalMinutes);
                }

                // 7. Record metrics
                stopwatch.Stop();
                MetricsService.RecordCompletion(Name, request.ModelId, true, stopwatch.Elapsed.TotalMilliseconds, response.Usage?.TotalTokens ?? 0);

                Logger.LogInformation("Completion created successfully for request {RequestId} in {Duration}ms",
                    requestId, stopwatch.Elapsed.TotalMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                MetricsService.RecordCompletion(Name, request.ModelId, false, stopwatch.Elapsed.TotalMilliseconds);

                Logger.LogError(ex, "Failed to create completion for request {RequestId}", requestId);
                throw HandleProviderException(ex, "Failed to create completion");
            }
        }
    }

    /// <summary>
    /// Enhanced streaming completion method with integrated services
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of completion responses</returns>
    protected async IAsyncEnumerable<CompletionResponse> CreateEnhancedCompletionStreamAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = new Activity($"{Name}.CreateStreamingCompletion").Start();
        activity?.SetTag("provider", Name);
        activity?.SetTag("model", request.ModelId);
        activity?.SetTag("streaming", true);

        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        var chunkCount = 0;
        var totalTokens = 0;
        var success = false;

        using (Logger.BeginScope(new Dictionary<string, object>
        {
            ["Provider"] = Name,
            ["ModelId"] = request.ModelId,
            ["RequestId"] = requestId,
            ["Streaming"] = true
        }))
        {
            Logger.LogInformation("Creating streaming completion for model {ModelId}", request.ModelId);

            // Content filtering for prompt
            await FilterPromptAsync(request);

            // Estimate token usage
            var tokenEstimate = await TokenCountingService.EstimateTokensAsync(request);
            Logger.LogDebug("Estimated tokens: {PromptTokens} prompt, {CompletionTokens} completion",
                tokenEstimate.PromptTokens, tokenEstimate.EstimatedCompletionTokens);

            // Get the stream with circuit breaker protection
            IAsyncEnumerable<CompletionResponse>? responseStream = null;

            try
            {
                responseStream = await CircuitBreakerService.ExecuteAsync(Name, async () =>
                {
                    return CreateCompletionStreamInternalAsync(request, cancellationToken);
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create streaming completion for request {RequestId}", requestId);
                MetricsService.RecordCompletion(Name, request.ModelId, false, stopwatch.Elapsed.TotalMilliseconds, 0);
                throw HandleProviderException(ex, "Failed to create streaming completion");
            }

            // Stream the responses with filtering
            await foreach (var response in responseStream.WithCancellation(cancellationToken))
            {
                CompletionResponse? filteredResponse = null;

                try
                {
                    chunkCount++;

                    // Content filtering for each chunk
                    filteredResponse = await FilterStreamingCompletionAsync(response);

                    // Track token usage
                    if (response.Usage != null)
                    {
                        totalTokens = response.Usage.TotalTokens;
                    }

                    // Check for completion
                    if (response.Choices?.Any(c => !string.IsNullOrEmpty(c.FinishReason)) == true)
                    {
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error processing streaming chunk for request {RequestId}", requestId);
                    // Continue with next chunk instead of breaking the stream
                    continue;
                }

                if (filteredResponse != null)
                {
                    yield return filteredResponse;
                }

                if (success)
                {
                    break;
                }
            }

            // Record final metrics
            stopwatch.Stop();
            MetricsService.RecordCompletion(Name, request.ModelId, success, stopwatch.Elapsed.TotalMilliseconds, totalTokens);

            Logger.LogInformation("Streaming completion completed for request {RequestId} with {ChunkCount} chunks in {Duration}ms",
                requestId, chunkCount, stopwatch.Elapsed.TotalMilliseconds);
        }
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
            bufferedResponse = await CreateEnhancedCompletionAsync(request, cancellationToken);

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

    /// <summary>
    /// Abstract method for provider-specific completion implementation
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion response</returns>
    protected abstract Task<CompletionResponse> CreateCompletionInternalAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Abstract method for provider-specific streaming completion implementation
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of completion responses</returns>
    protected abstract IAsyncEnumerable<CompletionResponse> CreateCompletionStreamInternalAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filter prompt content before sending to provider
    /// </summary>
    /// <param name="request">Completion request</param>
    private async Task FilterPromptAsync(CompletionRequest request)
    {
        if (request.Messages?.Any() == true)
        {
            foreach (var message in request.Messages)
            {
                if (!string.IsNullOrEmpty(message.Content))
                {
                    var filterResult = await ContentFilteringService.FilterPromptAsync(message.Content);
                    if (!filterResult.IsAllowed)
                    {
                        throw new InvalidOperationException($"Prompt content filtered: {filterResult.Reason}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Filter completion content after receiving from provider
    /// </summary>
    /// <param name="response">Completion response</param>
    private async Task FilterCompletionAsync(CompletionResponse response)
    {
        if (response.Choices?.Any() == true)
        {
            foreach (var choice in response.Choices)
            {
                if (!string.IsNullOrEmpty(choice.Message?.Content))
                {
                    var filterResult = await ContentFilteringService.FilterCompletionAsync(choice.Message.Content);
                    if (!filterResult.IsAllowed)
                    {
                        Logger.LogWarning("Completion filtered: {Reason}", filterResult.Reason);
                        choice.Message.Content = "[Content filtered]";
                        choice.FinishReason = "content_filter";
                    }
                }
            }
        }
    }

    /// <summary>
    /// Filter streaming completion content
    /// </summary>
    /// <param name="response">Completion response chunk</param>
    /// <returns>Filtered response</returns>
    private async Task<CompletionResponse> FilterStreamingCompletionAsync(CompletionResponse response)
    {
        if (response.Choices?.Any() == true)
        {
            foreach (var choice in response.Choices)
            {
                if (!string.IsNullOrEmpty(choice.Delta?.Content))
                {
                    var filterResult = await ContentFilteringService.FilterCompletionAsync(choice.Delta.Content);
                    if (!filterResult.IsAllowed)
                    {
                        Logger.LogWarning("Streaming completion chunk filtered: {Reason}", filterResult.Reason);
                        choice.Delta.Content = "[Content filtered]";
                        choice.FinishReason = "content_filter";
                    }
                }
            }
        }

        return response;
    }

    /// <summary>
    /// Generate cache key for completion request
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>Cache key</returns>
    private string GenerateCacheKey(CompletionRequest request)
    {
        var keyData = new
        {
            Provider = Name,
            request.ModelId,
            Messages = request.Messages?.Select(m => new { m.Role, m.Content }).ToArray(),
            request.Temperature,
            request.MaxTokens,
            request.TopP,
            request.FrequencyPenalty,
            request.PresencePenalty,
            request.Stop
        };

        var json = System.Text.Json.JsonSerializer.Serialize(keyData);
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));
        return $"{LLMGatewayConstants.CacheKeys.CompletionPrefix}{Name.ToLowerInvariant()}:{Convert.ToHexString(hash)[..16]}";
    }

    /// <summary>
    /// Determine if response should be cached
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>True if should cache</returns>
    private bool ShouldCacheResponse(CompletionRequest request)
    {
        // Don't cache streaming responses or responses with high temperature (non-deterministic)
        return !request.Stream && (request.Temperature ?? 0.7) <= 0.3;
    }

    /// <summary>
    /// Get cache duration based on request parameters
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>Cache duration in minutes</returns>
    private int GetCacheDurationMinutes(CompletionRequest request)
    {
        // Cache duration based on temperature - lower temperature = longer cache
        var temperature = request.Temperature ?? 0.7;
        return temperature switch
        {
            <= 0.1 => 60,  // Very deterministic - cache for 1 hour
            <= 0.3 => 30,  // Somewhat deterministic - cache for 30 minutes
            _ => 5         // Default - cache for 5 minutes
        };
    }
}
