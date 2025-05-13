using LLMGateway.Core.Models.PromptManagement;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for prompt template service
/// </summary>
public interface IPromptTemplateService
{
    /// <summary>
    /// Get all prompt templates
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of prompt templates</returns>
    Task<IEnumerable<PromptTemplate>> GetAllTemplatesAsync(string userId);
    
    /// <summary>
    /// Get prompt template by ID
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Prompt template</returns>
    Task<PromptTemplate> GetTemplateAsync(string templateId, string userId);
    
    /// <summary>
    /// Create prompt template
    /// </summary>
    /// <param name="request">Template request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Created prompt template</returns>
    Task<PromptTemplate> CreateTemplateAsync(PromptTemplateRequest request, string userId);
    
    /// <summary>
    /// Update prompt template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="request">Update request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Updated prompt template</returns>
    Task<PromptTemplate> UpdateTemplateAsync(string templateId, PromptTemplateUpdateRequest request, string userId);
    
    /// <summary>
    /// Delete prompt template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Task</returns>
    Task DeleteTemplateAsync(string templateId, string userId);
    
    /// <summary>
    /// Render prompt template
    /// </summary>
    /// <param name="request">Render request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Rendered prompt</returns>
    Task<PromptRenderResponse> RenderTemplateAsync(PromptRenderRequest request, string userId);
    
    /// <summary>
    /// Search prompt templates
    /// </summary>
    /// <param name="request">Search request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Search response</returns>
    Task<PromptTemplateSearchResponse> SearchTemplatesAsync(PromptTemplateSearchRequest request, string userId);
    
    /// <summary>
    /// Get template versions
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>List of template versions</returns>
    Task<IEnumerable<PromptTemplate>> GetTemplateVersionsAsync(string templateId, string userId);
    
    /// <summary>
    /// Create template version
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>New template version</returns>
    Task<PromptTemplate> CreateTemplateVersionAsync(string templateId, string userId);
}
