using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.PromptManagement;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for managing prompt templates
/// </summary>
public class PromptTemplateService : IPromptTemplateService
{
    private readonly IPromptTemplateRepository _repository;
    private readonly ILogger<PromptTemplateService> _logger;
    private static readonly Regex VariablePattern = new(@"\{\{([^{}]+)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="repository">Prompt template repository</param>
    /// <param name="logger">Logger</param>
    public PromptTemplateService(
        IPromptTemplateRepository repository,
        ILogger<PromptTemplateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PromptTemplate>> GetAllTemplatesAsync(string userId)
    {
        try
        {
            return await _repository.GetAllAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all prompt templates for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PromptTemplate> GetTemplateAsync(string templateId, string userId)
    {
        try
        {
            var template = await _repository.GetByIdAsync(templateId);
            if (template == null)
            {
                throw new NotFoundException($"Prompt template with ID {templateId} not found");
            }

            // Check if the user has access to the template
            if (!template.IsPublic && template.CreatedBy != userId)
            {
                throw new ForbiddenException("You don't have access to this template");
            }

            return template;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get prompt template {TemplateId} for user {UserId}", templateId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PromptTemplate> CreateTemplateAsync(PromptTemplateRequest request, string userId)
    {
        try
        {
            // Validate the request
            ValidateTemplateRequest(request);

            // Extract variables from the content
            var extractedVariables = ExtractVariablesFromContent(request.Content);
            var variables = request.Variables ?? new List<PromptVariable>();

            // Add any missing variables
            foreach (var varName in extractedVariables)
            {
                if (!variables.Any(v => v.Name == varName))
                {
                    variables.Add(new PromptVariable
                    {
                        Name = varName,
                        Description = $"Variable {varName}",
                        Required = true,
                        Type = PromptVariableType.String
                    });
                }
            }

            // Create the template
            var template = new PromptTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Content = request.Content,
                Tags = request.Tags ?? new List<string>(),
                Variables = variables,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsPublic = request.IsPublic,
                Version = 1
            };

            return await _repository.CreateAsync(template);
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to create prompt template for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PromptTemplate> UpdateTemplateAsync(string templateId, PromptTemplateUpdateRequest request, string userId)
    {
        try
        {
            // Get the existing template
            var template = await _repository.GetByIdAsync(templateId);
            if (template == null)
            {
                throw new NotFoundException($"Prompt template with ID {templateId} not found");
            }

            // Check if the user has permission to update the template
            if (template.CreatedBy != userId)
            {
                throw new ForbiddenException("You don't have permission to update this template");
            }

            // Update the template properties
            if (request.Name != null)
            {
                template.Name = request.Name;
            }

            if (request.Description != null)
            {
                template.Description = request.Description;
            }

            if (request.Content != null)
            {
                template.Content = request.Content;

                // Extract variables from the content
                var extractedVariables = ExtractVariablesFromContent(request.Content);
                var variables = request.Variables ?? template.Variables;

                // Add any missing variables
                foreach (var varName in extractedVariables)
                {
                    if (!variables.Any(v => v.Name == varName))
                    {
                        variables.Add(new PromptVariable
                        {
                            Name = varName,
                            Description = $"Variable {varName}",
                            Required = true,
                            Type = PromptVariableType.String
                        });
                    }
                }

                template.Variables = variables;
            }
            else if (request.Variables != null)
            {
                template.Variables = request.Variables;
            }

            if (request.Tags != null)
            {
                template.Tags = request.Tags;
            }

            if (request.IsPublic.HasValue)
            {
                template.IsPublic = request.IsPublic.Value;
            }

            template.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateAsync(template);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to update prompt template {TemplateId} for user {UserId}", templateId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteTemplateAsync(string templateId, string userId)
    {
        try
        {
            // Get the existing template
            var template = await _repository.GetByIdAsync(templateId);
            if (template == null)
            {
                throw new NotFoundException($"Prompt template with ID {templateId} not found");
            }

            // Check if the user has permission to delete the template
            if (template.CreatedBy != userId)
            {
                throw new ForbiddenException("You don't have permission to delete this template");
            }

            await _repository.DeleteAsync(templateId);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to delete prompt template {TemplateId} for user {UserId}", templateId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PromptRenderResponse> RenderTemplateAsync(PromptRenderRequest request, string userId)
    {
        try
        {
            // Get the template
            var template = await GetTemplateAsync(request.TemplateId, userId);

            // Render the template
            var renderedPrompt = RenderTemplate(template, request.Variables);

            return new PromptRenderResponse
            {
                RenderedPrompt = renderedPrompt,
                TemplateId = template.Id,
                TemplateName = template.Name,
                Variables = request.Variables
            };
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException && ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to render prompt template {TemplateId} for user {UserId}", request.TemplateId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PromptTemplateSearchResponse> SearchTemplatesAsync(PromptTemplateSearchRequest request, string userId)
    {
        try
        {
            var (templates, totalCount) = await _repository.SearchAsync(
                request.Query,
                request.Tags,
                request.CreatedBy,
                request.PublicOnly,
                request.Page,
                request.PageSize);

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new PromptTemplateSearchResponse
            {
                Templates = templates.ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search prompt templates for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PromptTemplate>> GetTemplateVersionsAsync(string templateId, string userId)
    {
        try
        {
            // Check if the template exists and the user has access
            await GetTemplateAsync(templateId, userId);

            // Get all versions
            return await _repository.GetVersionsAsync(templateId);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get versions for prompt template {TemplateId} for user {UserId}", templateId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PromptTemplate> CreateTemplateVersionAsync(string templateId, string userId)
    {
        try
        {
            // Get the existing template
            var template = await GetTemplateAsync(templateId, userId);

            // Check if the user has permission to create a new version
            if (template.CreatedBy != userId)
            {
                throw new ForbiddenException("You don't have permission to create a new version of this template");
            }

            // Create a new version
            var newVersion = new PromptTemplate
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Content = template.Content,
                Tags = template.Tags,
                Variables = template.Variables,
                CreatedBy = userId,
                CreatedAt = template.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                IsPublic = template.IsPublic,
                Version = template.Version + 1
            };

            return await _repository.UpdateAsync(newVersion);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to create new version for prompt template {TemplateId} for user {UserId}", templateId, userId);
            throw;
        }
    }

    #region Helper methods

    private static void ValidateTemplateRequest(PromptTemplateRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name", "Template name is required");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            errors.Add("Content", "Template content is required");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Invalid template request", errors);
        }
    }

    private static IEnumerable<string> ExtractVariablesFromContent(string content)
    {
        var matches = VariablePattern.Matches(content);
        return matches.Select(m => m.Groups[1].Value.Trim()).Distinct();
    }

    private string RenderTemplate(PromptTemplate template, Dictionary<string, string> variables)
    {
        // Check for missing required variables
        var missingVariables = template.Variables
            .Where(v => v.Required && !variables.ContainsKey(v.Name) && string.IsNullOrEmpty(v.DefaultValue))
            .Select(v => v.Name)
            .ToList();

        if (missingVariables.Any())
        {
            throw new ValidationException($"Missing required variables: {string.Join(", ", missingVariables)}");
        }

        // Render the template
        var renderedPrompt = template.Content;

        foreach (var variable in template.Variables)
        {
            string value;

            if (variables.TryGetValue(variable.Name, out var providedValue))
            {
                value = providedValue;
            }
            else if (!string.IsNullOrEmpty(variable.DefaultValue))
            {
                value = variable.DefaultValue;
            }
            else
            {
                // Skip optional variables that are not provided
                continue;
            }

            // Replace the variable in the template
            renderedPrompt = renderedPrompt.Replace($"{{{{{variable.Name}}}}}", value);
        }

        return renderedPrompt;
    }

    #endregion
}
