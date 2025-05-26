using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.PromptManagement;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LLMGateway.Core.Services;

/// <summary>
/// In-memory implementation of IPromptTemplateRepository for when database is not available
/// </summary>
public class InMemoryPromptTemplateRepository : IPromptTemplateRepository
{
    private readonly ILogger<InMemoryPromptTemplateRepository> _logger;
    private readonly ConcurrentDictionary<string, PromptTemplate> _templates = new();
    private readonly ConcurrentDictionary<string, List<PromptTemplate>> _versions = new();

    public InMemoryPromptTemplateRepository(ILogger<InMemoryPromptTemplateRepository> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<PromptTemplate>> GetAllAsync(string userId)
    {
        _logger.LogDebug("Getting all prompt templates for user {UserId}", userId);
        
        var templates = _templates.Values
            .Where(t => t.CreatedBy == userId || t.IsPublic)
            .OrderByDescending(t => t.UpdatedAt);
            
        return Task.FromResult(templates.AsEnumerable());
    }

    public Task<PromptTemplate?> GetByIdAsync(string templateId)
    {
        _logger.LogDebug("Getting prompt template {TemplateId}", templateId);
        
        _templates.TryGetValue(templateId, out var template);
        return Task.FromResult(template);
    }

    public Task<PromptTemplate> CreateAsync(PromptTemplate template)
    {
        _logger.LogDebug("Creating prompt template {TemplateId}", template.Id);
        
        template.Id = template.Id ?? Guid.NewGuid().ToString();
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        template.Version = 1;
        
        _templates[template.Id] = template;
        
        // Also store as version
        if (!_versions.ContainsKey(template.Id))
        {
            _versions[template.Id] = new List<PromptTemplate>();
        }
        _versions[template.Id].Add(template);
        
        return Task.FromResult(template);
    }

    public Task<PromptTemplate> UpdateAsync(PromptTemplate template)
    {
        _logger.LogDebug("Updating prompt template {TemplateId}", template.Id);
        
        if (!_templates.ContainsKey(template.Id))
        {
            throw new KeyNotFoundException($"Template {template.Id} not found");
        }
        
        template.UpdatedAt = DateTime.UtcNow;
        template.Version++;
        
        _templates[template.Id] = template;
        
        // Store new version
        if (!_versions.ContainsKey(template.Id))
        {
            _versions[template.Id] = new List<PromptTemplate>();
        }
        _versions[template.Id].Add(template);
        
        return Task.FromResult(template);
    }

    public Task DeleteAsync(string templateId)
    {
        _logger.LogDebug("Deleting prompt template {TemplateId}", templateId);
        
        _templates.TryRemove(templateId, out _);
        _versions.TryRemove(templateId, out _);
        
        return Task.CompletedTask;
    }

    public Task<(IEnumerable<PromptTemplate> Templates, int TotalCount)> SearchAsync(
        string? query,
        IEnumerable<string>? tags,
        string? createdBy,
        bool? publicOnly,
        int page,
        int pageSize)
    {
        _logger.LogDebug("Searching prompt templates with query: {Query}", query);
        
        var queryable = _templates.Values.AsQueryable();
        
        // Apply filters
        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(t =>
                t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Content.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        
        if (tags != null && tags.Any())
        {
            var tagList = tags.ToList();
            queryable = queryable.Where(t => t.Tags.Any(tag => tagList.Contains(tag)));
        }
        
        if (!string.IsNullOrWhiteSpace(createdBy))
        {
            queryable = queryable.Where(t => t.CreatedBy == createdBy);
        }
        
        if (publicOnly.HasValue && publicOnly.Value)
        {
            queryable = queryable.Where(t => t.IsPublic);
        }
        
        var totalCount = queryable.Count();
        
        var templates = queryable
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
            
        return Task.FromResult((templates.AsEnumerable(), totalCount));
    }

    public Task<IEnumerable<PromptTemplate>> GetVersionsAsync(string templateId)
    {
        _logger.LogDebug("Getting versions for prompt template {TemplateId}", templateId);
        
        if (_versions.TryGetValue(templateId, out var versions))
        {
            return Task.FromResult(versions.OrderByDescending(v => v.Version).AsEnumerable());
        }
        
        return Task.FromResult(Enumerable.Empty<PromptTemplate>());
    }
}
