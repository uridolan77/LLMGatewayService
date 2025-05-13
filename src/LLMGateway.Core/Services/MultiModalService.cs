using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Provider;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for multi-modal operations
/// </summary>
public class MultiModalService : IMultiModalService
{
    private readonly IModelService _modelService;
    private readonly IProviderService _providerService;
    private readonly ILogger<MultiModalService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="modelService">Model service</param>
    /// <param name="providerService">Provider service</param>
    /// <param name="logger">Logger</param>
    public MultiModalService(
        IModelService modelService,
        IProviderService providerService,
        ILogger<MultiModalService> logger)
    {
        _modelService = modelService;
        _providerService = providerService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CompletionResponse> CreateMultiModalCompletionAsync(MultiModalCompletionRequest request)
    {
        try
        {
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.ModelId))
            {
                throw new ValidationException("Model ID is required");
            }

            if (request.Messages == null || !request.Messages.Any())
            {
                throw new ValidationException("At least one message is required");
            }

            // Get the model
            var model = await _modelService.GetModelAsync(request.ModelId);
            if (model == null)
            {
                throw new ModelNotFoundException(request.ModelId);
            }

            // Check if the model supports multi-modal inputs
            if (!model.SupportsVision)
            {
                throw new ValidationException($"Model {request.ModelId} does not support multi-modal inputs");
            }

            // Get the provider
            var provider = _providerService.GetProvider(model.Provider);
            if (provider == null)
            {
                throw new ProviderNotFoundException(model.Provider);
            }

            // Check if the provider supports multi-modal inputs
            if (!provider.SupportsMultiModal)
            {
                throw new ValidationException($"Provider {model.Provider} does not support multi-modal inputs");
            }

            // Create the completion
            var response = await provider.CreateMultiModalCompletionAsync(request);

            _logger.LogInformation("Created multi-modal completion with model {ModelId}", request.ModelId);

            return response;
        }
        catch (Exception ex) when (ex is not ValidationException && ex is not ModelNotFoundException && ex is not ProviderNotFoundException)
        {
            _logger.LogError(ex, "Failed to create multi-modal completion with model {ModelId}", request.ModelId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<CompletionChunk> CreateStreamingMultiModalCompletionAsync(MultiModalCompletionRequest request)
    {
        // Validate the request
        if (string.IsNullOrWhiteSpace(request.ModelId))
        {
            throw new ValidationException("Model ID is required");
        }

        if (request.Messages == null || !request.Messages.Any())
        {
            throw new ValidationException("At least one message is required");
        }

        // Get the model
        ModelInfo? model;
        try
        {
            model = await _modelService.GetModelAsync(request.ModelId);
            if (model == null)
            {
                throw new ModelNotFoundException(request.ModelId);
            }
        }
        catch (Exception ex) when (ex is not ModelNotFoundException)
        {
            _logger.LogError(ex, "Failed to get model {ModelId}", request.ModelId);
            throw;
        }

        // Check if the model supports multi-modal inputs
        if (!model.SupportsVision)
        {
            throw new ValidationException($"Model {request.ModelId} does not support multi-modal inputs");
        }

        // Get the provider
        ILLMProvider? provider;
        try
        {
            provider = _providerService.GetProvider(model.Provider);
            if (provider == null)
            {
                throw new ProviderNotFoundException(model.Provider);
            }
        }
        catch (Exception ex) when (ex is not ProviderNotFoundException)
        {
            _logger.LogError(ex, "Failed to get provider {Provider}", model.Provider);
            throw;
        }

        // Check if the provider supports multi-modal inputs
        if (!provider.SupportsMultiModal)
        {
            throw new ValidationException($"Provider {model.Provider} does not support multi-modal inputs");
        }

        // Check if the provider supports streaming
        if (!provider.SupportsStreaming)
        {
            throw new ValidationException($"Provider {model.Provider} does not support streaming");
        }

        // Force streaming to be true
        request.Stream = true;

        // Create the streaming completion
        IAsyncEnumerable<CompletionChunk>? stream = null;
        try
        {
            stream = provider.CreateStreamingMultiModalCompletionAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create streaming multi-modal completion with model {ModelId}", request.ModelId);
            throw;
        }

        // Now yield the results outside of any try/catch
        await foreach (var chunk in stream)
        {
            yield return chunk;
        }

        _logger.LogInformation("Created streaming multi-modal completion with model {ModelId}", request.ModelId);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Model>> GetMultiModalModelsAsync()
    {
        try
        {
            var modelInfos = await _modelService.GetModelsAsync();
            return modelInfos.Where(m => m.SupportsVision)
                .Select(m => new Model
                {
                    Id = m.Id,
                    DisplayName = m.DisplayName,
                    Provider = m.Provider,
                    ProviderModelId = m.ProviderModelId,
                    ContextWindow = m.ContextWindow,
                    SupportsCompletions = m.SupportsCompletions,
                    SupportsEmbeddings = m.SupportsEmbeddings,
                    SupportsStreaming = m.SupportsStreaming,
                    SupportsFunctionCalling = m.SupportsFunctionCalling,
                    SupportsVision = m.SupportsVision,
                    CostPer1kPromptTokensUsd = m.InputPricePerToken,
                    CostPer1kCompletionTokensUsd = m.OutputPricePerToken
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get multi-modal models");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SupportsMultiModalAsync(string modelId)
    {
        try
        {
            var model = await _modelService.GetModelAsync(modelId);
            return model?.SupportsVision ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if model {ModelId} supports multi-modal inputs", modelId);
            throw;
        }
    }
}
