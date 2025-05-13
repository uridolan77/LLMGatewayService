using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Core.Routing;

/// <summary>
/// Cost-optimized router for selecting the most cost-effective model
/// </summary>
public class CostOptimizedRouter : ICostOptimizedRouter
{
    private readonly IModelService _modelService;
    private readonly ILogger<CostOptimizedRouter> _logger;
    private readonly RoutingOptions _routingOptions;
    
    // Cost per 1K tokens for various models (approximate values)
    private readonly Dictionary<string, (decimal InputCost, decimal OutputCost)> _modelCosts = new()
    {
        // OpenAI models
        { "openai.gpt-4", (0.03m, 0.06m) },
        { "openai.gpt-4-turbo", (0.01m, 0.03m) },
        { "openai.gpt-3.5-turbo", (0.0015m, 0.002m) },
        { "openai.text-embedding-ada-002", (0.0001m, 0.0m) },
        
        // Anthropic models
        { "anthropic.claude-3-opus", (0.015m, 0.075m) },
        { "anthropic.claude-3-sonnet", (0.003m, 0.015m) },
        { "anthropic.claude-3-haiku", (0.00025m, 0.00125m) },
        { "anthropic.claude-2.1", (0.008m, 0.024m) },
        { "anthropic.claude-2.0", (0.008m, 0.024m) },
        { "anthropic.claude-instant-1.2", (0.0008m, 0.0024m) },
        
        // Cohere models
        { "cohere.command-r", (0.0015m, 0.0015m) },
        { "cohere.command-r-plus", (0.003m, 0.003m) },
        { "cohere.command-light", (0.0003m, 0.0003m) },
        { "cohere.embed-english-v3.0", (0.0001m, 0.0m) },
        { "cohere.embed-multilingual-v3.0", (0.0001m, 0.0m) },
        
        // HuggingFace models (assuming free or very low cost)
        { "huggingface.mistralai_Mistral-7B-Instruct-v0.2", (0.0001m, 0.0001m) },
        { "huggingface.meta-llama_Llama-2-7b-chat-hf", (0.0001m, 0.0001m) },
        { "huggingface.meta-llama_Llama-2-13b-chat-hf", (0.0001m, 0.0001m) },
        { "huggingface.meta-llama_Llama-2-70b-chat-hf", (0.0001m, 0.0001m) }
    };

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="modelService">Model service</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Routing options</param>
    public CostOptimizedRouter(
        IModelService modelService,
        ILogger<CostOptimizedRouter> logger,
        IOptions<RoutingOptions> options)
    {
        _modelService = modelService;
        _logger = logger;
        _routingOptions = options.Value;
    }

    /// <inheritdoc/>
    public async Task<RoutingResult> RouteRequestAsync(CompletionRequest request)
    {
        _logger.LogInformation("Routing request based on cost optimization");
        
        if (!_routingOptions.EnableCostOptimizedRouting)
        {
            _logger.LogInformation("Cost-optimized routing is disabled");
            return new RoutingResult { Success = false };
        }
        
        // Get all available models
        var allModels = await _modelService.GetModelsAsync();
        
        // Filter models that support completions
        var completionModels = allModels.Where(m => m.SupportsCompletions).ToList();
        
        // Estimate the input token count
        var estimatedInputTokens = EstimateInputTokens(request);
        _logger.LogInformation("Estimated input tokens: {EstimatedInputTokens}", estimatedInputTokens);
        
        // Estimate the output token count
        var estimatedOutputTokens = request.MaxTokens ?? 1000;
        _logger.LogInformation("Estimated output tokens: {EstimatedOutputTokens}", estimatedOutputTokens);
        
        // Calculate the estimated cost for each model
        var modelCosts = new Dictionary<string, decimal>();
        
        foreach (var model in completionModels)
        {
            if (_modelCosts.TryGetValue(model.Id, out var costs))
            {
                var inputCost = costs.InputCost * (estimatedInputTokens / 1000m);
                var outputCost = costs.OutputCost * (estimatedOutputTokens / 1000m);
                var totalCost = inputCost + outputCost;
                
                modelCosts[model.Id] = totalCost;
                
                _logger.LogDebug("Estimated cost for model {ModelId}: {TotalCost:C}", model.Id, totalCost);
            }
        }
        
        // Find the model with the lowest cost
        if (modelCosts.Count == 0)
        {
            _logger.LogInformation("No cost information available for any model");
            return new RoutingResult { Success = false };
        }
        
        var lowestCostModelId = modelCosts.OrderBy(kv => kv.Value).First().Key;
        var lowestCostModel = completionModels.First(m => m.Id == lowestCostModelId);
        var lowestCost = modelCosts[lowestCostModelId];
        
        _logger.LogInformation("Selected lowest cost model {ModelId} with estimated cost {Cost:C}", 
            lowestCostModel.Id, lowestCost);
        
        return new RoutingResult
        {
            Provider = lowestCostModel.Provider,
            ModelId = lowestCostModel.Id,
            ProviderModelId = lowestCostModel.ProviderModelId,
            RoutingStrategy = "CostOptimized",
            RoutingReason = $"Lowest cost model (estimated: {lowestCost:C})",
            Success = true
        };
    }

    private int EstimateInputTokens(CompletionRequest request)
    {
        // A very simple token estimator (4 characters ≈ 1 token)
        var totalCharacters = 0;
        
        foreach (var message in request.Messages)
        {
            totalCharacters += message.Content?.Length ?? 0;
            
            // Add extra tokens for role prefixes
            totalCharacters += 10;
        }
        
        return totalCharacters / 4;
    }
}
