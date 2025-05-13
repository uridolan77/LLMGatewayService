// src/LLMGateway.Core/Services/EmbeddingService.cs (updated)
using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Models.Responses;
using LLMGateway.Core.Options;
using LLMGateway.Core.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace LLMGateway.Core.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IModelRouter _modelRouter;
    private readonly IOptions<GlobalOptions> _globalOptions;
    private readonly IOptions<RoutingOptions> _routingOptions;
    private readonly ILogger<EmbeddingService> _logger;
    
    public EmbeddingService(
        ILLMProviderFactory providerFactory,
        ITokenUsageService tokenUsageService,
        IModelRouter modelRouter,
        IOptions<GlobalOptions> globalOptions,
        IOptions<RoutingOptions> routingOptions,
        ILogger<EmbeddingService> logger)
    {
        _providerFactory = providerFactory;
        _tokenUsageService = tokenUsageService;
        _modelRouter = modelRouter;
        _globalOptions = globalOptions;
        _routingOptions = routingOptions;
        _logger = logger;
    }
    
    public async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, string userId)
    {
        ValidateRequest(request);
        
        var stopwatch = Stopwatch.StartNew();
        var originalModelId = request.Model;
        
        // Use model router to select the best model if smart routing is enabled
        if (_routingOptions.Value.EnableSmartRouting)
        {
            request.Model = await _modelRouter.SelectModelForEmbeddingAsync(request.Model, request, userId);
            
            if (request.Model != originalModelId)
            {
                _logger.LogInformation("Model router selected {SelectedModel} instead of {OriginalModel} for embedding",
                    request.Model, originalModelId);
            }
        }
        
        var provider = _providerFactory.GetProviderForModel(request.Model);
        
        if (provider == null)
        {
            throw new NotFoundException("model", request.Model);
        }
        
        try
        {
            var response = await provider.CreateEmbeddingAsync(request);
            stopwatch.Stop();
            
            // Track token usage
            if (_globalOptions.Value.TrackTokenUsage)
            {
                await _tokenUsageService.TrackTokenUsageAsync(new TokenUsageInfo
                {
                    UserId = userId,
                    ModelId = request.Model,
                    Provider = provider.ProviderName,
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = 0,
                    RequestType = "embedding",
                    Timestamp = DateTime.UtcNow
                });
            }
            
            // Track model metrics if enabled
            if (_routingOptions.Value.TrackModelMetrics)
            {
                await _modelRouter.RecordModelMetricsAsync(new ModelMetrics
                {
                    ModelId = request.Model,
                    Provider = provider.ProviderName,
                    RequestTokens = response.Usage.PromptTokens,
                    ResponseTokens = 0,
                    LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                    IsSuccess = true,
                    Cost = CalculateCost(request.Model, response.Usage.PromptTokens, 0),
                    Timestamp = DateTime.UtcNow
                });
            }
            
            return response;
        }
        catch (ProviderException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error from provider {ProviderName} when creating embedding: {ErrorMessage}",
                ex.ProviderName, ex.Message);
            
            // Track model error metrics if enabled
            if (_routingOptions.Value.TrackModelMetrics)
            {
                await _modelRouter.RecordModelMetricsAsync(new ModelMetrics
                {
                    ModelId = request.Model,
                    Provider = provider.ProviderName,
                    RequestTokens = EstimatePromptTokens(request, provider),
                    ResponseTokens = 0,
                    LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Cost = 0,
                    Timestamp = DateTime.UtcNow
                });
            }
            
            throw;
        }
    }
    
    private void ValidateRequest(EmbeddingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Model))
        {
            throw new ValidationException("The model parameter is required");
        }
        
        if (request.Input == null)
        {
            throw new ValidationException("The input parameter is required");
        }
        
        // Check if input is a string or array of strings
        bool isValidInput = false;
        
        if (request.Input is string)
        {
            isValidInput = true;
        }
        else if (request.Input is IEnumerable<string>)
        {
            isValidInput = true;
        }
        else if (request.Input is object[] array && array.All(item => item is string))
        {
            isValidInput = true;
        }
        
        if (!isValidInput)
        {
            throw new ValidationException("Input must be a string or an array of strings");
        }
    }
    
    private int EstimatePromptTokens(EmbeddingRequest request, ILLMProvider provider)
    {
        if (request.Input is string text)
        {
            return provider.CalculateTokenCount(text, request.Model);
        }
        else if (request.Input is IEnumerable<string> texts)
        {
            int totalTokens = 0;
            foreach (var item in texts)
            {
                totalTokens += provider.CalculateTokenCount(item, request.Model);
            }
            return totalTokens;
        }
        else if (request.Input is object[] array)
        {
            int totalTokens = 0;
            foreach (var item in array)
            {
                if (item is string stringItem)
                {
                    totalTokens += provider.CalculateTokenCount(stringItem, request.Model);
                }
            }
            return totalTokens;
        }
        
        return 0;
    }
    
    private double CalculateCost(string modelId, int promptTokens, int completionTokens)
    {
        // Get price per token from model mappings in LLMRouting
        var modelMapping = _globalOptions.Value.ModelMappings
            .FirstOrDefault(m => m.ModelId == modelId);
            
        if (modelMapping == null || 
            !modelMapping.Properties.TryGetValue("TokenPriceInput", out string? inputPrice))
        {
            return 0;
        }
        
        if (double.TryParse(inputPrice, out double inputPriceValue))
        {
            return (inputPriceValue * promptTokens) + (completionTokens > 0 && 
                   modelMapping.Properties.TryGetValue("TokenPriceOutput", out string? outputPrice) &&
                   double.TryParse(outputPrice, out double outputPriceValue) 
                        ? outputPriceValue * completionTokens 
                        : 0);
        }
        
        return 0;
    }
}
