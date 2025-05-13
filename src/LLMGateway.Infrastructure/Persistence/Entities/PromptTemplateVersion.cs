using System.Text.Json;

namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Prompt template version entity
/// </summary>
public class PromptTemplateVersion
{
    /// <summary>
    /// Version ID
    /// </summary>
    public int VersionId { get; set; }
    
    /// <summary>
    /// Template ID
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;
    
    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Template description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Template content with variable placeholders
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Template version
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Template tags (stored as JSON)
    /// </summary>
    public string TagsJson { get; set; } = "[]";
    
    /// <summary>
    /// Template variables (stored as JSON)
    /// </summary>
    public string VariablesJson { get; set; } = "[]";
    
    /// <summary>
    /// User ID of the creator
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether the template is public
    /// </summary>
    public bool IsPublic { get; set; } = false;
    
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
    /// Get variables
    /// </summary>
    /// <returns>List of variables</returns>
    public List<Core.Models.PromptManagement.PromptVariable> GetVariables()
    {
        if (string.IsNullOrEmpty(VariablesJson))
        {
            return new List<Core.Models.PromptManagement.PromptVariable>();
        }
        
        try
        {
            return JsonSerializer.Deserialize<List<Core.Models.PromptManagement.PromptVariable>>(VariablesJson) 
                ?? new List<Core.Models.PromptManagement.PromptVariable>();
        }
        catch
        {
            return new List<Core.Models.PromptManagement.PromptVariable>();
        }
    }
    
    /// <summary>
    /// Set variables
    /// </summary>
    /// <param name="variables">Variables</param>
    public void SetVariables(List<Core.Models.PromptManagement.PromptVariable> variables)
    {
        VariablesJson = JsonSerializer.Serialize(variables);
    }
    
    /// <summary>
    /// Convert to domain model
    /// </summary>
    /// <returns>Domain model</returns>
    public Core.Models.PromptManagement.PromptTemplate ToDomainModel()
    {
        return new Core.Models.PromptManagement.PromptTemplate
        {
            Id = TemplateId,
            Name = Name,
            Description = Description,
            Content = Content,
            Version = Version,
            Tags = GetTags(),
            Variables = GetVariables(),
            CreatedBy = CreatedBy,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            IsPublic = IsPublic
        };
    }
    
    /// <summary>
    /// Create from domain model
    /// </summary>
    /// <param name="model">Domain model</param>
    /// <returns>Entity</returns>
    public static PromptTemplateVersion FromDomainModel(Core.Models.PromptManagement.PromptTemplate model)
    {
        var entity = new PromptTemplateVersion
        {
            TemplateId = model.Id,
            Name = model.Name,
            Description = model.Description,
            Content = model.Content,
            Version = model.Version,
            CreatedBy = model.CreatedBy,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            IsPublic = model.IsPublic
        };
        
        entity.SetTags(model.Tags);
        entity.SetVariables(model.Variables);
        
        return entity;
    }
}
