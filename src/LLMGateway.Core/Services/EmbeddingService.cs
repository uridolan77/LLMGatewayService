using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.TokenUsage;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for creating embeddings
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IModelRouter _modelRouter;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly ICacheService _cacheService;
    private readonly ICostManagementService _costManagementService;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly GlobalOptions _globalOptions;
    private readonly FallbackOptions _fallbackOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    public EmbeddingService(
        ILLMProviderFactory providerFactory,
        IModelRouter modelRouter,
        ITokenUsageService tokenUsageService,
        ICacheService cacheService,
        ICostManagementService costManagementService,
        IOptions<GlobalOptions> globalOptions,
        IOptions<FallbackOptions> fallbackOptions,
        ILogger<EmbeddingService> logger)
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
    public async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating embedding for model {ModelId}", request.ModelId);

        // Check if the request is in the cache
        if (_globalOptions.EnableCaching)
        {
            var cacheKey = GenerateCacheKey(request);
            var cachedResponse = await _cacheService.GetAsync<EmbeddingResponse>(cacheKey);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for embedding request with model {ModelId}", request.ModelId);
                return cachedResponse;
            }
        }

        // Route the request to the appropriate provider
        var routingResult = await _modelRouter.RouteEmbeddingRequestAsync(request);
        if (!routingResult.Success)
        {
            throw new RoutingException(routingResult.ErrorMessage ?? "Failed to route embedding request");
        }

        // Get the provider
        var provider = _providerFactory.GetProvider(routingResult.Provider);

        // Update the model ID in the request
        var originalModelId = request.ModelId;
        request.ModelId = routingResult.ProviderModelId;

        EmbeddingResponse response;
        int attemptCount = 0;
        Exception? lastException = null;

        do
        {
            attemptCount++;
            try
            {
                // Create the embedding
                response = await provider.CreateEmbeddingAsync(request, cancellationToken);

                // Set the provider and model in the response
                response.Provider = routingResult.Provider;
                response.Model = originalModelId;

                // Track token usage
                if (_globalOptions.TrackTokenUsage)
                {
                    await TrackTokenUsageAsync(request, response);
                }

                // Cache the response if caching is enabled
                if (_globalOptions.EnableCaching)
                {
                    var cacheKey = GenerateCacheKey(request);
                    await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(_globalOptions.CacheExpirationMinutes));
                }

                return response;
            }
            catch (ProviderException ex) when (_fallbackOptions.EnableFallbacks && attemptCount <= _fallbackOptions.MaxFallbackAttempts)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Provider {Provider} failed to create embedding for model {ModelId}. Attempting fallback.", routingResult.Provider, routingResult.ModelId);

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
                routingResult = await _modelRouter.RouteEmbeddingRequestAsync(request);
                if (!routingResult.Success)
                {
                    throw new RoutingException(routingResult.ErrorMessage ?? "Failed to route embedding request to fallback model", ex);
                }

                // Get the provider for the fallback model
                provider = _providerFactory.GetProvider(routingResult.Provider);
                request.ModelId = routingResult.ProviderModelId;

                _logger.LogInformation("Falling back to model {ModelId} with provider {Provider}", fallbackModelId, routingResult.Provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create embedding for model {ModelId} with provider {Provider}", request.ModelId, routingResult.Provider);
                lastException = ex;
                throw;
            }
        } while (attemptCount <= _fallbackOptions.MaxFallbackAttempts);

        // If we get here, all fallback attempts failed
        throw new FallbackExhaustedException($"All fallback attempts failed for model {originalModelId}", lastException);
    }

    private async Task TrackTokenUsageAsync(EmbeddingRequest request, EmbeddingResponse response)
    {
        try
        {
            // Generate a request ID if the response doesn't have one
            var requestId = Guid.NewGuid().ToString();

            // Track token usage
            var record = new TokenUsageRecord
            {
                RequestId = requestId,
                ModelId = response.Model,
                Provider = response.Provider,
                RequestType = "embedding",
                PromptTokens = response.Usage.PromptTokens,
                CompletionTokens = 0, // Embeddings don't have completion tokens
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
                    await _costManagementService.TrackEmbeddingCostAsync(
                        request,
                        response,
                        request.User ?? "anonymous",
                        requestId,
                        request.ProjectId,
                        request.Tags);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to track cost for embedding");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track token usage for embedding");
        }
    }

    private string GenerateCacheKey(EmbeddingRequest request)
    {
        // Generate a cache key for the request
        // This should include all relevant request parameters
        string inputString;

        if (request.Input is string stringInput)
        {
            inputString = stringInput;
        }
        else if (request.Input is IEnumerable<string> stringArrayInput)
        {
            inputString = string.Join("|", stringArrayInput);
        }
        else
        {
            inputString = JsonSerializer.Serialize(request.Input);
        }

        // Use a hash of the input to avoid very long cache keys
        using var sha256 = SHA256.Create();
        var inputBytes = Encoding.UTF8.GetBytes(inputString);
        var hashBytes = sha256.ComputeHash(inputBytes);
        var inputHash = Convert.ToBase64String(hashBytes);

        return $"embedding:{request.ModelId}:{inputHash}:{request.Dimensions ?? 0}";
    }
}
