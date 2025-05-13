using System.Text.Json.Serialization;

namespace LLMGateway.Core.Models.Completion;

/// <summary>
/// Response from a completion request
/// </summary>
public class CompletionResponse
{
    /// <summary>
    /// ID of the completion
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = "chat.completion";
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    /// <summary>
    /// Model used for the completion
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider used for the completion
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Choices in the completion
    /// </summary>
    public List<CompletionChoice> Choices { get; set; } = new();
    
    /// <summary>
    /// Usage statistics
    /// </summary>
    public CompletionUsage Usage { get; set; } = new();
    
    /// <summary>
    /// System fingerprint
    /// </summary>
    public string? SystemFingerprint { get; set; }
    
    /// <summary>
    /// Additional provider-specific parameters
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}

/// <summary>
/// Choice in a completion response
/// </summary>
public class CompletionChoice
{
    /// <summary>
    /// Index of the choice
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Message in the choice
    /// </summary>
    public Message Message { get; set; } = new();
    
    /// <summary>
    /// Finish reason
    /// </summary>
    public string? FinishReason { get; set; }
    
    /// <summary>
    /// Delta in a streaming response
    /// </summary>
    public Message? Delta { get; set; }
}

/// <summary>
/// Usage statistics for a completion
/// </summary>
public class CompletionUsage
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
