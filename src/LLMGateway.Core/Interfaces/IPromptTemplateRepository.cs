using LLMGateway.Core.Models.PromptManagement;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for prompt template repository
/// </summary>
public interface IPromptTemplateRepository
{
    /// <summary>
    /// Get all prompt templates
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of prompt templates</returns>
    Task<IEnumerable<PromptTemplate>> GetAllAsync(string userId);
    
    /// <summary>
    /// Get prompt template by ID
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>Prompt template</returns>
    Task<PromptTemplate?> GetByIdAsync(string templateId);
    
    /// <summary>
    /// Create prompt template
    /// </summary>
    /// <param name="template">Template to create</param>
    /// <returns>Created template</returns>
    Task<PromptTemplate> CreateAsync(PromptTemplate template);
    
    /// <summary>
    /// Update prompt template
    /// </summary>
    /// <param name="template">Template to update</param>
    /// <returns>Updated template</returns>
    Task<PromptTemplate> UpdateAsync(PromptTemplate template);
    
    /// <summary>
    /// Delete prompt template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>Task</returns>
    Task DeleteAsync(string templateId);
    
    /// <summary>
    /// Search prompt templates
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="tags">Filter by tags</param>
    /// <param name="createdBy">Filter by creator</param>
    /// <param name="publicOnly">Include only public templates</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Search results</returns>
    Task<(IEnumerable<PromptTemplate> Templates, int TotalCount)> SearchAsync(
        string? query,
        IEnumerable<string>? tags,
        string? createdBy,
        bool? publicOnly,
        int page,
        int pageSize);
    
    /// <summary>
    /// Get template versions
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>List of template versions</returns>
    Task<IEnumerable<PromptTemplate>> GetVersionsAsync(string templateId);
}
