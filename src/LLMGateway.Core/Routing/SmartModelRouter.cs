using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Core.Routing;

/// <summary>
/// Smart router for selecting the appropriate model for a request
/// </summary>
public class SmartModelRouter : IModelRouter
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IModelService _modelService;
    private readonly IContentBasedRouter _contentBasedRouter;
    private readonly ICostOptimizedRouter _costOptimizedRouter;
    private readonly ILatencyOptimizedRouter _latencyOptimizedRouter;
    private readonly ILogger<SmartModelRouter> _logger;
    private readonly LLMRoutingOptions _routingOptions;
    private readonly RoutingOptions _advancedRoutingOptions;
    private readonly UserPreferencesOptions _userPreferencesOptions;
    private readonly FallbackOptions _fallbackOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    public SmartModelRouter(
        ILLMProviderFactory providerFactory,
        IModelService modelService,
        IContentBasedRouter contentBasedRouter,
        ICostOptimizedRouter costOptimizedRouter,
        ILatencyOptimizedRouter latencyOptimizedRouter,
        IOptions<LLMRoutingOptions> routingOptions,
        IOptions<RoutingOptions> advancedRoutingOptions,
        IOptions<UserPreferencesOptions> userPreferencesOptions,
        IOptions<FallbackOptions> fallbackOptions,
        ILogger<SmartModelRouter> logger)
    {
        _providerFactory = providerFactory;
        _modelService = modelService;
        _contentBasedRouter = contentBasedRouter;
        _costOptimizedRouter = costOptimizedRouter;
        _latencyOptimizedRouter = latencyOptimizedRouter;
        _logger = logger;
        _routingOptions = routingOptions.Value;
        _advancedRoutingOptions = advancedRoutingOptions.Value;
        _userPreferencesOptions = userPreferencesOptions.Value;
        _fallbackOptions = fallbackOptions.Value;
    }

    /// <inheritdoc/>
    public async Task<RoutingResult> RouteCompletionRequestAsync(CompletionRequest request)
    {
        _logger.LogInformation("Routing completion request for model {ModelId}", request.ModelId);

        // Check if the model ID is a simple alias that needs to be mapped
        var modelId = MapModelAlias(request.ModelId);

        // Check if the user has a preferred model
        if (!string.IsNullOrEmpty(request.User))
        {
            var userPreference = _userPreferencesOptions.UserModelPreferences
                .FirstOrDefault(p => p.UserId == request.User);

            if (userPreference != null && !string.IsNullOrEmpty(userPreference.PreferredModelId))
            {
                _logger.LogInformation("Using user-preferred model {ModelId} for user {UserId}",
                    userPreference.PreferredModelId, request.User);
                modelId = userPreference.PreferredModelId;
            }
        }

        // Check if the model is in the routing options
        var modelMapping = _routingOptions.ModelMappings.FirstOrDefault(m => m.ModelId == modelId);
        if (modelMapping != null)
        {
            _logger.LogInformation("Found model mapping for {ModelId} to provider {Provider}",
                modelId, modelMapping.ProviderName);

            return new RoutingResult
            {
                Provider = modelMapping.ProviderName,
                ModelId = modelId,
                ProviderModelId = modelMapping.ProviderModelId,
                RoutingStrategy = "DirectMapping",
                Success = true
            };
        }

        // If smart routing is enabled, try to find the best model
        if (_advancedRoutingOptions.EnableSmartRouting)
        {
            var routingStrategy = DetermineRoutingStrategy(request);
            var result = await ApplyRoutingStrategy(modelId, routingStrategy, request);
            if (result.Success)
            {
                return result;
            }
        }

        // If we get here, we couldn't find a mapping or apply smart routing
        // Try to find a provider that supports this model directly
        try
        {
            var model = await _modelService.GetModelAsync(modelId);
            var provider = _providerFactory.GetProvider(model.Provider);

            _logger.LogInformation("Using provider {Provider} for model {ModelId}",
                model.Provider, modelId);

            return new RoutingResult
            {
                Provider = model.Provider,
                ModelId = modelId,
                ProviderModelId = model.ProviderModelId,
                RoutingStrategy = "DirectProvider",
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find provider for model {ModelId}", modelId);

            return new RoutingResult
            {
                Success = false,
                ErrorMessage = $"No provider found for model {modelId}"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<RoutingResult> RouteEmbeddingRequestAsync(EmbeddingRequest request)
    {
        _logger.LogInformation("Routing embedding request for model {ModelId}", request.ModelId);

        // Check if the model ID is a simple alias that needs to be mapped
        var modelId = MapModelAlias(request.ModelId);

        // Check if the model is in the routing options
        var modelMapping = _routingOptions.ModelMappings.FirstOrDefault(m => m.ModelId == modelId);
        if (modelMapping != null)
        {
            _logger.LogInformation("Found model mapping for {ModelId} to provider {Provider}",
                modelId, modelMapping.ProviderName);

            return new RoutingResult
            {
                Provider = modelMapping.ProviderName,
                ModelId = modelId,
                ProviderModelId = modelMapping.ProviderModelId,
                RoutingStrategy = "DirectMapping",
                Success = true
            };
        }

        // Try to find a provider that supports this model directly
        try
        {
            var model = await _modelService.GetModelAsync(modelId);

            // Check if the model supports embeddings
            if (!model.SupportsEmbeddings)
            {
                _logger.LogWarning("Model {ModelId} does not support embeddings", modelId);
                return new RoutingResult
                {
                    Success = false,
                    ErrorMessage = $"Model {modelId} does not support embeddings"
                };
            }

            var provider = _providerFactory.GetProvider(model.Provider);

            _logger.LogInformation("Using provider {Provider} for model {ModelId}",
                model.Provider, modelId);

            return new RoutingResult
            {
                Provider = model.Provider,
                ModelId = modelId,
                ProviderModelId = model.ProviderModelId,
                RoutingStrategy = "DirectProvider",
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find provider for model {ModelId}", modelId);

            return new RoutingResult
            {
                Success = false,
                ErrorMessage = $"No provider found for model {modelId}"
            };
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetFallbackModelsAsync(string modelId, string? errorCode = null)
    {
        if (!_fallbackOptions.EnableFallbacks)
        {
            return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
        }

        var rule = _fallbackOptions.Rules.FirstOrDefault(r => r.ModelId == modelId);
        if (rule == null)
        {
            _logger.LogWarning("No fallback rule found for model {ModelId}", modelId);
            return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
        }

        // If an error code is specified, check if it's in the rule's error codes
        if (!string.IsNullOrEmpty(errorCode) && rule.ErrorCodes.Any() && !rule.ErrorCodes.Contains(errorCode))
        {
            _logger.LogWarning("Error code {ErrorCode} not in fallback rule for model {ModelId}", errorCode, modelId);
            return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
        }

        _logger.LogInformation("Found fallback models for {ModelId}: {FallbackModels}",
            modelId, string.Join(", ", rule.FallbackModels));

        return Task.FromResult<IEnumerable<string>>(rule.FallbackModels);
    }

    private string MapModelAlias(string modelId)
    {
        // Check if the model ID is a simple alias that needs to be mapped
        var modelMapping = _advancedRoutingOptions.ModelMappings.FirstOrDefault(m => m.ModelId == modelId);
        if (modelMapping != null)
        {
            _logger.LogInformation("Mapping model alias {ModelId} to {TargetModelId}",
                modelId, modelMapping.TargetModelId);
            return modelMapping.TargetModelId;
        }

        return modelId;
    }

    private string DetermineRoutingStrategy(CompletionRequest request)
    {
        // Check if the user has a preferred routing strategy
        if (!string.IsNullOrEmpty(request.User))
        {
            var userPreference = _userPreferencesOptions.UserRoutingPreferences
                .FirstOrDefault(p => p.UserId == request.User);

            if (userPreference != null && !string.IsNullOrEmpty(userPreference.RoutingStrategy))
            {
                _logger.LogInformation("Using user-preferred routing strategy {Strategy} for user {UserId}",
                    userPreference.RoutingStrategy, request.User);
                return userPreference.RoutingStrategy;
            }
        }

        // Check if the model has a preferred routing strategy
        var modelStrategy = _advancedRoutingOptions.ModelRoutingStrategies
            .FirstOrDefault(s => s.ModelId == request.ModelId);

        if (modelStrategy != null && !string.IsNullOrEmpty(modelStrategy.Strategy))
        {
            _logger.LogInformation("Using model-preferred routing strategy {Strategy} for model {ModelId}",
                modelStrategy.Strategy, request.ModelId);
            return modelStrategy.Strategy;
        }

        // Default strategy based on request characteristics
        if (request.Temperature.HasValue && request.Temperature.Value < 0.3)
        {
            return "QualityOptimized";
        }

        if (request.MaxTokens.HasValue && request.MaxTokens.Value > 1000)
        {
            return "CostOptimized";
        }

        return "LoadBalanced";
    }

    private async Task<RoutingResult> ApplyRoutingStrategy(string modelId, string strategy, CompletionRequest request)
    {
        _logger.LogInformation("Applying routing strategy {Strategy} for model {ModelId}", strategy, modelId);

        // This is a simplified implementation - in a real system, this would be much more sophisticated
        switch (strategy)
        {
            case "CostOptimized":
                return await ApplyCostOptimizedStrategy(modelId);

            case "LatencyOptimized":
                return await ApplyLatencyOptimizedStrategy(modelId);

            case "QualityOptimized":
                return await ApplyQualityOptimizedStrategy(modelId);

            case "LoadBalanced":
                return await ApplyLoadBalancedStrategy(modelId);

            case "ContentBased":
                return await ApplyContentBasedStrategy(modelId, request);

            default:
                _logger.LogWarning("Unknown routing strategy {Strategy}, falling back to direct mapping", strategy);
                return new RoutingResult { Success = false };
        }
    }

    private async Task<RoutingResult> ApplyCostOptimizedStrategy(string modelId)
    {
        if (!_advancedRoutingOptions.EnableCostOptimizedRouting)
        {
            _logger.LogInformation("Cost-optimized routing is disabled");
            return new RoutingResult { Success = false };
        }

        // Use the dedicated cost-optimized router
        var request = new CompletionRequest { ModelId = modelId };
        return await _costOptimizedRouter.RouteRequestAsync(request);
    }

    private async Task<RoutingResult> ApplyLatencyOptimizedStrategy(string modelId)
    {
        if (!_advancedRoutingOptions.EnableLatencyOptimizedRouting)
        {
            _logger.LogInformation("Latency-optimized routing is disabled");
            return new RoutingResult { Success = false };
        }

        // Use the dedicated latency-optimized router
        var request = new CompletionRequest { ModelId = modelId };
        return await _latencyOptimizedRouter.RouteRequestAsync(request);
    }

    private Task<RoutingResult> ApplyQualityOptimizedStrategy(string modelId)
    {
        // In a real implementation, this would look at model quality metrics and select the best option
        // For now, we'll just return a simple mapping

        if (_advancedRoutingOptions.EnableQualityOptimizedRouting)
        {
            // For example, larger models typically produce higher quality outputs
            var mapping = _routingOptions.ModelMappings.FirstOrDefault(m =>
                m.ModelId == "openai.gpt-4-turbo" || m.ModelId == "anthropic.claude-3-opus");

            if (mapping != null)
            {
                return Task.FromResult(new RoutingResult
                {
                    Provider = mapping.ProviderName,
                    ModelId = mapping.ModelId,
                    ProviderModelId = mapping.ProviderModelId,
                    RoutingStrategy = "QualityOptimized",
                    Success = true
                });
            }
        }

        return Task.FromResult(new RoutingResult { Success = false });
    }

    private Task<RoutingResult> ApplyLoadBalancedStrategy(string modelId)
    {
        // In a real implementation, this would distribute traffic across multiple providers
        // For now, we'll just return a simple mapping based on a random selection

        if (_advancedRoutingOptions.EnableLoadBalancing)
        {
            var compatibleModels = _routingOptions.ModelMappings
                .Where(m => m.ContextWindow >= 8000) // Some basic filtering
                .ToList();

            if (compatibleModels.Any())
            {
                var random = new Random();
                var mapping = compatibleModels[random.Next(compatibleModels.Count)];

                return Task.FromResult(new RoutingResult
                {
                    Provider = mapping.ProviderName,
                    ModelId = mapping.ModelId,
                    ProviderModelId = mapping.ProviderModelId,
                    RoutingStrategy = "LoadBalanced",
                    Success = true
                });
            }
        }

        return Task.FromResult(new RoutingResult { Success = false });
    }

    private async Task<RoutingResult> ApplyContentBasedStrategy(string modelId, CompletionRequest request)
    {
        if (!_advancedRoutingOptions.EnableContentBasedRouting)
        {
            _logger.LogInformation("Content-based routing is disabled");
            return new RoutingResult { Success = false };
        }

        // Use the dedicated content-based router
        return await _contentBasedRouter.RouteRequestAsync(request);
    }
}
