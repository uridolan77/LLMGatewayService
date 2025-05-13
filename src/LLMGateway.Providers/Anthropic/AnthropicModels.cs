using System.Text.Json.Serialization;

namespace LLMGateway.Providers.Anthropic;

/// <summary>
/// Response from the Anthropic list models endpoint
/// </summary>
public class AnthropicListModelsResponse
{
    /// <summary>
    /// List of models
    /// </summary>
    public List<AnthropicModel> Models { get; set; } = new();
}

/// <summary>
/// Anthropic model
/// </summary>
public class AnthropicModel
{
    /// <summary>
    /// Model name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Model description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Maximum context window
    /// </summary>
    public int ContextWindow { get; set; }
}

/// <summary>
/// Request for an Anthropic message
/// </summary>
public class AnthropicMessageRequest
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Messages
    /// </summary>
    public List<AnthropicMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// System prompt
    /// </summary>
    public string? System { get; set; }
    
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
    /// Whether to stream the response
    /// </summary>
    public bool Stream { get; set; }
    
    /// <summary>
    /// Stop sequences
    /// </summary>
    public List<string>? StopSequences { get; set; }
    
    /// <summary>
    /// Tools
    /// </summary>
    public List<AnthropicTool>? Tools { get; set; }
    
    /// <summary>
    /// Tool choice
    /// </summary>
    public AnthropicToolChoice? ToolChoice { get; set; }
    
    /// <summary>
    /// Additional parameters
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}

/// <summary>
/// Anthropic message
/// </summary>
public class AnthropicMessage
{
    /// <summary>
    /// Role
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Content
    /// </summary>
    public object Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Tool calls
    /// </summary>
    public List<AnthropicToolCall>? ToolCalls { get; set; }
}

/// <summary>
/// Anthropic content block
/// </summary>
public class AnthropicContentBlock
{
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = "text";
    
    /// <summary>
    /// Text
    /// </summary>
    public string? Text { get; set; }
}

/// <summary>
/// Anthropic tool call
/// </summary>
public class AnthropicToolCall
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = "function";
    
    /// <summary>
    /// Function
    /// </summary>
    public AnthropicFunctionCall Function { get; set; } = new();
}

/// <summary>
/// Anthropic function call
/// </summary>
public class AnthropicFunctionCall
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
/// Anthropic tool
/// </summary>
public class AnthropicTool
{
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = "function";
    
    /// <summary>
    /// Function
    /// </summary>
    public AnthropicFunctionDefinition Function { get; set; } = new();
}

/// <summary>
/// Anthropic function definition
/// </summary>
public class AnthropicFunctionDefinition
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
/// Anthropic tool choice
/// </summary>
public class AnthropicToolChoice
{
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = "auto";
    
    /// <summary>
    /// Function
    /// </summary>
    public AnthropicFunctionChoice? Function { get; set; }
}

/// <summary>
/// Anthropic function choice
/// </summary>
public class AnthropicFunctionChoice
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Response from an Anthropic message
/// </summary>
public class AnthropicMessageResponse
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = "message";
    
    /// <summary>
    /// Model
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Role
    /// </summary>
    public string Role { get; set; } = "assistant";
    
    /// <summary>
    /// Content
    /// </summary>
    public List<AnthropicContentBlock> Content { get; set; } = new();
    
    /// <summary>
    /// Stop reason
    /// </summary>
    public string? StopReason { get; set; }
    
    /// <summary>
    /// Stop sequence
    /// </summary>
    public string? StopSequence { get; set; }
    
    /// <summary>
    /// Usage
    /// </summary>
    public AnthropicUsage Usage { get; set; } = new();
    
    /// <summary>
    /// Tool calls
    /// </summary>
    public List<AnthropicToolCall>? ToolCalls { get; set; }
    
    /// <summary>
    /// Delta in a streaming response
    /// </summary>
    public AnthropicDelta? Delta { get; set; }
}

/// <summary>
/// Anthropic delta for streaming
/// </summary>
public class AnthropicDelta
{
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = "message_delta";
    
    /// <summary>
    /// Text
    /// </summary>
    public string? Text { get; set; }
    
    /// <summary>
    /// Stop reason
    /// </summary>
    public string? StopReason { get; set; }
    
    /// <summary>
    /// Tool calls
    /// </summary>
    public List<AnthropicToolCall>? ToolCalls { get; set; }
}

/// <summary>
/// Anthropic usage
/// </summary>
public class AnthropicUsage
{
    /// <summary>
    /// Input tokens
    /// </summary>
    public int InputTokens { get; set; }
    
    /// <summary>
    /// Output tokens
    /// </summary>
    public int OutputTokens { get; set; }
}

/// <summary>
/// Request for an Anthropic embedding
/// </summary>
public class AnthropicEmbeddingRequest
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Input text
    /// </summary>
    public object Input { get; set; } = new();
    
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
/// Response from an Anthropic embedding
/// </summary>
public class AnthropicEmbeddingResponse
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = "list";
    
    /// <summary>
    /// Embedding data
    /// </summary>
    public List<AnthropicEmbeddingData> Data { get; set; } = new();
    
    /// <summary>
    /// Model
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Usage
    /// </summary>
    public AnthropicEmbeddingUsage Usage { get; set; } = new();
}

/// <summary>
/// Anthropic embedding data
/// </summary>
public class AnthropicEmbeddingData
{
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = "embedding";
    
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
/// Anthropic embedding usage
/// </summary>
public class AnthropicEmbeddingUsage
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
