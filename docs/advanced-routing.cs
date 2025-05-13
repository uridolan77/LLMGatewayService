// src/LLMGateway.Core/Routing/IModelRouter.cs
using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;

namespace LLMGateway.Core.Routing;

public interface IModelRouter
{
    Task<string> SelectModelAsync(string requestedModelId, CompletionRequest request, string userId);
    Task<string> SelectModelForEmbeddingAsync(string requestedModelId, EmbeddingRequest request, string userId);
    Task RecordRoutingDecisionAsync(RoutingDecision decision);
    Task RecordModelMetricsAsync(ModelMetrics metrics);
}

// src/LLMGateway.Core/Routing/RoutingDecision.cs
namespace LLMGateway.Core.Routing;

public class RoutingDecision
{
    public string OriginalModelId { get; set; } = string.Empty;
    public string SelectedModelId { get; set; } = string.Empty;
    public string RoutingStrategy { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? RequestContent { get; set; }
    public int? RequestTokenCount { get; set; }
    public bool IsFallback { get; set; }
    public string? FallbackReason { get; set; }
    public int LatencyMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// src/LLMGateway.Core/Routing/ModelMetrics.cs
namespace LLMGateway.Core.Routing;

public class ModelMetrics
{
    public string ModelId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int RequestTokens { get; set; }
    public int ResponseTokens { get; set; }
    public int LatencyMs { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public double Cost { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// src/LLMGateway.Core/Routing/RoutingStrategy.cs
namespace LLMGateway.Core.Routing;

public enum RoutingStrategy
{
    Direct,          // Use exactly the model that was requested
    CostOptimized,   // Route to the most cost-effective model
    LatencyOptimized,// Route to the lowest latency model
    QualityOptimized,// Route to the highest quality model
    LoadBalanced,    // Distribute load across models
    ContentBased,    // Select model based on content analysis
    UserPreference,  // Use user's preferred model
    Experimental     // A/B testing between models
}

// src/LLMGateway.Core/Routing/ContentClassifier.cs
namespace LLMGateway.Core.Routing;

public static class ContentClassifier
{
    public static ContentClassification ClassifyContent(string content)
    {
        var classification = new ContentClassification();
        
        // Check for code in the content
        if (ContainsCode(content))
        {
            classification.ContainsCode = true;
            classification.CodeLanguages = DetectCodeLanguages(content);
        }
        
        // Check for mathematical content
        if (ContainsMath(content))
        {
            classification.ContainsMath = true;
        }
        
        // Check for creative writing request
        if (IsCreativeWritingRequest(content))
        {
            classification.IsCreativeRequest = true;
        }
        
        // Detect primary language
        classification.PrimaryLanguage = DetectLanguage(content);
        
        // Estimate complexity
        classification.Complexity = EstimateComplexity(content);
        
        return classification;
    }
    
    private static bool ContainsCode(string content)
    {
        // Simple heuristic: look for common programming patterns
        return content.Contains("```") ||
               content.Contains("def ") ||
               content.Contains("function ") ||
               content.Contains("class ") ||
               content.Contains("import ") ||
               content.Contains("var ") ||
               content.Contains("const ") ||
               content.Contains("let ") ||
               content.Contains("public ") ||
               content.Contains("private ") ||
               content.Contains("if (") ||
               content.Contains("for (");
    }
    
    private static List<string> DetectCodeLanguages(string content)
    {
        var languages = new List<string>();
        
        if (content.Contains("def ") || content.Contains("import ") && content.Contains("print("))
            languages.Add("python");
            
        if (content.Contains("function ") || content.Contains("const ") || content.Contains("let ") || content.Contains("var "))
            languages.Add("javascript");
            
        if (content.Contains("public class ") || content.Contains("private void ") || content.Contains("namespace "))
            languages.Add("csharp");
            
        if (content.Contains("func ") || content.Contains("package "))
            languages.Add("go");
            
        if (content.Contains("fn ") || content.Contains("pub struct ") || content.Contains("impl "))
            languages.Add("rust");
            
        return languages.Count > 0 ? languages : new List<string> { "unknown" };
    }
    
    private static bool ContainsMath(string content)
    {
        // Look for mathematical notations and symbols
        return content.Contains("\\frac") ||
               content.Contains("\\sum") ||
               content.Contains("\\int") ||
               content.Contains("\\lim") ||
               content.Contains("\\mathbb") ||
               content.Contains("\\sqrt") ||
               (content.Contains("$") && (content.Contains("^") || content.Contains("_"))) ||
               content.Contains("calcul") ||
               content.Contains("equation");
    }
    
    private static bool IsCreativeWritingRequest(string content)
    {
        var contentLower = content.ToLowerInvariant();
        
        return contentLower.Contains("write a story") ||
               contentLower.Contains("write a poem") ||
               contentLower.Contains("creative writing") ||
               contentLower.Contains("fictional") ||
               contentLower.Contains("narrative") ||
               (contentLower.Contains("write") && contentLower.Contains("essay"));
    }
    
    private static string DetectLanguage(string content)
    {
        // Simplified language detection based on common words
        // In a real implementation, use a proper NLP library
        
        var contentLower = content.ToLowerInvariant();
        
        if (contentLower.Contains("the ") && contentLower.Contains("and ") && contentLower.Contains("for "))
            return "english";
            
        if (contentLower.Contains("el ") && contentLower.Contains("la ") && contentLower.Contains("que "))
            return "spanish";
            
        if (contentLower.Contains("le ") && contentLower.Contains("la ") && contentLower.Contains("est "))
            return "french";
            
        if (contentLower.Contains("der ") && contentLower.Contains("die ") && contentLower.Contains("und "))
            return "german";
            
        return "unknown";
    }
    
    private static ContentComplexity EstimateComplexity(string content)
    {
        // Count markers of complexity
        var sentenceCount = content.Split(new[] { '.', '!', '?' }).Length;
        var wordCount = content.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var avgWordsPerSentence = sentenceCount > 0 ? wordCount / (double)sentenceCount : 0;
        
        // Simple heuristic
        if (avgWordsPerSentence > 25 || content.Length > 1000)
            return ContentComplexity.High;
            
        if (avgWordsPerSentence > 15 || content.Length > 500)
            return ContentComplexity.Medium;
            
        return ContentComplexity.Low;
    }
}

// src/LLMGateway.Core/Routing/ContentClassification.cs
namespace LLMGateway.Core.Routing;

public class ContentClassification
{
    public bool ContainsCode { get; set; }
    public List<string> CodeLanguages { get; set; } = new();
    public bool ContainsMath { get; set; }
    public bool IsCreativeRequest { get; set; }
    public string PrimaryLanguage { get; set; } = "unknown";
    public ContentComplexity Complexity { get; set; } = ContentComplexity.Medium;
}

// src/LLMGateway.Core/Routing/ContentComplexity.cs
namespace LLMGateway.Core.Routing;

public enum ContentComplexity
{
    Low,
    Medium,
    High
}

// src/LLMGateway.Core/Routing/SmartModelRouter.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace LLMGateway.Core.Routing;

public class SmartModelRouter : IModelRouter
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IRoutingRepository _routingRepository;
    private readonly IModelMetricsRepository _modelMetricsRepository;
    private readonly IOptions<RoutingOptions> _routingOptions;
    private readonly IOptions<UserPreferencesOptions> _userPreferencesOptions;
    private readonly ILogger<SmartModelRouter> _logger;
    
    private readonly Random _random = new();
    
    public SmartModelRouter(
        ILLMProviderFactory providerFactory,
        IRoutingRepository routingRepository,
        IModelMetricsRepository modelMetricsRepository,
        IOptions<RoutingOptions> routingOptions,
        IOptions<UserPreferencesOptions> userPreferencesOptions,
        ILogger<SmartModelRouter> logger)
    {
        _providerFactory = providerFactory;
        _routingRepository = routingRepository;
        _modelMetricsRepository = modelMetricsRepository;
        _routingOptions = routingOptions;
        _userPreferencesOptions = userPreferencesOptions;
        _logger = logger;
    }
    
    public async Task<string> SelectModelAsync(string requestedModelId, CompletionRequest request, string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Get available models that can handle completions
            var availableModels = await GetAvailableCompletionModelsAsync();
            
            // Prioritize explicit model mappings
            var mappedModelId = GetMappedModelId(requestedModelId);
            if (!string.IsNullOrEmpty(mappedModelId) && availableModels.Any(m => m.Id == mappedModelId))
            {
                requestedModelId = mappedModelId;
            }
            
            // Apply routing strategy
            var strategy = DetermineRoutingStrategy(requestedModelId, request, userId);
            string selectedModelId;
            
            switch (strategy)
            {
                case RoutingStrategy.Direct:
                    selectedModelId = requestedModelId;
                    break;
                    
                case RoutingStrategy.CostOptimized:
                    selectedModelId = await SelectCostOptimizedModelAsync(requestedModelId, request, availableModels);
                    break;
                    
                case RoutingStrategy.LatencyOptimized:
                    selectedModelId = await SelectLatencyOptimizedModelAsync(requestedModelId, availableModels);
                    break;
                    
                case RoutingStrategy.QualityOptimized:
                    selectedModelId = SelectQualityOptimizedModel(requestedModelId, request, availableModels);
                    break;
                    
                case RoutingStrategy.LoadBalanced:
                    selectedModelId = await SelectLoadBalancedModelAsync(requestedModelId, availableModels);
                    break;
                    
                case RoutingStrategy.ContentBased:
                    selectedModelId = SelectContentBasedModel(requestedModelId, request, availableModels);
                    break;
                    
                case RoutingStrategy.UserPreference:
                    selectedModelId = await SelectUserPreferredModelAsync(requestedModelId, userId, availableModels);
                    break;
                    
                case RoutingStrategy.Experimental:
                    selectedModelId = SelectExperimentalModel(requestedModelId, availableModels);
                    break;
                    
                default:
                    selectedModelId = requestedModelId;
                    break;
            }
            
            // If the selected model doesn't exist or isn't capable of completions, fall back to the requested model
            if (!availableModels.Any(m => m.Id == selectedModelId))
            {
                _logger.LogWarning("Selected model {SelectedModelId} is not available, falling back to {RequestedModelId}", 
                    selectedModelId, requestedModelId);
                selectedModelId = requestedModelId;
                strategy = RoutingStrategy.Direct;
            }
            
            stopwatch.Stop();
            
            // Record the routing decision
            var decision = new RoutingDecision
            {
                OriginalModelId = requestedModelId,
                SelectedModelId = selectedModelId,
                RoutingStrategy = strategy.ToString(),
                UserId = userId,
                RequestContent = GetContentSummary(request),
                RequestTokenCount = EstimateTokenCount(request),
                IsFallback = false,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                Timestamp = DateTime.UtcNow
            };
            
            await RecordRoutingDecisionAsync(decision);
            
            return selectedModelId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting model: {ErrorMessage}", ex.Message);
            return requestedModelId; // Fall back to the requested model
        }
    }
    
    public async Task<string> SelectModelForEmbeddingAsync(string requestedModelId, EmbeddingRequest request, string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Get available models that can handle embeddings
            var availableModels = await GetAvailableEmbeddingModelsAsync();
            
            // Prioritize explicit model mappings
            var mappedModelId = GetMappedModelId(requestedModelId);
            if (!string.IsNullOrEmpty(mappedModelId) && availableModels.Any(m => m.Id == mappedModelId))
            {
                requestedModelId = mappedModelId;
            }
            
            // For embeddings, we currently just support direct routing and cost optimization
            var strategy = _routingOptions.Value.EnableCostOptimizedRouting ? 
                RoutingStrategy.CostOptimized : RoutingStrategy.Direct;
            
            string selectedModelId = strategy == RoutingStrategy.CostOptimized
                ? await SelectCostOptimizedEmbeddingModelAsync(requestedModelId, availableModels)
                : requestedModelId;
            
            // If the selected model doesn't exist or isn't capable of embeddings, fall back to the requested model
            if (!availableModels.Any(m => m.Id == selectedModelId))
            {
                _logger.LogWarning("Selected embedding model {SelectedModelId} is not available, falling back to {RequestedModelId}", 
                    selectedModelId, requestedModelId);
                selectedModelId = requestedModelId;
                strategy = RoutingStrategy.Direct;
            }
            
            stopwatch.Stop();
            
            // Record the routing decision
            var decision = new RoutingDecision
            {
                OriginalModelId = requestedModelId,
                SelectedModelId = selectedModelId,
                RoutingStrategy = strategy.ToString(),
                UserId = userId,
                RequestContent = "embedding-request", // Don't store embedding content
                RequestTokenCount = EstimateEmbeddingTokenCount(request),
                IsFallback = false,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                Timestamp = DateTime.UtcNow
            };
            
            await RecordRoutingDecisionAsync(decision);
            
            return selectedModelId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting embedding model: {ErrorMessage}", ex.Message);
            return requestedModelId; // Fall back to the requested model
        }
    }
    
    public async Task RecordRoutingDecisionAsync(RoutingDecision decision)
    {
        if (_routingOptions.Value.TrackRoutingDecisions)
        {
            try
            {
                await _routingRepository.AddRoutingDecisionAsync(decision);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording routing decision: {ErrorMessage}", ex.Message);
            }
        }
    }
    
    public async Task RecordModelMetricsAsync(ModelMetrics metrics)
    {
        if (_routingOptions.Value.TrackModelMetrics)
        {
            try
            {
                await _modelMetricsRepository.AddModelMetricsAsync(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording model metrics: {ErrorMessage}", ex.Message);
            }
        }
    }
    
    private async Task<List<ModelInfo>> GetAvailableCompletionModelsAsync()
    {
        var allModels = await _providerFactory.GetAllModelsAsync();
        return allModels.Where(m => m.Capabilities.SupportsCompletion).ToList();
    }
    
    private async Task<List<ModelInfo>> GetAvailableEmbeddingModelsAsync()
    {
        var allModels = await _providerFactory.GetAllModelsAsync();
        return allModels.Where(m => m.Capabilities.SupportsEmbedding).ToList();
    }
    
    private string GetMappedModelId(string modelId)
    {
        var mapping = _routingOptions.Value.ModelMappings
            .FirstOrDefault(m => m.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
            
        return mapping?.TargetModelId ?? string.Empty;
    }
    
    private RoutingStrategy DetermineRoutingStrategy(string modelId, CompletionRequest request, string userId)
    {
        // Check if the user has a specific routing preference
        var userPreference = _userPreferencesOptions.Value.UserRoutingPreferences
            .FirstOrDefault(p => p.UserId == userId);
            
        if (userPreference != null && userPreference.RoutingStrategy != RoutingStrategy.Direct)
        {
            return userPreference.RoutingStrategy;
        }
        
        // Check if there's a specific routing strategy for this model
        var modelRouting = _routingOptions.Value.ModelRoutingStrategies
            .FirstOrDefault(s => s.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
            
        if (modelRouting != null)
        {
            return modelRouting.Strategy;
        }
        
        // Check if content-based routing is enabled and analyze the content
        if (_routingOptions.Value.EnableContentBasedRouting && request.Messages.Count > 0)
        {
            var lastMessage = request.Messages.Last();
            
            if (lastMessage.Role == "user" && !string.IsNullOrEmpty(lastMessage.Content))
            {
                var classification = ContentClassifier.ClassifyContent(lastMessage.Content);
                
                // Route to code-optimized models for code-heavy requests
                if (classification.ContainsCode && classification.Complexity == ContentComplexity.High)
                {
                    return RoutingStrategy.ContentBased;
                }
                
                // Route to creative models for creative writing requests
                if (classification.IsCreativeRequest)
                {
                    return RoutingStrategy.ContentBased;
                }
                
                // Route to specialized models for math-heavy content
                if (classification.ContainsMath)
                {
                    return RoutingStrategy.ContentBased;
                }
            }
        }
        
        // Apply the default strategy
        if (_routingOptions.Value.EnableLoadBalancing)
        {
            return RoutingStrategy.LoadBalanced;
        }
        
        if (_routingOptions.Value.EnableLatencyOptimizedRouting)
        {
            return RoutingStrategy.LatencyOptimized;
        }
        
        if (_routingOptions.Value.EnableCostOptimizedRouting)
        {
            return RoutingStrategy.CostOptimized;
        }
        
        if (_routingOptions.Value.EnableExperimentalRouting)
        {
            return RoutingStrategy.Experimental;
        }
        
        // Default to direct routing
        return RoutingStrategy.Direct;
    }
    
    private async Task<string> SelectCostOptimizedModelAsync(
        string originalModelId, 
        CompletionRequest request, 
        List<ModelInfo> availableModels)
    {
        // Get the original model info
        var originalModel = availableModels.FirstOrDefault(m => m.Id == originalModelId);
        
        if (originalModel == null)
        {
            return originalModelId;
        }
        
        // Get context length required for this request
        var requestTokens = EstimateTokenCount(request);
        var totalTokens = requestTokens + (request.MaxTokens ?? 1000);
        
        // Find models that can handle this request
        var compatibleModels = availableModels
            .Where(m => m.ContextWindow >= totalTokens)
            .ToList();
        
        if (!compatibleModels.Any())
        {
            return originalModelId;
        }
        
        // Get the model metrics for cost calculation
        var modelMetrics = await _modelMetricsRepository.GetModelMetricsAsync(
            compatibleModels.Select(m => m.Id).ToList());
        
        // Find the model with the lowest expected cost
        var lowestCostModel = compatibleModels
            .Where(m => m.Properties.ContainsKey("TokenPriceInput") && 
                        m.Properties.ContainsKey("TokenPriceOutput"))
            .OrderBy(m => 
                double.Parse(m.Properties["TokenPriceInput"]) * requestTokens +
                double.Parse(m.Properties["TokenPriceOutput"]) * (request.MaxTokens ?? 1000))
            .FirstOrDefault();
        
        return lowestCostModel?.Id ?? originalModelId;
    }
    
    private async Task<string> SelectCostOptimizedEmbeddingModelAsync(
        string originalModelId, 
        List<ModelInfo> availableModels)
    {
        // Get the original model info
        var originalModel = availableModels.FirstOrDefault(m => m.Id == originalModelId);
        
        if (originalModel == null)
        {
            return originalModelId;
        }
        
        // Get the model metrics for cost calculation
        var modelMetrics = await _modelMetricsRepository.GetModelMetricsAsync(
            availableModels.Select(m => m.Id).ToList());
        
        // Find the model with the lowest cost per token
        var lowestCostModel = availableModels
            .Where(m => m.Properties.ContainsKey("TokenPriceInput"))
            .OrderBy(m => double.Parse(m.Properties["TokenPriceInput"]))
            .FirstOrDefault();
        
        return lowestCostModel?.Id ?? originalModelId;
    }
    
    private async Task<string> SelectLatencyOptimizedModelAsync(
        string originalModelId, 
        List<ModelInfo> availableModels)
    {
        // Get provider for the original model to ensure we stay within the same provider
        var originalProvider = GetProviderForModel(originalModelId);
        
        if (string.IsNullOrEmpty(originalProvider))
        {
            return originalModelId;
        }
        
        // Get models from the same provider
        var sameProviderModels = availableModels
            .Where(m => m.Provider == originalProvider)
            .ToList();
        
        if (!sameProviderModels.Any())
        {
            return originalModelId;
        }
        
        // Get the model metrics for latency comparison
        var modelMetrics = await _modelMetricsRepository.GetModelMetricsAsync(
            sameProviderModels.Select(m => m.Id).ToList());
        
        // Find the model with the lowest average latency
        var fastestModel = modelMetrics
            .OrderBy(m => m.AverageLatencyMs)
            .FirstOrDefault();
        
        return fastestModel?.ModelId ?? originalModelId;
    }
    
    private string SelectQualityOptimizedModel(
        string originalModelId, 
        CompletionRequest request,
        List<ModelInfo> availableModels)
    {
        // For quality-optimized routing, we want the most advanced model that can handle the request
        // This is usually the latest/largest model from each provider
        
        // Define model quality rankings (simplified approach)
        var modelRankings = new Dictionary<string, int>
        {
            { "openai.gpt-4-turbo", 100 },
            { "openai.gpt-4", 95 },
            { "anthropic.claude-3-opus", 98 },
            { "anthropic.claude-3-sonnet", 90 },
            { "anthropic.claude-3-haiku", 85 },
            { "openai.gpt-3.5-turbo", 80 },
            { "cohere.command-r", 85 },
            { "cohere.command", 80 }
        };
        
        // Get the ranking of the original model
        int originalRanking = modelRankings.ContainsKey(originalModelId) ? 
            modelRankings[originalModelId] : 50;
        
        // Find models with equal or better ranking
        var betterModels = availableModels
            .Where(m => modelRankings.ContainsKey(m.Id) && modelRankings[m.Id] >= originalRanking)
            .OrderByDescending(m => modelRankings[m.Id])
            .ToList();
        
        if (!betterModels.Any())
        {
            return originalModelId;
        }
        
        return betterModels.First().Id;
    }
    
    private async Task<string> SelectLoadBalancedModelAsync(
        string originalModelId, 
        List<ModelInfo> availableModels)
    {
        // Get provider for the original model
        var originalProvider = GetProviderForModel(originalModelId);
        
        if (string.IsNullOrEmpty(originalProvider))
        {
            return originalModelId;
        }
        
        // Get the health status of providers
        var providerHealth = await _routingRepository.GetProviderHealthStatusAsync();
        
        // Check if the original provider is healthy
        var isOriginalProviderHealthy = providerHealth
            .Any(p => p.ProviderName == originalProvider && p.Status == "Healthy");
        
        if (!isOriginalProviderHealthy)
        {
            // Find a healthy alternative provider
            var healthyProviders = providerHealth
                .Where(p => p.Status == "Healthy")
                .Select(p => p.ProviderName)
                .ToList();
            
            if (healthyProviders.Any())
            {
                // Select a random healthy provider
                var randomProvider = healthyProviders[_random.Next(healthyProviders.Count)];
                
                // Find similar models from the random provider
                var alternativeModels = availableModels
                    .Where(m => m.Provider == randomProvider)
                    .ToList();
                
                if (alternativeModels.Any())
                {
                    return alternativeModels[_random.Next(alternativeModels.Count)].Id;
                }
            }
        }
        
        // Get current throughput for models from the original provider
        var modelThroughput = await _modelMetricsRepository.GetModelThroughputAsync(
            availableModels.Where(m => m.Provider == originalProvider).Select(m => m.Id).ToList());
        
        // Use throughput to balance load
        if (modelThroughput.Any())
        {
            var leastUsedModel = modelThroughput
                .OrderBy(m => m.ThroughputPerMinute)
                .First();
            
            return leastUsedModel.ModelId;
        }
        
        return originalModelId;
    }
    
    private string SelectContentBasedModel(
        string originalModelId, 
        CompletionRequest request,
        List<ModelInfo> availableModels)
    {
        if (request.Messages.Count == 0)
        {
            return originalModelId;
        }
        
        // Get the last user message for content analysis
        var lastUserMessage = request.Messages
            .LastOrDefault(m => m.Role == "user");
            
        if (lastUserMessage == null || string.IsNullOrEmpty(lastUserMessage.Content))
        {
            return originalModelId;
        }
        
        // Analyze the content
        var classification = ContentClassifier.ClassifyContent(lastUserMessage.Content);
        
        // Define model specialties
        var modelSpecialties = new Dictionary<string, List<string>>
        {
            { "code", new List<string> { "openai.gpt-4-turbo", "anthropic.claude-3-opus" } },
            { "math", new List<string> { "openai.gpt-4-turbo", "anthropic.claude-3-opus" } },
            { "creative", new List<string> { "anthropic.claude-3-opus", "anthropic.claude-3-sonnet" } }
        };
        
        // Select a model based on content type
        if (classification.ContainsCode && modelSpecialties.ContainsKey("code"))
        {
            var codeModels = modelSpecialties["code"]
                .Where(m => availableModels.Any(a => a.Id == m))
                .ToList();
                
            if (codeModels.Any())
            {
                return codeModels.First();
            }
        }
        
        if (classification.ContainsMath && modelSpecialties.ContainsKey("math"))
        {
            var mathModels = modelSpecialties["math"]
                .Where(m => availableModels.Any(a => a.Id == m))
                .ToList();
                
            if (mathModels.Any())
            {
                return mathModels.First();
            }
        }
        
        if (classification.IsCreativeRequest && modelSpecialties.ContainsKey("creative"))
        {
            var creativeModels = modelSpecialties["creative"]
                .Where(m => availableModels.Any(a => a.Id == m))
                .ToList();
                
            if (creativeModels.Any())
            {
                return creativeModels.First();
            }
        }
        
        // If we can't find a specialized model, use the original model
        return originalModelId;
    }
    
    private async Task<string> SelectUserPreferredModelAsync(
        string originalModelId, 
        string userId,
        List<ModelInfo> availableModels)
    {
        // Check if the user has a preferred model
        var userPreference = _userPreferencesOptions.Value.UserModelPreferences
            .FirstOrDefault(p => p.UserId == userId);
            
        if (userPreference != null && !string.IsNullOrEmpty(userPreference.PreferredModelId))
        {
            var preferredModel = availableModels
                .FirstOrDefault(m => m.Id == userPreference.PreferredModelId);
                
            if (preferredModel != null)
            {
                return preferredModel.Id;
            }
        }
        
        // If no explicit preference, check user history
        var userHistory = await _routingRepository.GetUserRoutingHistoryAsync(userId, 20);
        
        if (userHistory.Any())
        {
            // Find the most frequently used model by this user
            var mostUsedModel = userHistory
                .GroupBy(h => h.SelectedModelId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();
                
            if (!string.IsNullOrEmpty(mostUsedModel) && 
                availableModels.Any(m => m.Id == mostUsedModel))
            {
                return mostUsedModel;
            }
        }
        
        return originalModelId;
    }
    
    private string SelectExperimentalModel(string originalModelId, List<ModelInfo> availableModels)
    {
        // For experimental routing, we randomly select an alternative model
        // This is useful for A/B testing or evaluating model performance
        
        if (!_routingOptions.Value.ExperimentalModels.Any())
        {
            return originalModelId;
        }
        
        // Check if we should run an experiment based on the sampling rate
        if (_random.NextDouble() > _routingOptions.Value.ExperimentalSamplingRate)
        {
            return originalModelId;
        }
        
        // Pick a random model from the experimental set
        var experimentalModels = _routingOptions.Value.ExperimentalModels
            .Where(m => availableModels.Any(a => a.Id == m))
            .ToList();
            
        if (experimentalModels.Any())
        {
            return experimentalModels[_random.Next(experimentalModels.Count)];
        }
        
        return originalModelId;
    }
    
    private string GetProviderForModel(string modelId)
    {
        var model = _providerFactory.GetProviderForModel(modelId);
        return model?.ProviderName ?? string.Empty;
    }
    
    private string GetContentSummary(CompletionRequest request)
    {
        if (request.Messages.Count == 0)
        {
            return string.Empty;
        }
        
        var lastUserMessage = request.Messages
            .LastOrDefault(m => m.Role == "user");
            
        if (lastUserMessage == null || string.IsNullOrEmpty(lastUserMessage.Content))
        {
            return string.Empty;
        }
        
        // Return a truncated summary
        var content = lastUserMessage.Content;
        
        if (content.Length > 100)
        {
            return content.Substring(0, 97) + "...";
        }
        
        return content;
    }
    
    private int EstimateTokenCount(CompletionRequest request)
    {
        int totalTokens = 0;
        
        foreach (var message in request.Messages)
        {
            // Simple approximation: 4 characters ~= 1 token
            totalTokens += message.Content.Length / 4 + 5; // +5 for message metadata
        }
        
        return totalTokens;
    }
    
    private int EstimateEmbeddingTokenCount(EmbeddingRequest request)
    {
        int totalTokens = 0;
        
        if (request.Input is string text)
        {
            totalTokens = text.Length / 4 + 1;
        }
        else if (request.Input is IEnumerable<string> texts)
        {
            foreach (var item in texts)
            {
                totalTokens += item.Length / 4 + 1;
            }
        }
        
        return totalTokens;
    }
}

// src/LLMGateway.Core/Options/RoutingOptions.cs
namespace LLMGateway.Core.Options;

using LLMGateway.Core.Routing;

public class RoutingOptions
{
    public bool EnableSmartRouting { get; set; } = true;
    public bool EnableLoadBalancing { get; set; } = false;
    public bool EnableLatencyOptimizedRouting { get; set; } = false;
    public bool EnableCostOptimizedRouting { get; set; } = true;
    public bool EnableContentBasedRouting { get; set; } = true;
    public bool TrackRoutingDecisions { get; set; } = true;
    public bool TrackModelMetrics { get; set; } = true;
    public bool EnableExperimentalRouting { get; set; } = false;
    public double ExperimentalSamplingRate { get; set; } = 0.1;
    public List<string> ExperimentalModels { get; set; } = new();
    
    public List<ModelMapping> ModelMappings { get; set; } = new();
    public List<ModelRoutingStrategy> ModelRoutingStrategies { get; set; } = new();
}

public class ModelMapping
{
    public string ModelId { get; set; } = string.Empty;
    public string TargetModelId { get; set; } = string.Empty;
}

public class ModelRoutingStrategy
{
    public string ModelId { get; set; } = string.Empty;
    public RoutingStrategy Strategy { get; set; } = RoutingStrategy.Direct;
}

// src/LLMGateway.Core/Options/UserPreferencesOptions.cs
namespace LLMGateway.Core.Options;

using LLMGateway.Core.Routing;

public class UserPreferencesOptions
{
    public List<UserRoutingPreference> UserRoutingPreferences { get; set; } = new();
    public List<UserModelPreference> UserModelPreferences { get; set; } = new();
}

public class UserRoutingPreference
{
    public string UserId { get; set; } = string.Empty;
    public RoutingStrategy RoutingStrategy { get; set; } = RoutingStrategy.Direct;
}

public class UserModelPreference
{
    public string UserId { get; set; } = string.Empty;
    public string PreferredModelId { get; set; } = string.Empty;
}

// src/LLMGateway.Infrastructure/Persistence/Repositories/IRoutingRepository.cs
using LLMGateway.Core.Routing;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

public interface IRoutingRepository
{
    Task AddRoutingDecisionAsync(RoutingDecision decision);
    Task<List<RoutingDecision>> GetUserRoutingHistoryAsync(string userId, int limit);
    Task<List<ProviderHealthStatus>> GetProviderHealthStatusAsync();
}

// src/LLMGateway.Infrastructure/Persistence/Repositories/RoutingRepository.cs
using LLMGateway.Core.Routing;
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

public class RoutingRepository : IRoutingRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<RoutingRepository> _logger;
    
    public RoutingRepository(
        LLMGatewayDbContext dbContext,
        ILogger<RoutingRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task AddRoutingDecisionAsync(RoutingDecision decision)
    {
        try
        {
            var entity = new RoutingHistoryEntity
            {
                OriginalModelId = decision.OriginalModelId,
                SelectedModelId = decision.SelectedModelId,
                RoutingStrategy = decision.RoutingStrategy,
                UserId = decision.UserId,
                RequestContent = decision.RequestContent,
                RequestTokenCount = decision.RequestTokenCount,
                IsFallback = decision.IsFallback,
                FallbackReason = decision.FallbackReason,
                LatencyMs = decision.LatencyMs,
                Timestamp = decision.Timestamp
            };
            
            _dbContext.RoutingHistory.Add(entity);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving routing decision to database: {ErrorMessage}", ex.Message);
            // We don't want to throw an exception here, as it's just for tracking
        }
    }
    
    public async Task<List<RoutingDecision>> GetUserRoutingHistoryAsync(string userId, int limit)
    {
        try
        {
            var entities = await _dbContext.RoutingHistory
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Timestamp)
                .Take(limit)
                .ToListAsync();
            
            return entities.Select(e => new RoutingDecision
            {
                OriginalModelId = e.OriginalModelId,
                SelectedModelId = e.SelectedModelId,
                RoutingStrategy = e.RoutingStrategy,
                UserId = e.UserId,
                RequestContent = e.RequestContent,
                RequestTokenCount = e.RequestTokenCount,
                IsFallback = e.IsFallback,
                FallbackReason = e.FallbackReason,
                LatencyMs = e.LatencyMs,
                Timestamp = e.Timestamp
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user routing history from database: {ErrorMessage}", ex.Message);
            return new List<RoutingDecision>();
        }
    }
    
    public async Task<List<ProviderHealthStatus>> GetProviderHealthStatusAsync()
    {
        try
        {
            // Group by provider name and get the latest status for each provider
            var latestHealthChecks = await _dbContext.ProviderHealthChecks
                .GroupBy(h => h.ProviderName)
                .Select(g => g.OrderByDescending(h => h.Timestamp).FirstOrDefault())
                .ToListAsync();
            
            return latestHealthChecks.Select(e => new ProviderHealthStatus
            {
                ProviderName = e.ProviderName,
                Status = e.Status,
                LatencyMs = e.LatencyMs,
                ErrorMessage = e.ErrorMessage,
                LastChecked = e.Timestamp
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provider health status from database: {ErrorMessage}", ex.Message);
            return new List<ProviderHealthStatus>();
        }
    }
}

// src/LLMGateway.Core/Routing/ProviderHealthStatus.cs
namespace LLMGateway.Core.Routing;

public class ProviderHealthStatus
{
    public string ProviderName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime LastChecked { get; set; }
}

// src/LLMGateway.Infrastructure/Persistence/Repositories/IModelMetricsRepository.cs
using LLMGateway.Core.Routing;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

public interface IModelMetricsRepository
{
    Task AddModelMetricsAsync(ModelMetrics metrics);
    Task<List<ModelMetricsData>> GetModelMetricsAsync(List<string> modelIds);
    Task<List<ModelThroughputData>> GetModelThroughputAsync(List<string> modelIds);
}

// src/LLMGateway.Core/Routing/ModelMetricsData.cs
namespace LLMGateway.Core.Routing;

public class ModelMetricsData
{
    public string ModelId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public double AverageLatencyMs { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRate => ErrorCount > 0 ? (double)ErrorCount / (SuccessCount + ErrorCount) : 0;
    public double AvgCostPerRequest { get; set; }
}

// src/LLMGateway.Core/Routing/ModelThroughputData.cs
namespace LLMGateway.Core.Routing;

public class ModelThroughputData
{
    public string ModelId { get; set; } = string.Empty;
    public double ThroughputPerMinute { get; set; }
}

// src/LLMGateway.Infrastructure/Persistence/Repositories/ModelMetricsRepository.cs
using LLMGateway.Core.Routing;
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

public class ModelMetricsRepository : IModelMetricsRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<ModelMetricsRepository> _logger;
    
    public ModelMetricsRepository(
        LLMGatewayDbContext dbContext,
        ILogger<ModelMetricsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task AddModelMetricsAsync(ModelMetrics metrics)
    {
        try
        {
            // Check if there's an existing metrics record for this model
            var existingMetrics = await _dbContext.ModelMetrics
                .FirstOrDefaultAsync(m => m.ModelId == metrics.ModelId);
            
            if (existingMetrics != null)
            {
                // Update existing metrics
                existingMetrics.AverageLatencyMs = (existingMetrics.AverageLatencyMs * (existingMetrics.SuccessCount + existingMetrics.ErrorCount) + metrics.LatencyMs) /
                                                  (existingMetrics.SuccessCount + existingMetrics.ErrorCount + 1);
                
                if (metrics.IsSuccess)
                {
                    existingMetrics.SuccessCount += 1;
                }
                else
                {
                    existingMetrics.ErrorCount += 1;
                }
                
                existingMetrics.CostPerRequest = (existingMetrics.CostPerRequest * (existingMetrics.SuccessCount + existingMetrics.ErrorCount - 1) + metrics.Cost) /
                                                (existingMetrics.SuccessCount + existingMetrics.ErrorCount);
                
                existingMetrics.LastUpdated = DateTime.UtcNow;
                
                // Calculate and update throughput
                var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
                var recentRequests = await _dbContext.RequestLogs
                    .CountAsync(r => r.ModelId == metrics.ModelId && r.Timestamp >= oneMinuteAgo);
                
                existingMetrics.ThroughputPerMinute = recentRequests;
            }
            else
            {
                // Create new metrics record
                var entity = new ModelMetricsEntity
                {
                    ModelId = metrics.ModelId,
                    Provider = metrics.Provider,
                    AverageLatencyMs = metrics.LatencyMs,
                    SuccessCount = metrics.IsSuccess ? 1 : 0,
                    ErrorCount = metrics.IsSuccess ? 0 : 1,
                    ThroughputPerMinute = 1, // First request
                    CostPerRequest = metrics.Cost,
                    LastUpdated = DateTime.UtcNow
                };
                
                _dbContext.ModelMetrics.Add(entity);
            }
            
            // Also log this request
            var requestLog = new RequestLogEntity
            {
                RequestType = "completion",
                ModelId = metrics.ModelId,
                UserId = "unknown", // This should be passed in the metrics
                PromptTokens = metrics.RequestTokens,
                CompletionTokens = metrics.ResponseTokens,
                LatencyMs = metrics.LatencyMs,
                IsSuccess = metrics.IsSuccess,
                ErrorMessage = metrics.ErrorMessage,
                Timestamp = DateTime.UtcNow
            };
            
            _dbContext.RequestLogs.Add(requestLog);
            
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving model metrics to database: {ErrorMessage}", ex.Message);
            // We don't want to throw an exception here, as it's just for tracking
        }
    }
    
    public async Task<List<ModelMetricsData>> GetModelMetricsAsync(List<string> modelIds)
    {
        try
        {
            var entities = await _dbContext.ModelMetrics
                .Where(m => modelIds.Contains(m.ModelId))
                .ToListAsync();
            
            return entities.Select(e => new ModelMetricsData
            {
                ModelId = e.ModelId,
                Provider = e.Provider,
                AverageLatencyMs = e.AverageLatencyMs,
                SuccessCount = e.SuccessCount,
                ErrorCount = e.ErrorCount,
                AvgCostPerRequest = e.CostPerRequest
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving model metrics from database: {ErrorMessage}", ex.Message);
            return new List<ModelMetricsData>();
        }
    }
    
    public async Task<List<ModelThroughputData>> GetModelThroughputAsync(List<string> modelIds)
    {
        try
        {
            var entities = await _dbContext.ModelMetrics
                .Where(m => modelIds.Contains(m.ModelId))
                .ToListAsync();
            
            return entities.Select(e => new ModelThroughputData
            {
                ModelId = e.ModelId,
                ThroughputPerMinute = e.ThroughputPerMinute
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving model throughput from database: {ErrorMessage}", ex.Message);
            return new List<ModelThroughputData>();
        }
    }
}
