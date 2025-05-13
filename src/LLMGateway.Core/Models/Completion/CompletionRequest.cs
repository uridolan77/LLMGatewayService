using System.Text.Json.Serialization;

namespace LLMGateway.Core.Models.Completion;

/// <summary>
/// Request for a completion
/// </summary>
public class CompletionRequest
{
    /// <summary>
    /// Model ID to use for the completion
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Messages to include in the completion
    /// </summary>
    public List<Message> Messages { get; set; } = new();

    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Temperature for sampling
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Top-p for nucleus sampling
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// Number of completions to generate
    /// </summary>
    public int? N { get; set; }

    /// <summary>
    /// Whether to stream the response
    /// </summary>
    public bool Stream { get; set; } = false;

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
    /// Project identifier
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Tags for cost tracking and analytics
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Response format
    /// </summary>
    public ResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// Tools available to the model
    /// </summary>
    public List<Tool>? Tools { get; set; }

    /// <summary>
    /// Tool choice
    /// </summary>
    public ToolChoice? ToolChoice { get; set; }

    /// <summary>
    /// Additional provider-specific parameters
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}

/// <summary>
/// Message in a completion request
/// </summary>
public class Message
{
    /// <summary>
    /// Role of the message sender
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Content of the message
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Name of the message sender
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Tool calls in the message
    /// </summary>
    public List<ToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Tool call ID that this message is responding to
    /// </summary>
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Function call in the message (legacy format)
    /// </summary>
    public FunctionCall? FunctionCall { get; set; }

    /// <summary>
    /// Additional provider-specific parameters
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalParameters { get; set; }
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
/// Function call in a tool call
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
/// Tool available to the model
/// </summary>
public class Tool
{
    /// <summary>
    /// Type of the tool
    /// </summary>
    public string Type { get; set; } = "function";

    /// <summary>
    /// Function definition
    /// </summary>
    public FunctionDefinition Function { get; set; } = new();
}

/// <summary>
/// Function definition in a tool
/// </summary>
public class FunctionDefinition
{
    /// <summary>
    /// Name of the function
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the function
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Parameters of the function
    /// </summary>
    public object? Parameters { get; set; }
}

/// <summary>
/// Tool choice
/// </summary>
public class ToolChoice
{
    /// <summary>
    /// Type of the tool choice
    /// </summary>
    public string Type { get; set; } = "auto";

    /// <summary>
    /// Function to use
    /// </summary>
    public FunctionChoice? Function { get; set; }
}

/// <summary>
/// Function choice
/// </summary>
public class FunctionChoice
{
    /// <summary>
    /// Name of the function
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Response format
/// </summary>
public class ResponseFormat
{
    /// <summary>
    /// Type of the response format
    /// </summary>
    public string Type { get; set; } = "text";
}
