using System.Text.Json;
using LLMGateway.Core.Models.Completion;

namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Conversation message entity
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
    /// Prompt tokens
    /// </summary>
    public int? PromptTokens { get; set; }
    
    /// <summary>
    /// Completion tokens
    /// </summary>
    public int? CompletionTokens { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int? TotalTokens { get; set; }
    
    /// <summary>
    /// Function call (stored as JSON)
    /// </summary>
    public string? FunctionCallJson { get; set; }
    
    /// <summary>
    /// Metadata (stored as JSON)
    /// </summary>
    public string MetadataJson { get; set; } = "{}";
    
    /// <summary>
    /// Navigation property for conversation
    /// </summary>
    public virtual Conversation Conversation { get; set; } = null!;
    
    /// <summary>
    /// Get metadata as dictionary
    /// </summary>
    /// <returns>Metadata dictionary</returns>
    public Dictionary<string, string> GetMetadata()
    {
        if (string.IsNullOrEmpty(MetadataJson))
        {
            return new Dictionary<string, string>();
        }
        
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson) ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
    
    /// <summary>
    /// Set metadata
    /// </summary>
    /// <param name="metadata">Metadata</param>
    public void SetMetadata(Dictionary<string, string> metadata)
    {
        MetadataJson = JsonSerializer.Serialize(metadata);
    }
    
    /// <summary>
    /// Get function call
    /// </summary>
    /// <returns>Function call</returns>
    public FunctionCall? GetFunctionCall()
    {
        if (string.IsNullOrEmpty(FunctionCallJson))
        {
            return null;
        }
        
        try
        {
            return JsonSerializer.Deserialize<FunctionCall>(FunctionCallJson);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Set function call
    /// </summary>
    /// <param name="functionCall">Function call</param>
    public void SetFunctionCall(FunctionCall? functionCall)
    {
        FunctionCallJson = functionCall != null ? JsonSerializer.Serialize(functionCall) : null;
    }
    
    /// <summary>
    /// Convert to domain model
    /// </summary>
    /// <returns>Domain model</returns>
    public Core.Models.Conversation.ConversationMessage ToDomainModel()
    {
        return new Core.Models.Conversation.ConversationMessage
        {
            Id = Id,
            ConversationId = ConversationId,
            Role = Role,
            Content = Content,
            CreatedAt = CreatedAt,
            ModelId = ModelId,
            Provider = Provider,
            TokenUsage = PromptTokens.HasValue || CompletionTokens.HasValue || TotalTokens.HasValue
                ? new Core.Models.Conversation.MessageTokenUsage
                {
                    PromptTokens = PromptTokens ?? 0,
                    CompletionTokens = CompletionTokens ?? 0,
                    TotalTokens = TotalTokens ?? 0
                }
                : null,
            FunctionCall = GetFunctionCall(),
            Metadata = GetMetadata()
        };
    }
    
    /// <summary>
    /// Create from domain model
    /// </summary>
    /// <param name="model">Domain model</param>
    /// <returns>Entity</returns>
    public static ConversationMessage FromDomainModel(Core.Models.Conversation.ConversationMessage model)
    {
        var entity = new ConversationMessage
        {
            Id = model.Id,
            ConversationId = model.ConversationId,
            Role = model.Role,
            Content = model.Content,
            CreatedAt = model.CreatedAt,
            ModelId = model.ModelId,
            Provider = model.Provider,
            PromptTokens = model.TokenUsage?.PromptTokens,
            CompletionTokens = model.TokenUsage?.CompletionTokens,
            TotalTokens = model.TokenUsage?.TotalTokens
        };
        
        entity.SetFunctionCall(model.FunctionCall);
        entity.SetMetadata(model.Metadata);
        
        return entity;
    }
}
