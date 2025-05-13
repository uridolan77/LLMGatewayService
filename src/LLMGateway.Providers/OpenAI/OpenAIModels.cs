using System.Text.Json.Serialization;

namespace LLMGateway.Providers.OpenAI;

/// <summary>
/// Response from the OpenAI list models endpoint
/// </summary>
public class OpenAIListModelsResponse
{
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = string.Empty;
    
    /// <summary>
    /// List of models
    /// </summary>
    public List<OpenAIModel> Data { get; set; } = new();
}

/// <summary>
/// OpenAI model
/// </summary>
public class OpenAIModel
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = string.Empty;
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public long Created { get; set; }
    
    /// <summary>
    /// Owned by
    /// </summary>
    public string OwnedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request for an OpenAI chat completion
/// </summary>
public class OpenAIChatCompletionRequest
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Messages
    /// </summary>
    public List<OpenAIChatMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    public int? MaxTokens { get; set; }
    
    /// <summary>
    /// Temperature
    /// </summary>
    public double? Temperature { get; set; }
    
    /// <summary>
    /// Top-p
    /// </summary>
    public double? TopP { get; set; }
    
    /// <summary>
    /// Number of completions to generate
    /// </summary>
    public int? N { get; set; }
    
    /// <summary>
    /// Whether to stream the response
    /// </summary>
    public bool Stream { get; set; }
    
    /// <summary>
    /// Stop sequences
    /// </summary>
    public List<string>? Stop { get; set; }
    
    /// <summary>
    /// Presence penalty
    /// </summary>
    public double? PresencePenalty { get; set; }
    
    /// <summary>
    /// Frequency penalty
    /// </summary>
    public double? FrequencyPenalty { get; set; }
    
    /// <summary>
    /// Logit bias
    /// </summary>
    public Dictionary<string, double>? LogitBias { get; set; }
    
    /// <summary>
    /// User identifier
    /// </summary>
    public string? User { get; set; }
    
    /// <summary>
    /// Response format
    /// </summary>
    public OpenAIResponseFormat? ResponseFormat { get; set; }
    
    /// <summary>
    /// Tools
    /// </summary>
    public List<OpenAITool>? Tools { get; set; }
    
    /// <summary>
    /// Tool choice
    /// </summary>
    public OpenAIToolChoice? ToolChoice { get; set; }
    
    /// <summary>
    /// Additional parameters
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}

/// <summary>
/// OpenAI chat message
/// </summary>
public class OpenAIChatMessage
{
    /// <summary>
    /// Role
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Content
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    /// Name
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Tool calls
    /// </summary>
    public List<OpenAIToolCall>? ToolCalls { get; set; }
    
    /// <summary>
    /// Tool call ID
    /// </summary>
    public string? ToolCallId { get; set; }
}

/// <summary>
/// OpenAI tool call
/// </summary>
public class OpenAIToolCall
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Function
    /// </summary>
    public OpenAIFunctionCall Function { get; set; } = new();
}

/// <summary>
/// OpenAI function call
/// </summary>
public class OpenAIFunctionCall
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Arguments
    /// </summary>
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// OpenAI response format
/// </summary>
public class OpenAIResponseFormat
{
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// OpenAI tool
/// </summary>
public class OpenAITool
{
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Function
    /// </summary>
    public OpenAIFunctionDefinition Function { get; set; } = new();
}

/// <summary>
/// OpenAI function definition
/// </summary>
public class OpenAIFunctionDefinition
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Parameters
    /// </summary>
    public object? Parameters { get; set; }
}

/// <summary>
/// OpenAI tool choice
/// </summary>
public class OpenAIToolChoice
{
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Function
    /// </summary>
    public OpenAIFunctionChoice? Function { get; set; }
}

/// <summary>
/// OpenAI function choice
/// </summary>
public class OpenAIFunctionChoice
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Response from an OpenAI chat completion
/// </summary>
public class OpenAIChatCompletionResponse
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = string.Empty;
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public long Created { get; set; }
    
    /// <summary>
    /// Model
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Choices
    /// </summary>
    public List<OpenAIChatCompletionChoice> Choices { get; set; } = new();
    
    /// <summary>
    /// Usage
    /// </summary>
    public OpenAIUsage? Usage { get; set; }
    
    /// <summary>
    /// System fingerprint
    /// </summary>
    public string? SystemFingerprint { get; set; }
}

/// <summary>
/// OpenAI chat completion choice
/// </summary>
public class OpenAIChatCompletionChoice
{
    /// <summary>
    /// Index
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Message
    /// </summary>
    public OpenAIChatMessage? Message { get; set; }
    
    /// <summary>
    /// Finish reason
    /// </summary>
    public string? FinishReason { get; set; }
    
    /// <summary>
    /// Delta
    /// </summary>
    public OpenAIChatMessage? Delta { get; set; }
}

/// <summary>
/// OpenAI usage
/// </summary>
public class OpenAIUsage
{
    /// <summary>
    /// Prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Completion tokens
    /// </summary>
    public int CompletionTokens { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
}

/// <summary>
/// Request for an OpenAI embedding
/// </summary>
public class OpenAIEmbeddingRequest
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Input
    /// </summary>
    public object Input { get; set; } = new();
    
    /// <summary>
    /// User identifier
    /// </summary>
    public string? User { get; set; }
    
    /// <summary>
    /// Dimensions
    /// </summary>
    public int? Dimensions { get; set; }
    
    /// <summary>
    /// Encoding format
    /// </summary>
    public string? EncodingFormat { get; set; }
}

/// <summary>
/// Response from an OpenAI embedding
/// </summary>
public class OpenAIEmbeddingResponse
{
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = string.Empty;
    
    /// <summary>
    /// Data
    /// </summary>
    public List<OpenAIEmbeddingData> Data { get; set; } = new();
    
    /// <summary>
    /// Model
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Usage
    /// </summary>
    public OpenAIEmbeddingUsage Usage { get; set; } = new();
}

/// <summary>
/// OpenAI embedding data
/// </summary>
public class OpenAIEmbeddingData
{
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = string.Empty;
    
    /// <summary>
    /// Embedding
    /// </summary>
    public List<float> Embedding { get; set; } = new();
    
    /// <summary>
    /// Index
    /// </summary>
    public int Index { get; set; }
}

/// <summary>
/// OpenAI embedding usage
/// </summary>
public class OpenAIEmbeddingUsage
{
    /// <summary>
    /// Prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
}
