using System.Text.Json;

namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Conversation entity
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
    /// Metadata (stored as JSON)
    /// </summary>
    public string MetadataJson { get; set; } = "{}";
    
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
    /// Tags (stored as JSON)
    /// </summary>
    public string TagsJson { get; set; } = "[]";
    
    /// <summary>
    /// Navigation property for messages
    /// </summary>
    public virtual ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
    
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
    /// Get tags as list
    /// </summary>
    /// <returns>List of tags</returns>
    public List<string> GetTags()
    {
        if (string.IsNullOrEmpty(TagsJson))
        {
            return new List<string>();
        }
        
        try
        {
            return JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
    
    /// <summary>
    /// Set tags
    /// </summary>
    /// <param name="tags">Tags</param>
    public void SetTags(List<string> tags)
    {
        TagsJson = JsonSerializer.Serialize(tags);
    }
    
    /// <summary>
    /// Convert to domain model
    /// </summary>
    /// <returns>Domain model</returns>
    public Core.Models.Conversation.Conversation ToDomainModel()
    {
        return new Core.Models.Conversation.Conversation
        {
            Id = Id,
            Title = Title,
            UserId = UserId,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            Metadata = GetMetadata(),
            ModelId = ModelId,
            SystemPrompt = SystemPrompt,
            IsArchived = IsArchived,
            Tags = GetTags(),
            Messages = Messages.Select(m => m.ToDomainModel()).ToList()
        };
    }
    
    /// <summary>
    /// Create from domain model
    /// </summary>
    /// <param name="model">Domain model</param>
    /// <returns>Entity</returns>
    public static Conversation FromDomainModel(Core.Models.Conversation.Conversation model)
    {
        var entity = new Conversation
        {
            Id = model.Id,
            Title = model.Title,
            UserId = model.UserId,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            ModelId = model.ModelId,
            SystemPrompt = model.SystemPrompt,
            IsArchived = model.IsArchived
        };
        
        entity.SetMetadata(model.Metadata);
        entity.SetTags(model.Tags);
        
        return entity;
    }
}
