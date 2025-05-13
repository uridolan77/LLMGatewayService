using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Core.Models.Routing;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for retrieving model information
/// </summary>
public class ModelService : IModelService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ModelService> _logger;
    private readonly GlobalOptions _globalOptions;
    private readonly LLMRoutingOptions _routingOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    public ModelService(
        ILLMProviderFactory providerFactory,
        ICacheService cacheService,
        IOptions<GlobalOptions> globalOptions,
        IOptions<LLMRoutingOptions> routingOptions,
        ILogger<ModelService> logger)
    {
        _providerFactory = providerFactory;
        _cacheService = cacheService;
        _logger = logger;
        _globalOptions = globalOptions.Value;
        _routingOptions = routingOptions.Value;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ModelInfo>> GetModelsAsync()
    {
        _logger.LogInformation("Getting all models");

        // Check if the models are in the cache
        if (_globalOptions.EnableCaching)
        {
            var cachedModels = await _cacheService.GetAsync<List<ModelInfo>>("models:all");
            if (cachedModels != null && cachedModels.Count > 0)
            {
                _logger.LogInformation("Cache hit for all models");
                return cachedModels;
            }
        }

        var models = new List<ModelInfo>();

        // Add models from the routing options
        foreach (var mapping in _routingOptions.ModelMappings)
        {
            var model = new ModelInfo
            {
                Id = mapping.ModelId,
                DisplayName = mapping.DisplayName,
                Provider = mapping.ProviderName,
                ProviderModelId = mapping.ProviderModelId,
                ContextWindow = mapping.ContextWindow,
                Properties = mapping.Properties
            };

            models.Add(model);
        }

        // If provider discovery is enabled, get models from providers
        if (_globalOptions.EnableProviderDiscovery)
        {
            try
            {
                var providers = _providerFactory.GetAllProviders();
                foreach (var provider in providers)
                {
                    try
                    {
                        var providerModels = await provider.GetModelsAsync();
                        
                        // Only add models that aren't already in the list
                        foreach (var model in providerModels)
                        {
                            if (!models.Any(m => m.Id == model.Id))
                            {
                                models.Add(model);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get models from provider {Provider}", provider.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get providers for model discovery");
            }
        }

        // Cache the models
        if (_globalOptions.EnableCaching)
        {
            await _cacheService.SetAsync("models:all", models, TimeSpan.FromMinutes(_globalOptions.CacheExpirationMinutes));
        }

        return models;
    }

    /// <inheritdoc/>
    public async Task<ModelInfo> GetModelAsync(string modelId)
    {
        _logger.LogInformation("Getting model {ModelId}", modelId);

        // Check if the model is in the cache
        if (_globalOptions.EnableCaching)
        {
            var cachedModel = await _cacheService.GetAsync<ModelInfo>($"models:{modelId}");
            if (cachedModel != null)
            {
                _logger.LogInformation("Cache hit for model {ModelId}", modelId);
                return cachedModel;
            }
        }

        // Check if the model is in the routing options
        var modelMapping = _routingOptions.ModelMappings.FirstOrDefault(m => m.ModelId == modelId);
        if (modelMapping != null)
        {
            var model = new ModelInfo
            {
                Id = modelMapping.ModelId,
                DisplayName = modelMapping.DisplayName,
                Provider = modelMapping.ProviderName,
                ProviderModelId = modelMapping.ProviderModelId,
                ContextWindow = modelMapping.ContextWindow,
                Properties = modelMapping.Properties
            };

            // Cache the model
            if (_globalOptions.EnableCaching)
            {
                await _cacheService.SetAsync($"models:{modelId}", model, TimeSpan.FromMinutes(_globalOptions.CacheExpirationMinutes));
            }

            return model;
        }

        // If provider discovery is enabled, try to get the model from the provider
        if (_globalOptions.EnableProviderDiscovery)
        {
            try
            {
                var providers = _providerFactory.GetAllProviders();
                foreach (var provider in providers)
                {
                    try
                    {
                        var model = await provider.GetModelAsync(modelId);
                        if (model != null)
                        {
                            // Cache the model
                            if (_globalOptions.EnableCaching)
                            {
                                await _cacheService.SetAsync($"models:{modelId}", model, TimeSpan.FromMinutes(_globalOptions.CacheExpirationMinutes));
                            }

                            return model;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Provider {Provider} does not have model {ModelId}", provider.Name, modelId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get providers for model discovery");
            }
        }

        throw new ModelNotFoundException(modelId);
    }
}
