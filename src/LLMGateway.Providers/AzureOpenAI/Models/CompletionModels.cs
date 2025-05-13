using System.Text.Json.Serialization;

namespace LLMGateway.Providers.AzureOpenAI.Models;

/// <summary>
/// Choice in a completion response
/// </summary>
public class Choice
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
    public Delta? Delta { get; set; }
}

/// <summary>
/// Message in a completion response
/// </summary>
public class Message
{
    /// <summary>
    /// Role of the message sender
    /// </summary>
    public string Role { get; set; } = "assistant";
    
    /// <summary>
    /// Content of the message
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    /// Name of the message sender
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Function call in the message
    /// </summary>
    public FunctionCall? FunctionCall { get; set; }
    
    /// <summary>
    /// Tool calls in the message
    /// </summary>
    public List<ToolCall>? ToolCalls { get; set; }
}

/// <summary>
/// Delta in a streaming response
/// </summary>
public class Delta
{
    /// <summary>
    /// Role of the message sender
    /// </summary>
    public string? Role { get; set; }
    
    /// <summary>
    /// Content of the message
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    /// Function call in the message
    /// </summary>
    public FunctionCall? FunctionCall { get; set; }
    
    /// <summary>
    /// Tool calls in the message
    /// </summary>
    public List<ToolCall>? ToolCalls { get; set; }
}

/// <summary>
/// Function call in a message
/// </summary>
public class FunctionCall
{
    /// <summary>
    /// Name of the function
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Arguments to the function
    /// </summary>
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// Tool call in a message
/// </summary>
public class ToolCall
{
    /// <summary>
    /// ID of the tool call
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of the tool call
    /// </summary>
    public string Type { get; set; } = "function";
    
    /// <summary>
    /// Function call
    /// </summary>
    public FunctionCall Function { get; set; } = new();
}

/// <summary>
/// Usage information in a completion response
/// </summary>
public class Usage
{
    /// <summary>
    /// Number of prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Number of completion tokens
    /// </summary>
    public int CompletionTokens { get; set; }
    
    /// <summary>
    /// Total number of tokens
    /// </summary>
    public int TotalTokens { get; set; }
}
