using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Core.Routing;

/// <summary>
/// Latency-optimized router for selecting the model with the lowest latency
/// </summary>
public class LatencyOptimizedRouter : ILatencyOptimizedRouter
{
    private readonly IModelService _modelService;
    private readonly IModelPerformanceMonitor _performanceMonitor;
    private readonly ILogger<LatencyOptimizedRouter> _logger;
    private readonly RoutingOptions _routingOptions;
    
    // Default latency estimates for models (in milliseconds)
    private readonly Dictionary<string, double> _defaultLatencies = new()
    {
        // OpenAI models
        { "openai.gpt-4", 2000 },
        { "openai.gpt-4-turbo", 1500 },
        { "openai.gpt-3.5-turbo", 800 },
        { "openai.text-embedding-ada-002", 200 },
        
        // Anthropic models
        { "anthropic.claude-3-opus", 3000 },
        { "anthropic.claude-3-sonnet", 1500 },
        { "anthropic.claude-3-haiku", 500 },
        { "anthropic.claude-2.1", 2000 },
        { "anthropic.claude-2.0", 2000 },
        { "anthropic.claude-instant-1.2", 800 },
        
        // Cohere models
        { "cohere.command-r", 1000 },
        { "cohere.command-r-plus", 1500 },
        { "cohere.command-light", 500 },
        { "cohere.embed-english-v3.0", 300 },
        { "cohere.embed-multilingual-v3.0", 300 },
        
        // HuggingFace models
        { "huggingface.mistralai_Mistral-7B-Instruct-v0.2", 3000 },
        { "huggingface.meta-llama_Llama-2-7b-chat-hf", 3000 },
        { "huggingface.meta-llama_Llama-2-13b-chat-hf", 4000 },
        { "huggingface.meta-llama_Llama-2-70b-chat-hf", 6000 }
    };

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="modelService">Model service</param>
    /// <param name="performanceMonitor">Model performance monitor</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Routing options</param>
    public LatencyOptimizedRouter(
        IModelService modelService,
        IModelPerformanceMonitor performanceMonitor,
        ILogger<LatencyOptimizedRouter> logger,
        IOptions<RoutingOptions> options)
    {
        _modelService = modelService;
        _performanceMonitor = performanceMonitor;
        _logger = logger;
        _routingOptions = options.Value;
    }

    /// <inheritdoc/>
    public async Task<RoutingResult> RouteRequestAsync(CompletionRequest request)
    {
        _logger.LogInformation("Routing request based on latency optimization");
        
        if (!_routingOptions.EnableLatencyOptimizedRouting)
        {
            _logger.LogInformation("Latency-optimized routing is disabled");
            return new RoutingResult { Success = false };
        }
        
        // Get all available models
        var allModels = await _modelService.GetModelsAsync();
        
        // Filter models that support completions
        var completionModels = allModels.Where(m => m.SupportsCompletions).ToList();
        
        // Get performance metrics for all models
        var performanceMetrics = _performanceMonitor.GetAllModelPerformanceMetrics();
        
        // Calculate the estimated latency for each model
        var modelLatencies = new Dictionary<string, double>();
        
        foreach (var model in completionModels)
        {
            double latency;
            
            // Use actual performance metrics if available
            if (performanceMetrics.TryGetValue(model.Id, out var metrics) && 
                metrics.RequestCount > 10) // Only use metrics if we have enough data
            {
                latency = metrics.AverageResponseTimeMs;
                _logger.LogDebug("Using actual latency for model {ModelId}: {Latency}ms", model.Id, latency);
            }
            else if (_defaultLatencies.TryGetValue(model.Id, out var defaultLatency))
            {
                latency = defaultLatency;
                _logger.LogDebug("Using default latency for model {ModelId}: {Latency}ms", model.Id, latency);
            }
            else
            {
                // Use a high default value for unknown models
                latency = 5000;
                _logger.LogDebug("Using fallback latency for model {ModelId}: {Latency}ms", model.Id, latency);
            }
            
            // Adjust latency based on token count
            var estimatedTokens = EstimateInputTokens(request);
            var tokenFactor = Math.Max(1.0, estimatedTokens / 1000.0);
            var adjustedLatency = latency * tokenFactor;
            
            modelLatencies[model.Id] = adjustedLatency;
            
            _logger.LogDebug("Estimated latency for model {ModelId}: {Latency}ms (adjusted for {Tokens} tokens)", 
                model.Id, adjustedLatency, estimatedTokens);
        }
        
        // Find the model with the lowest latency
        if (modelLatencies.Count == 0)
        {
            _logger.LogInformation("No latency information available for any model");
            return new RoutingResult { Success = false };
        }
        
        var lowestLatencyModelId = modelLatencies.OrderBy(kv => kv.Value).First().Key;
        var lowestLatencyModel = completionModels.First(m => m.Id == lowestLatencyModelId);
        var lowestLatency = modelLatencies[lowestLatencyModelId];
        
        _logger.LogInformation("Selected lowest latency model {ModelId} with estimated latency {Latency}ms", 
            lowestLatencyModel.Id, lowestLatency);
        
        return new RoutingResult
        {
            Provider = lowestLatencyModel.Provider,
            ModelId = lowestLatencyModel.Id,
            ProviderModelId = lowestLatencyModel.ProviderModelId,
            RoutingStrategy = "LatencyOptimized",
            RoutingReason = $"Lowest latency model (estimated: {lowestLatency:F0}ms)",
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
