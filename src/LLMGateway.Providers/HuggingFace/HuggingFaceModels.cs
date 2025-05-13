using System.Text.Json.Serialization;

namespace LLMGateway.Providers.HuggingFace;

/// <summary>
/// Response from the HuggingFace list models endpoint
/// </summary>
public class HuggingFaceListModelsResponse
{
    /// <summary>
    /// List of models
    /// </summary>
    public List<HuggingFaceModel> Models { get; set; } = new();
}

/// <summary>
/// HuggingFace model
/// </summary>
public class HuggingFaceModel
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Pipeline tag
    /// </summary>
    public string? PipelineTag { get; set; }
}

/// <summary>
/// Request for a HuggingFace text generation
/// </summary>
public class HuggingFaceTextGenerationRequest
{
    /// <summary>
    /// Input text
    /// </summary>
    public string Inputs { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameters
    /// </summary>
    public HuggingFaceTextGenerationParameters? Parameters { get; set; }
    
    /// <summary>
    /// Options
    /// </summary>
    public HuggingFaceTextGenerationOptions? Options { get; set; }
}

/// <summary>
/// Parameters for a HuggingFace text generation
/// </summary>
public class HuggingFaceTextGenerationParameters
{
    /// <summary>
    /// Temperature
    /// </summary>
    public double? Temperature { get; set; }
    
    /// <summary>
    /// Top-p
    /// </summary>
    public double? TopP { get; set; }
    
    /// <summary>
    /// Top-k
    /// </summary>
    public int? TopK { get; set; }
    
    /// <summary>
    /// Maximum new tokens
    /// </summary>
    public int? MaxNewTokens { get; set; }
    
    /// <summary>
    /// Repetition penalty
    /// </summary>
    public double? RepetitionPenalty { get; set; }
    
    /// <summary>
    /// Maximum length
    /// </summary>
    public int? MaxLength { get; set; }
    
    /// <summary>
    /// Return full text
    /// </summary>
    public bool? ReturnFullText { get; set; }
    
    /// <summary>
    /// Number of return sequences
    /// </summary>
    public int? NumReturnSequences { get; set; }
    
    /// <summary>
    /// Do sample
    /// </summary>
    public bool? DoSample { get; set; }
}

/// <summary>
/// Options for a HuggingFace text generation
/// </summary>
public class HuggingFaceTextGenerationOptions
{
    /// <summary>
    /// Whether to use cache
    /// </summary>
    public bool? UseCache { get; set; }
    
    /// <summary>
    /// Whether to wait for model
    /// </summary>
    public bool? WaitForModel { get; set; }
}

/// <summary>
/// Response from a HuggingFace text generation
/// </summary>
public class HuggingFaceTextGenerationResponse
{
    /// <summary>
    /// Generated text
    /// </summary>
    public string? GeneratedText { get; set; }
    
    /// <summary>
    /// Generated tokens
    /// </summary>
    public int? GeneratedTokens { get; set; }
    
    /// <summary>
    /// Details
    /// </summary>
    public HuggingFaceTextGenerationDetails? Details { get; set; }
}

/// <summary>
/// Details for a HuggingFace text generation
/// </summary>
public class HuggingFaceTextGenerationDetails
{
    /// <summary>
    /// Finish reason
    /// </summary>
    public string? FinishReason { get; set; }
    
    /// <summary>
    /// Prompt tokens
    /// </summary>
    public int? PromptTokens { get; set; }
    
    /// <summary>
    /// Generated tokens
    /// </summary>
    public int? GeneratedTokens { get; set; }
}

/// <summary>
/// Request for a HuggingFace chat completion
/// </summary>
public class HuggingFaceChatCompletionRequest
{
    /// <summary>
    /// Input messages
    /// </summary>
    public List<HuggingFaceChatMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// Parameters
    /// </summary>
    public HuggingFaceTextGenerationParameters? Parameters { get; set; }
    
    /// <summary>
    /// Options
    /// </summary>
    public HuggingFaceTextGenerationOptions? Options { get; set; }
}

/// <summary>
/// HuggingFace chat message
/// </summary>
public class HuggingFaceChatMessage
{
    /// <summary>
    /// Role
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Content
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response from a HuggingFace chat completion
/// </summary>
public class HuggingFaceChatCompletionResponse
{
    /// <summary>
    /// Generated text
    /// </summary>
    public string? GeneratedText { get; set; }
    
    /// <summary>
    /// Generated tokens
    /// </summary>
    public int? GeneratedTokens { get; set; }
    
    /// <summary>
    /// Details
    /// </summary>
    public HuggingFaceTextGenerationDetails? Details { get; set; }
}

/// <summary>
/// Request for a HuggingFace feature extraction (embedding)
/// </summary>
public class HuggingFaceFeatureExtractionRequest
{
    /// <summary>
    /// Input text
    /// </summary>
    public object Inputs { get; set; } = new();
    
    /// <summary>
    /// Options
    /// </summary>
    public HuggingFaceFeatureExtractionOptions? Options { get; set; }
}

/// <summary>
/// Options for a HuggingFace feature extraction
/// </summary>
public class HuggingFaceFeatureExtractionOptions
{
    /// <summary>
    /// Whether to use cache
    /// </summary>
    public bool? UseCache { get; set; }
    
    /// <summary>
    /// Whether to wait for model
    /// </summary>
    public bool? WaitForModel { get; set; }
}
