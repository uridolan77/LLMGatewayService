using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.TokenUsage;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for creating completions
/// </summary>
public class CompletionService : ICompletionService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IModelRouter _modelRouter;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly ICacheService _cacheService;
    private readonly ICostManagementService _costManagementService;
    private readonly ILogger<CompletionService> _logger;
    private readonly GlobalOptions _globalOptions;
    private readonly FallbackOptions _fallbackOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    public CompletionService(
        ILLMProviderFactory providerFactory,
        IModelRouter modelRouter,
        ITokenUsageService tokenUsageService,
        ICacheService cacheService,
        ICostManagementService costManagementService,
        IOptions<GlobalOptions> globalOptions,
        IOptions<FallbackOptions> fallbackOptions,
        ILogger<CompletionService> logger)
    {
        _providerFactory = providerFactory;
        _modelRouter = modelRouter;
        _tokenUsageService = tokenUsageService;
        _cacheService = cacheService;
        _costManagementService = costManagementService;
        _logger = logger;
        _globalOptions = globalOptions.Value;
        _fallbackOptions = fallbackOptions.Value;
    }

    /// <inheritdoc/>
    public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating completion for model {ModelId}", request.ModelId);

        // Check if the request is cacheable and if it's in the cache
        if (_globalOptions.EnableCaching && !request.Stream && IsCacheable(request))
        {
            var cacheKey = GenerateCacheKey(request);
            var cachedResponse = await _cacheService.GetAsync<CompletionResponse>(cacheKey);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for completion request with model {ModelId}", request.ModelId);
                return cachedResponse;
            }
        }

        // Route the request to the appropriate provider
        var routingResult = await _modelRouter.RouteCompletionRequestAsync(request);
        if (!routingResult.Success)
        {
            throw new RoutingException(routingResult.ErrorMessage ?? "Failed to route completion request");
        }

        // Get the provider
        var provider = _providerFactory.GetProvider(routingResult.Provider);

        // Update the model ID in the request
        var originalModelId = request.ModelId;
        request.ModelId = routingResult.ProviderModelId;

        CompletionResponse response;
        int attemptCount = 0;
        Exception? lastException = null;

        do
        {
            attemptCount++;
            try
            {
                // Create the completion
                response = await provider.CreateCompletionAsync(request, cancellationToken);

                // Set the provider and model in the response
                response.Provider = routingResult.Provider;
                response.Model = originalModelId;

                // Track token usage
                if (_globalOptions.TrackTokenUsage)
                {
                    await TrackTokenUsageAsync(request, response);
                }

                // Cache the response if caching is enabled
                if (_globalOptions.EnableCaching && !request.Stream && IsCacheable(request))
                {
                    var cacheKey = GenerateCacheKey(request);
                    await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(_globalOptions.CacheExpirationMinutes));
                }

                return response;
            }
            catch (ProviderException ex) when (_fallbackOptions.EnableFallbacks && attemptCount <= _fallbackOptions.MaxFallbackAttempts)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Provider {Provider} failed to create completion for model {ModelId}. Attempting fallback.", routingResult.Provider, routingResult.ModelId);

                // Get fallback models
                var fallbackModels = await _modelRouter.GetFallbackModelsAsync(originalModelId, ex.ErrorCode);
                if (!fallbackModels.Any())
                {
                    _logger.LogWarning("No fallback models available for {ModelId}", originalModelId);
                    throw new FallbackExhaustedException($"No fallback models available for {originalModelId}", ex);
                }

                // Try the next fallback model
                var fallbackModelId = fallbackModels.ElementAt(Math.Min(attemptCount - 1, fallbackModels.Count() - 1));
                request.ModelId = fallbackModelId;

                // Route the request to the fallback model
                routingResult = await _modelRouter.RouteCompletionRequestAsync(request);
                if (!routingResult.Success)
                {
                    throw new RoutingException(routingResult.ErrorMessage ?? "Failed to route completion request to fallback model", ex);
                }

                // Get the provider for the fallback model
                provider = _providerFactory.GetProvider(routingResult.Provider);
                request.ModelId = routingResult.ProviderModelId;

                _logger.LogInformation("Falling back to model {ModelId} with provider {Provider}", fallbackModelId, routingResult.Provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create completion for model {ModelId} with provider {Provider}", request.ModelId, routingResult.Provider);
                lastException = ex;
                throw;
            }
        } while (attemptCount <= _fallbackOptions.MaxFallbackAttempts);

        // If we get here, all fallback attempts failed
        throw new FallbackExhaustedException($"All fallback attempts failed for model {originalModelId}", lastException);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<CompletionResponse> CreateCompletionStreamAsync(CompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating streaming completion for model {ModelId}", request.ModelId);

        // Route the request to the appropriate provider
        var routingResult = await _modelRouter.RouteCompletionRequestAsync(request);
        if (!routingResult.Success)
        {
            throw new RoutingException(routingResult.ErrorMessage ?? "Failed to route completion request");
        }

        // Update the model ID in the request
        var originalModelId = request.ModelId;
        request.ModelId = routingResult.ProviderModelId;

        // Ensure streaming is enabled
        request.Stream = true;

        // Create the provider
        var provider = _providerFactory.GetProvider(routingResult.Provider);

        // This approach avoids yield in try/catch by keeping all yields outside the try block
        IAsyncEnumerable<CompletionResponse>? responseStream = null;

        // Attempt to create the stream without yielding yet
        try
        {
            responseStream = provider.CreateCompletionStreamAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create streaming completion for model {ModelId} with provider {Provider}",
                request.ModelId, routingResult.Provider);
            throw;
        }

        // Now that we have a stream successfully created, we can yield from it
        await foreach (var response in responseStream.WithCancellation(cancellationToken))
        {
            // Set the provider and model in the response
            response.Provider = routingResult.Provider;
            response.Model = originalModelId;

            // Track token usage for the first chunk only
            if (_globalOptions.TrackTokenUsage && response.Choices.Any(c => c.Delta == null || c.Delta.Content == null))
            {
                await TrackTokenUsageAsync(request, response);
            }

            yield return response;
        }
    }

    private async Task TrackTokenUsageAsync(CompletionRequest request, CompletionResponse response)
    {
        try
        {
            // Track token usage
            var record = new TokenUsageRecord
            {
                RequestId = response.Id,
                ModelId = response.Model,
                Provider = response.Provider,
                RequestType = "completion",
                PromptTokens = response.Usage.PromptTokens,
                CompletionTokens = response.Usage.CompletionTokens,
                TotalTokens = response.Usage.TotalTokens,
                UserId = request.User ?? "anonymous",
                ApiKeyId = "unknown" // This would be set by middleware
            };

            await _tokenUsageService.TrackUsageAsync(record);

            // Track cost
            if (_globalOptions.EnableCostTracking)
            {
                try
                {
                    await _costManagementService.TrackCompletionCostAsync(
                        request,
                        response,
                        request.User ?? "anonymous",
                        response.Id,
                        request.ProjectId,
                        request.Tags);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to track cost for completion");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track token usage for completion");
        }
    }

    private bool IsCacheable(CompletionRequest request)
    {
        // Determine if a request is cacheable
        // For example, requests with low temperature are more cacheable
        return request.Temperature.GetValueOrDefault(0.7) < 0.1;
    }

    private string GenerateCacheKey(CompletionRequest request)
    {
        // Generate a cache key for the request
        // This should include all relevant request parameters
        return $"completion:{request.ModelId}:{string.Join(",", request.Messages.Select(m => $"{m.Role}:{m.Content}"))}";
    }
}
