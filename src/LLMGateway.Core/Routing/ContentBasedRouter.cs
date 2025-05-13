using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace LLMGateway.Core.Routing;

/// <summary>
/// Content-based router for selecting the appropriate model based on request content
/// </summary>
public class ContentBasedRouter : IContentBasedRouter
{
    private readonly IModelService _modelService;
    private readonly ILogger<ContentBasedRouter> _logger;
    private readonly RoutingOptions _routingOptions;
    
    // Regular expressions for content analysis
    private static readonly Regex CodeRegex = new(
        @"(```[\s\S]*?```)|(`[^`]+`)|(\b(function|class|def|var|const|let|import|from|public|private|protected|interface|implements|extends)\b)",
        RegexOptions.Compiled);
    
    private static readonly Regex MathRegex = new(
        @"(\$\$[\s\S]*?\$\$)|(\$[^$]+\$)|(\\\([\s\S]*?\\\))|(\\\[[\s\S]*?\\\])|(\b(sum|int|sqrt|frac|alpha|beta|gamma|delta|theta|pi|infty)\b)",
        RegexOptions.Compiled);
    
    private static readonly Regex CreativeRegex = new(
        @"\b(story|poem|creative|write a|fiction|narrative|tale|novel|plot|character|setting|scene|dialogue|verse|rhyme|stanza)\b",
        RegexOptions.Compiled);
    
    private static readonly Regex AnalyticalRegex = new(
        @"\b(analyze|analysis|evaluate|assessment|critique|review|examine|investigate|research|study|report|summarize|explain|describe|compare|contrast)\b",
        RegexOptions.Compiled);
    
    private static readonly Regex LongFormRegex = new(
        @"\b(essay|article|paper|report|document|thesis|dissertation|book|chapter|section|paragraph|page)\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="modelService">Model service</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Routing options</param>
    public ContentBasedRouter(
        IModelService modelService,
        ILogger<ContentBasedRouter> logger,
        IOptions<RoutingOptions> options)
    {
        _modelService = modelService;
        _logger = logger;
        _routingOptions = options.Value;
    }

    /// <inheritdoc/>
    public async Task<RoutingResult> RouteRequestAsync(CompletionRequest request)
    {
        _logger.LogInformation("Routing request based on content analysis");
        
        if (!_routingOptions.EnableContentBasedRouting)
        {
            _logger.LogInformation("Content-based routing is disabled");
            return new RoutingResult { Success = false };
        }
        
        // Analyze the content of the request
        var contentType = AnalyzeContent(request);
        _logger.LogInformation("Content analysis result: {ContentType}", contentType);
        
        // Get all available models
        var allModels = await _modelService.GetModelsAsync();
        
        // Select the appropriate model based on content type
        ModelInfo? selectedModel = null;
        string routingReason = string.Empty;
        
        switch (contentType)
        {
            case ContentType.Code:
                selectedModel = FindBestModelForCode(allModels);
                routingReason = "Code content detected";
                break;
                
            case ContentType.Math:
                selectedModel = FindBestModelForMath(allModels);
                routingReason = "Mathematical content detected";
                break;
                
            case ContentType.Creative:
                selectedModel = FindBestModelForCreative(allModels);
                routingReason = "Creative writing content detected";
                break;
                
            case ContentType.Analytical:
                selectedModel = FindBestModelForAnalytical(allModels);
                routingReason = "Analytical content detected";
                break;
                
            case ContentType.LongForm:
                selectedModel = FindBestModelForLongForm(allModels);
                routingReason = "Long-form content detected";
                break;
                
            default:
                _logger.LogInformation("No specific content type detected");
                return new RoutingResult { Success = false };
        }
        
        if (selectedModel == null)
        {
            _logger.LogInformation("No suitable model found for content type {ContentType}", contentType);
            return new RoutingResult { Success = false };
        }
        
        _logger.LogInformation("Selected model {ModelId} for content type {ContentType}", selectedModel.Id, contentType);
        
        return new RoutingResult
        {
            Provider = selectedModel.Provider,
            ModelId = selectedModel.Id,
            ProviderModelId = selectedModel.ProviderModelId,
            RoutingStrategy = "ContentBased",
            RoutingReason = routingReason,
            Success = true
        };
    }

    private ContentType AnalyzeContent(CompletionRequest request)
    {
        // Combine all user messages into a single string for analysis
        var content = string.Join(" ", request.Messages
            .Where(m => m.Role == "user")
            .Select(m => m.Content ?? string.Empty));
        
        // Check for code content
        if (CodeRegex.IsMatch(content))
        {
            return ContentType.Code;
        }
        
        // Check for mathematical content
        if (MathRegex.IsMatch(content))
        {
            return ContentType.Math;
        }
        
        // Check for creative writing content
        if (CreativeRegex.IsMatch(content))
        {
            return ContentType.Creative;
        }
        
        // Check for analytical content
        if (AnalyticalRegex.IsMatch(content))
        {
            return ContentType.Analytical;
        }
        
        // Check for long-form content
        if (LongFormRegex.IsMatch(content))
        {
            return ContentType.LongForm;
        }
        
        // Default to general content
        return ContentType.General;
    }

    private ModelInfo? FindBestModelForCode(IEnumerable<ModelInfo> models)
    {
        // Prioritize models that are good for code generation
        var preferredModels = new[]
        {
            "openai.gpt-4-turbo",
            "anthropic.claude-3-opus",
            "anthropic.claude-3-sonnet",
            "openai.gpt-4",
            "openai.gpt-3.5-turbo"
        };
        
        return FindModelByPreference(models, preferredModels);
    }

    private ModelInfo? FindBestModelForMath(IEnumerable<ModelInfo> models)
    {
        // Prioritize models that are good for mathematical content
        var preferredModels = new[]
        {
            "anthropic.claude-3-opus",
            "openai.gpt-4-turbo",
            "openai.gpt-4",
            "anthropic.claude-3-sonnet",
            "openai.gpt-3.5-turbo"
        };
        
        return FindModelByPreference(models, preferredModels);
    }

    private ModelInfo? FindBestModelForCreative(IEnumerable<ModelInfo> models)
    {
        // Prioritize models that are good for creative writing
        var preferredModels = new[]
        {
            "anthropic.claude-3-opus",
            "anthropic.claude-3-sonnet",
            "openai.gpt-4-turbo",
            "cohere.command-r-plus",
            "openai.gpt-4",
            "openai.gpt-3.5-turbo"
        };
        
        return FindModelByPreference(models, preferredModels);
    }

    private ModelInfo? FindBestModelForAnalytical(IEnumerable<ModelInfo> models)
    {
        // Prioritize models that are good for analytical content
        var preferredModels = new[]
        {
            "anthropic.claude-3-opus",
            "openai.gpt-4-turbo",
            "openai.gpt-4",
            "anthropic.claude-3-sonnet",
            "cohere.command-r-plus",
            "openai.gpt-3.5-turbo"
        };
        
        return FindModelByPreference(models, preferredModels);
    }

    private ModelInfo? FindBestModelForLongForm(IEnumerable<ModelInfo> models)
    {
        // Prioritize models with large context windows for long-form content
        var modelsWithLargeContext = models
            .Where(m => m.ContextWindow >= 32000)
            .OrderByDescending(m => m.ContextWindow)
            .ToList();
        
        if (modelsWithLargeContext.Any())
        {
            return modelsWithLargeContext.First();
        }
        
        // Fall back to preferred models
        var preferredModels = new[]
        {
            "anthropic.claude-3-opus",
            "anthropic.claude-3-sonnet",
            "openai.gpt-4-turbo",
            "openai.gpt-4",
            "openai.gpt-3.5-turbo"
        };
        
        return FindModelByPreference(models, preferredModels);
    }

    private ModelInfo? FindModelByPreference(IEnumerable<ModelInfo> models, string[] preferredModels)
    {
        foreach (var preferredModel in preferredModels)
        {
            var model = models.FirstOrDefault(m => m.Id == preferredModel);
            if (model != null)
            {
                return model;
            }
        }
        
        return null;
    }
}

/// <summary>
/// Content type for routing
/// </summary>
public enum ContentType
{
    /// <summary>
    /// General content
    /// </summary>
    General,
    
    /// <summary>
    /// Code content
    /// </summary>
    Code,
    
    /// <summary>
    /// Mathematical content
    /// </summary>
    Math,
    
    /// <summary>
    /// Creative writing content
    /// </summary>
    Creative,
    
    /// <summary>
    /// Analytical content
    /// </summary>
    Analytical,
    
    /// <summary>
    /// Long-form content
    /// </summary>
    LongForm
}
