using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.PromptManagement;
using LLMGateway.Infrastructure.Persistence;
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Repositories;

/// <summary>
/// Repository for prompt templates
/// </summary>
public class PromptTemplateRepository : IPromptTemplateRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<PromptTemplateRepository> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger</param>
    public PromptTemplateRepository(
        LLMGatewayDbContext dbContext,
        ILogger<PromptTemplateRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.PromptManagement.PromptTemplate>> GetAllAsync(string userId)
    {
        try
        {
            var entities = await _dbContext.PromptTemplates
                .Where(t => t.CreatedBy == userId || t.IsPublic)
                .OrderByDescending(t => t.UpdatedAt)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all prompt templates for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.PromptManagement.PromptTemplate?> GetByIdAsync(string templateId)
    {
        try
        {
            var entity = await _dbContext.PromptTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId);

            return entity?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get prompt template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.PromptManagement.PromptTemplate> CreateAsync(Core.Models.PromptManagement.PromptTemplate template)
    {
        try
        {
            var entity = Persistence.Entities.PromptTemplate.FromDomainModel(template);

            _dbContext.PromptTemplates.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Also create a version record
            var versionEntity = Persistence.Entities.PromptTemplateVersion.FromDomainModel(template);
            _dbContext.PromptTemplateVersions.Add(versionEntity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create prompt template {TemplateId}", template.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.PromptManagement.PromptTemplate> UpdateAsync(Core.Models.PromptManagement.PromptTemplate template)
    {
        try
        {
            var entity = await _dbContext.PromptTemplates
                .FirstOrDefaultAsync(t => t.Id == template.Id);

            if (entity == null)
            {
                throw new Exception($"Prompt template with ID {template.Id} not found");
            }

            // Update the entity
            entity.Name = template.Name;
            entity.Description = template.Description;
            entity.Content = template.Content;
            entity.Version = template.Version;
            entity.SetTags(template.Tags);
            entity.SetVariables(template.Variables);
            entity.UpdatedAt = template.UpdatedAt;
            entity.IsPublic = template.IsPublic;

            _dbContext.PromptTemplates.Update(entity);
            await _dbContext.SaveChangesAsync();

            // Also create a version record
            var versionEntity = Persistence.Entities.PromptTemplateVersion.FromDomainModel(template);
            _dbContext.PromptTemplateVersions.Add(versionEntity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update prompt template {TemplateId}", template.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string templateId)
    {
        try
        {
            var entity = await _dbContext.PromptTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (entity != null)
            {
                _dbContext.PromptTemplates.Remove(entity);

                // Also delete all versions
                var versions = await _dbContext.PromptTemplateVersions
                    .Where(v => v.TemplateId == templateId)
                    .ToListAsync();

                _dbContext.PromptTemplateVersions.RemoveRange(versions);

                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete prompt template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Core.Models.PromptManagement.PromptTemplate> Templates, int TotalCount)> SearchAsync(
        string? query,
        IEnumerable<string>? tags,
        string? createdBy,
        bool? publicOnly,
        int page,
        int pageSize)
    {
        try
        {
            var queryable = _dbContext.PromptTemplates.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(query))
            {
                queryable = queryable.Where(t =>
                    t.Name.Contains(query) ||
                    t.Description.Contains(query) ||
                    t.Content.Contains(query));
            }

            if (tags != null && tags.Any())
            {
                var tagList = tags.ToList();
                var tagJson = System.Text.Json.JsonSerializer.Serialize(tagList);

                // This is a simple approach - in a real implementation, you would use a more sophisticated
                // approach to search for tags in the JSON array
                queryable = queryable.Where(t => tagList.Any(tag => t.TagsJson.Contains(tag)));
            }

            if (!string.IsNullOrWhiteSpace(createdBy))
            {
                queryable = queryable.Where(t => t.CreatedBy == createdBy);
            }

            if (publicOnly.HasValue && publicOnly.Value)
            {
                queryable = queryable.Where(t => t.IsPublic);
            }

            // Get total count
            var totalCount = await queryable.CountAsync();

            // Apply pagination
            var entities = await queryable
                .OrderByDescending(t => t.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (entities.Select(e => e.ToDomainModel()).ToList(), totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search prompt templates");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.PromptManagement.PromptTemplate>> GetVersionsAsync(string templateId)
    {
        try
        {
            var entities = await _dbContext.PromptTemplateVersions
                .Where(t => t.TemplateId == templateId)
                .OrderByDescending(t => t.Version)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get versions for prompt template {TemplateId}", templateId);
            throw;
        }
    }
}
