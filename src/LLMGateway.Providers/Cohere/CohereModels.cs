using System.Text.Json.Serialization;

namespace LLMGateway.Providers.Cohere;

/// <summary>
/// Response from the Cohere list models endpoint
/// </summary>
public class CohereListModelsResponse
{
    /// <summary>
    /// List of models
    /// </summary>
    public List<CohereModel> Models { get; set; } = new();
}

/// <summary>
/// Cohere model
/// </summary>
public class CohereModel
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Model name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Model description
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request for a Cohere chat completion
/// </summary>
public class CohereChatRequest
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Chat history
    /// </summary>
    public List<CohereChatMessage>? ChatHistory { get; set; }
    
    /// <summary>
    /// Temperature
    /// </summary>
    public double? Temperature { get; set; }
    
    /// <summary>
    /// Top-p
    /// </summary>
    public double? P { get; set; }
    
    /// <summary>
    /// Top-k
    /// </summary>
    public int? K { get; set; }
    
    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    public int? MaxTokens { get; set; }
    
    /// <summary>
    /// Whether to stream the response
    /// </summary>
    public bool? Stream { get; set; }
    
    /// <summary>
    /// Prompt truncation
    /// </summary>
    public string? PromptTruncation { get; set; }
    
    /// <summary>
    /// Tools
    /// </summary>
    public List<CohereTool>? Tools { get; set; }
    
    /// <summary>
    /// User identifier
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Additional parameters
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}

/// <summary>
/// Cohere chat message
/// </summary>
public class CohereChatMessage
{
    /// <summary>
    /// Role
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Tool calls
    /// </summary>
    public List<CohereToolCall>? ToolCalls { get; set; }
    
    /// <summary>
    /// Tool call ID
    /// </summary>
    public string? ToolCallId { get; set; }
}

/// <summary>
/// Cohere tool call
/// </summary>
public class CohereToolCall
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Cohere tool
/// </summary>
public class CohereTool
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
    /// Parameter schema
    /// </summary>
    public object? ParameterSchema { get; set; }
}

/// <summary>
/// Response from a Cohere chat completion
/// </summary>
public class CohereChatResponse
{
    /// <summary>
    /// Text
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Generation ID
    /// </summary>
    public string GenerationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Finish reason
    /// </summary>
    public string? FinishReason { get; set; }
    
    /// <summary>
    /// Tool calls
    /// </summary>
    public List<CohereToolCall>? ToolCalls { get; set; }
    
    /// <summary>
    /// Token count
    /// </summary>
    public CohereTokenCount? TokenCount { get; set; }
    
    /// <summary>
    /// Meta
    /// </summary>
    public CohereMeta? Meta { get; set; }
}

/// <summary>
/// Cohere token count
/// </summary>
public class CohereTokenCount
{
    /// <summary>
    /// Prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Response tokens
    /// </summary>
    public int ResponseTokens { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
}

/// <summary>
/// Cohere meta
/// </summary>
public class CohereMeta
{
    /// <summary>
    /// API version
    /// </summary>
    public string ApiVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// Billed units
    /// </summary>
    public CohereBilledUnits? BilledUnits { get; set; }
}

/// <summary>
/// Cohere billed units
/// </summary>
public class CohereBilledUnits
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
/// Request for a Cohere embedding
/// </summary>
public class CohereEmbeddingRequest
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Texts to embed
    /// </summary>
    public List<string> Texts { get; set; } = new();
    
    /// <summary>
    /// Input type
    /// </summary>
    public string? InputType { get; set; }
    
    /// <summary>
    /// Truncate
    /// </summary>
    public string? Truncate { get; set; }
}

/// <summary>
/// Response from a Cohere embedding
/// </summary>
public class CohereEmbeddingResponse
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Embeddings
    /// </summary>
    public List<List<float>> Embeddings { get; set; } = new();
    
    /// <summary>
    /// Meta
    /// </summary>
    public CohereMeta? Meta { get; set; }
}
