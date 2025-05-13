using LLMGateway.API.Controllers.Base;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.PromptManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for prompt templates
/// </summary>
[ApiVersion("1.0")]
[Authorize]
public class PromptTemplatesController : BaseApiController
{
    private readonly IPromptTemplateService _promptTemplateService;
    private readonly ILogger<PromptTemplatesController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="promptTemplateService">Prompt template service</param>
    /// <param name="logger">Logger</param>
    public PromptTemplatesController(
        IPromptTemplateService promptTemplateService,
        ILogger<PromptTemplatesController> logger)
    {
        _promptTemplateService = promptTemplateService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current user ID from the claims
    /// </summary>
    /// <returns>User ID</returns>
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
    }

    /// <summary>
    /// Get all prompt templates
    /// </summary>
    /// <returns>List of prompt templates</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PromptTemplate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PromptTemplate>>> GetAllTemplatesAsync()
    {
        var userId = GetUserId();
        var templates = await _promptTemplateService.GetAllTemplatesAsync(userId);
        return Ok(templates);
    }

    /// <summary>
    /// Get prompt template by ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Prompt template</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PromptTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromptTemplate>> GetTemplateAsync(string id)
    {
        var userId = GetUserId();
        var template = await _promptTemplateService.GetTemplateAsync(id, userId);
        return Ok(template);
    }

    /// <summary>
    /// Create prompt template
    /// </summary>
    /// <param name="request">Template request</param>
    /// <returns>Created prompt template</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PromptTemplate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromptTemplate>> CreateTemplateAsync(PromptTemplateRequest request)
    {
        var userId = GetUserId();
        var template = await _promptTemplateService.CreateTemplateAsync(request, userId);
        return CreatedAtAction(nameof(GetTemplateAsync), new { id = template.Id }, template);
    }

    /// <summary>
    /// Update prompt template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated prompt template</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PromptTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromptTemplate>> UpdateTemplateAsync(string id, PromptTemplateUpdateRequest request)
    {
        var userId = GetUserId();
        var template = await _promptTemplateService.UpdateTemplateAsync(id, request, userId);
        return Ok(template);
    }

    /// <summary>
    /// Delete prompt template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteTemplateAsync(string id)
    {
        var userId = GetUserId();
        await _promptTemplateService.DeleteTemplateAsync(id, userId);
        return NoContent();
    }

    /// <summary>
    /// Render prompt template
    /// </summary>
    /// <param name="request">Render request</param>
    /// <returns>Rendered prompt</returns>
    [HttpPost("render")]
    [ProducesResponseType(typeof(PromptRenderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromptRenderResponse>> RenderTemplateAsync(PromptRenderRequest request)
    {
        var userId = GetUserId();
        var response = await _promptTemplateService.RenderTemplateAsync(request, userId);
        return Ok(response);
    }

    /// <summary>
    /// Search prompt templates
    /// </summary>
    /// <param name="request">Search request</param>
    /// <returns>Search response</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PromptTemplateSearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromptTemplateSearchResponse>> SearchTemplatesAsync(PromptTemplateSearchRequest request)
    {
        var userId = GetUserId();
        var response = await _promptTemplateService.SearchTemplatesAsync(request, userId);
        return Ok(response);
    }

    /// <summary>
    /// Get template versions
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>List of template versions</returns>
    [HttpGet("{id}/versions")]
    [ProducesResponseType(typeof(IEnumerable<PromptTemplate>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PromptTemplate>>> GetTemplateVersionsAsync(string id)
    {
        var userId = GetUserId();
        var versions = await _promptTemplateService.GetTemplateVersionsAsync(id, userId);
        return Ok(versions);
    }

    /// <summary>
    /// Create template version
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>New template version</returns>
    [HttpPost("{id}/versions")]
    [ProducesResponseType(typeof(PromptTemplate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromptTemplate>> CreateTemplateVersionAsync(string id)
    {
        var userId = GetUserId();
        var template = await _promptTemplateService.CreateTemplateVersionAsync(id, userId);
        return CreatedAtAction(nameof(GetTemplateAsync), new { id = template.Id }, template);
    }
}
