using System.Text.Json;

namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Cost record entity
/// </summary>
public class CostRecord
{
    /// <summary>
    /// Record ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Request ID
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Operation type
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Input tokens
    /// </summary>
    public int InputTokens { get; set; }
    
    /// <summary>
    /// Output tokens
    /// </summary>
    public int OutputTokens { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
    
    /// <summary>
    /// Cost in USD
    /// </summary>
    public decimal CostUsd { get; set; }
    
    /// <summary>
    /// Project ID
    /// </summary>
    public string? ProjectId { get; set; }
    
    /// <summary>
    /// Tags (stored as JSON)
    /// </summary>
    public string TagsJson { get; set; } = "[]";
    
    /// <summary>
    /// Metadata (stored as JSON)
    /// </summary>
    public string MetadataJson { get; set; } = "{}";
    
    /// <summary>
    /// Get tags
    /// </summary>
    /// <returns>Tags</returns>
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
    /// Get metadata
    /// </summary>
    /// <returns>Metadata</returns>
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
    /// Convert to domain model
    /// </summary>
    /// <returns>Domain model</returns>
    public Core.Models.Cost.CostRecord ToDomainModel()
    {
        return new Core.Models.Cost.CostRecord
        {
            Id = Id,
            RequestId = RequestId,
            UserId = UserId,
            Provider = Provider,
            ModelId = ModelId,
            OperationType = OperationType,
            Timestamp = Timestamp,
            InputTokens = InputTokens,
            OutputTokens = OutputTokens,
            TotalTokens = TotalTokens,
            CostUsd = CostUsd,
            ProjectId = ProjectId,
            Tags = GetTags(),
            Metadata = GetMetadata()
        };
    }
    
    /// <summary>
    /// Create from domain model
    /// </summary>
    /// <param name="model">Domain model</param>
    /// <returns>Entity</returns>
    public static CostRecord FromDomainModel(Core.Models.Cost.CostRecord model)
    {
        var entity = new CostRecord
        {
            Id = model.Id,
            RequestId = model.RequestId,
            UserId = model.UserId,
            Provider = model.Provider,
            ModelId = model.ModelId,
            OperationType = model.OperationType,
            Timestamp = model.Timestamp,
            InputTokens = model.InputTokens,
            OutputTokens = model.OutputTokens,
            TotalTokens = model.TotalTokens,
            CostUsd = model.CostUsd,
            ProjectId = model.ProjectId
        };
        
        entity.SetTags(model.Tags);
        entity.SetMetadata(model.Metadata);
        
        return entity;
    }
}
