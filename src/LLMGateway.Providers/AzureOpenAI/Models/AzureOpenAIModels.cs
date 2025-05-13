using System.Text.Json.Serialization;

namespace LLMGateway.Providers.AzureOpenAI.Models;

/// <summary>
/// Azure OpenAI chat completion request
/// </summary>
public class AzureOpenAIChatCompletionRequest
{
    /// <summary>
    /// Messages
    /// </summary>
    [JsonPropertyName("messages")]
    public List<AzureOpenAIChatMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
    
    /// <summary>
    /// Temperature
    /// </summary>
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }
    
    /// <summary>
    /// Top P
    /// </summary>
    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }
    
    /// <summary>
    /// Frequency penalty
    /// </summary>
    [JsonPropertyName("frequency_penalty")]
    public float? FrequencyPenalty { get; set; }
    
    /// <summary>
    /// Presence penalty
    /// </summary>
    [JsonPropertyName("presence_penalty")]
    public float? PresencePenalty { get; set; }
    
    /// <summary>
    /// Stop sequences
    /// </summary>
    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }
    
    /// <summary>
    /// Whether to stream the response
    /// </summary>
    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }
    
    /// <summary>
    /// Functions
    /// </summary>
    [JsonPropertyName("functions")]
    public List<AzureOpenAIFunction>? Functions { get; set; }
    
    /// <summary>
    /// Function call
    /// </summary>
    [JsonPropertyName("function_call")]
    public string? FunctionCall { get; set; }
}

/// <summary>
/// Azure OpenAI chat message
/// </summary>
public class AzureOpenAIChatMessage
{
    /// <summary>
    /// Role
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Content
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    /// <summary>
    /// Name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    /// <summary>
    /// Function call
    /// </summary>
    [JsonPropertyName("function_call")]
    public AzureOpenAIFunctionCall? FunctionCall { get; set; }
}

/// <summary>
/// Azure OpenAI function call
/// </summary>
public class AzureOpenAIFunctionCall
{
    /// <summary>
    /// Name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Arguments
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// Azure OpenAI function
/// </summary>
public class AzureOpenAIFunction
{
    /// <summary>
    /// Name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// Parameters
    /// </summary>
    [JsonPropertyName("parameters")]
    public object? Parameters { get; set; }
}

/// <summary>
/// Azure OpenAI chat completion response
/// </summary>
public class AzureOpenAIChatCompletionResponse
{
    /// <summary>
    /// ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Object
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    /// <summary>
    /// Created
    /// </summary>
    [JsonPropertyName("created")]
    public int Created { get; set; }
    
    /// <summary>
    /// Model
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Choices
    /// </summary>
    [JsonPropertyName("choices")]
    public List<AzureOpenAIChatCompletionChoice> Choices { get; set; } = new();
    
    /// <summary>
    /// Usage
    /// </summary>
    [JsonPropertyName("usage")]
    public AzureOpenAIChatCompletionUsage? Usage { get; set; }
}

/// <summary>
/// Azure OpenAI chat completion choice
/// </summary>
public class AzureOpenAIChatCompletionChoice
{
    /// <summary>
    /// Index
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    /// <summary>
    /// Message
    /// </summary>
    [JsonPropertyName("message")]
    public AzureOpenAIChatMessage? Message { get; set; }
    
    /// <summary>
    /// Delta
    /// </summary>
    [JsonPropertyName("delta")]
    public AzureOpenAIChatMessage? Delta { get; set; }
    
    /// <summary>
    /// Finish reason
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// Azure OpenAI chat completion usage
/// </summary>
public class AzureOpenAIChatCompletionUsage
{
    /// <summary>
    /// Prompt tokens
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Completion tokens
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// Azure OpenAI embedding request
/// </summary>
public class AzureOpenAIEmbeddingRequest
{
    /// <summary>
    /// Input
    /// </summary>
    [JsonPropertyName("input")]
    public object Input { get; set; } = new();
    
    /// <summary>
    /// Dimensions
    /// </summary>
    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; }
    
    /// <summary>
    /// User
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }
}

/// <summary>
/// Azure OpenAI embedding response
/// </summary>
public class AzureOpenAIEmbeddingResponse
{
    /// <summary>
    /// Object
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    /// <summary>
    /// Data
    /// </summary>
    [JsonPropertyName("data")]
    public List<AzureOpenAIEmbeddingData> Data { get; set; } = new();
    
    /// <summary>
    /// Model
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Usage
    /// </summary>
    [JsonPropertyName("usage")]
    public AzureOpenAIEmbeddingUsage Usage { get; set; } = new();
}

/// <summary>
/// Azure OpenAI embedding data
/// </summary>
public class AzureOpenAIEmbeddingData
{
    /// <summary>
    /// Object
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    /// <summary>
    /// Embedding
    /// </summary>
    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; } = new();
    
    /// <summary>
    /// Index
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }
}

/// <summary>
/// Azure OpenAI embedding usage
/// </summary>
public class AzureOpenAIEmbeddingUsage
{
    /// <summary>
    /// Prompt tokens
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
