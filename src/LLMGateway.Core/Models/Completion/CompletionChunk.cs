using System.Text.Json.Serialization;

namespace LLMGateway.Core.Models.Completion;

/// <summary>
/// Chunk of a streaming completion response
/// </summary>
public class CompletionChunk
{
    /// <summary>
    /// ID of the completion
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; } = "chat.completion.chunk";
    
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
    public List<ChunkChoice> Choices { get; set; } = new();
}

/// <summary>
/// Choice in a streaming completion response
/// </summary>
public class ChunkChoice
{
    /// <summary>
    /// Index of the choice
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Delta in the choice
    /// </summary>
    public DeltaMessage Delta { get; set; } = new();
    
    /// <summary>
    /// Finish reason
    /// </summary>
    public string? FinishReason { get; set; }
}

/// <summary>
/// Delta message in a streaming completion response
/// </summary>
public class DeltaMessage
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
    /// Tool calls in the message
    /// </summary>
    public List<ToolCall>? ToolCalls { get; set; }
}
