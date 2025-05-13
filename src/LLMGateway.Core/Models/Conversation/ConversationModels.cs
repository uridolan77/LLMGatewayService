using LLMGateway.Core.Models.Completion;

namespace LLMGateway.Core.Models.Conversation;

/// <summary>
/// Conversation
/// </summary>
public class Conversation
{
    /// <summary>
    /// Conversation ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Conversation title
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Conversation messages
    /// </summary>
    public List<ConversationMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// Conversation metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// System prompt
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the conversation is archived
    /// </summary>
    public bool IsArchived { get; set; } = false;
    
    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Conversation message
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// Message ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Conversation ID
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Message role
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Message content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Token usage
    /// </summary>
    public MessageTokenUsage? TokenUsage { get; set; }
    
    /// <summary>
    /// Function call
    /// </summary>
    public FunctionCall? FunctionCall { get; set; }
    
    /// <summary>
    /// Message metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Message token usage
/// </summary>
public class MessageTokenUsage
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
/// Create conversation request
/// </summary>
public class CreateConversationRequest
{
    /// <summary>
    /// Conversation title
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// System prompt
    /// </summary>
    public string? SystemPrompt { get; set; }
    
    /// <summary>
    /// Initial message
    /// </summary>
    public string? InitialMessage { get; set; }
    
    /// <summary>
    /// Conversation metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// Tags
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Update conversation request
/// </summary>
public class UpdateConversationRequest
{
    /// <summary>
    /// Conversation title
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// System prompt
    /// </summary>
    public string? SystemPrompt { get; set; }
    
    /// <summary>
    /// Conversation metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// Whether the conversation is archived
    /// </summary>
    public bool? IsArchived { get; set; }
    
    /// <summary>
    /// Tags
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Add message request
/// </summary>
public class AddMessageRequest
{
    /// <summary>
    /// Message role
    /// </summary>
    public string Role { get; set; } = "user";
    
    /// <summary>
    /// Message content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Message metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Conversation search request
/// </summary>
public class ConversationSearchRequest
{
    /// <summary>
    /// Search query
    /// </summary>
    public string? Query { get; set; }
    
    /// <summary>
    /// Filter by tags
    /// </summary>
    public List<string>? Tags { get; set; }
    
    /// <summary>
    /// Include archived conversations
    /// </summary>
    public bool IncludeArchived { get; set; } = false;
    
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Page number
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Conversation search response
/// </summary>
public class ConversationSearchResponse
{
    /// <summary>
    /// Conversations
    /// </summary>
    public List<Conversation> Conversations { get; set; } = new();
    
    /// <summary>
    /// Total count
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Page number
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Total pages
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// Continue conversation request
/// </summary>
public class ContinueConversationRequest
{
    /// <summary>
    /// Conversation ID
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;
    
    /// <summary>
    /// User message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Model ID (optional, will use the conversation's model if not specified)
    /// </summary>
    public string? ModelId { get; set; }
    
    /// <summary>
    /// Temperature
    /// </summary>
    public float? Temperature { get; set; }
    
    /// <summary>
    /// Max tokens
    /// </summary>
    public int? MaxTokens { get; set; }
    
    /// <summary>
    /// Message metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
