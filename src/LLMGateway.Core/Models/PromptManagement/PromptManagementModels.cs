using System.Text.Json.Serialization;

namespace LLMGateway.Core.Models.PromptManagement;

/// <summary>
/// Prompt template
/// </summary>
public class PromptTemplate
{
    /// <summary>
    /// Template ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
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
    /// Template tags
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Template variables
    /// </summary>
    public List<PromptVariable> Variables { get; set; } = new();
    
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
}

/// <summary>
/// Prompt variable
/// </summary>
public class PromptVariable
{
    /// <summary>
    /// Variable name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Variable description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Default value
    /// </summary>
    public string? DefaultValue { get; set; }
    
    /// <summary>
    /// Whether the variable is required
    /// </summary>
    public bool Required { get; set; } = true;
    
    /// <summary>
    /// Variable type
    /// </summary>
    public PromptVariableType Type { get; set; } = PromptVariableType.String;
    
    /// <summary>
    /// Possible values for enum type
    /// </summary>
    public List<string>? EnumValues { get; set; }
}

/// <summary>
/// Prompt variable type
/// </summary>
public enum PromptVariableType
{
    /// <summary>
    /// String type
    /// </summary>
    String,
    
    /// <summary>
    /// Number type
    /// </summary>
    Number,
    
    /// <summary>
    /// Boolean type
    /// </summary>
    Boolean,
    
    /// <summary>
    /// Enum type
    /// </summary>
    Enum,
    
    /// <summary>
    /// Date type
    /// </summary>
    Date
}

/// <summary>
/// Prompt template request
/// </summary>
public class PromptTemplateRequest
{
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
    /// Template tags
    /// </summary>
    public List<string>? Tags { get; set; }
    
    /// <summary>
    /// Template variables
    /// </summary>
    public List<PromptVariable>? Variables { get; set; }
    
    /// <summary>
    /// Whether the template is public
    /// </summary>
    public bool IsPublic { get; set; } = false;
}

/// <summary>
/// Prompt template update request
/// </summary>
public class PromptTemplateUpdateRequest
{
    /// <summary>
    /// Template name
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Template content with variable placeholders
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    /// Template tags
    /// </summary>
    public List<string>? Tags { get; set; }
    
    /// <summary>
    /// Template variables
    /// </summary>
    public List<PromptVariable>? Variables { get; set; }
    
    /// <summary>
    /// Whether the template is public
    /// </summary>
    public bool? IsPublic { get; set; }
}

/// <summary>
/// Prompt render request
/// </summary>
public class PromptRenderRequest
{
    /// <summary>
    /// Template ID
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;
    
    /// <summary>
    /// Variables for rendering
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();
}

/// <summary>
/// Prompt render response
/// </summary>
public class PromptRenderResponse
{
    /// <summary>
    /// Rendered prompt
    /// </summary>
    public string RenderedPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// Template ID
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;
    
    /// <summary>
    /// Template name
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;
    
    /// <summary>
    /// Variables used for rendering
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();
}

/// <summary>
/// Prompt template search request
/// </summary>
public class PromptTemplateSearchRequest
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
    /// Filter by creator
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Include only public templates
    /// </summary>
    public bool? PublicOnly { get; set; }
    
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
/// Prompt template search response
/// </summary>
public class PromptTemplateSearchResponse
{
    /// <summary>
    /// Templates
    /// </summary>
    public List<PromptTemplate> Templates { get; set; } = new();
    
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
