using LLMGateway.API.Controllers.Base;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for A/B testing
/// </summary>
[ApiVersion("1.0")]
[Authorize]
public class ABTestingController : BaseApiController
{
    private readonly IABTestingService _abTestingService;
    private readonly ILogger<ABTestingController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="abTestingService">A/B testing service</param>
    /// <param name="logger">Logger</param>
    public ABTestingController(
        IABTestingService abTestingService,
        ILogger<ABTestingController> logger)
    {
        _abTestingService = abTestingService;
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
    /// Get all experiments
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive experiments</param>
    /// <returns>List of experiments</returns>
    [HttpGet("experiments")]
    [ProducesResponseType(typeof(IEnumerable<ABTestingExperiment>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ABTestingExperiment>>> GetAllExperimentsAsync([FromQuery] bool includeInactive = false)
    {
        var experiments = await _abTestingService.GetAllExperimentsAsync(includeInactive);
        return Ok(experiments);
    }

    /// <summary>
    /// Get experiment by ID
    /// </summary>
    /// <param name="id">Experiment ID</param>
    /// <returns>Experiment</returns>
    [HttpGet("experiments/{id}")]
    [ProducesResponseType(typeof(ABTestingExperiment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ABTestingExperiment>> GetExperimentAsync(string id)
    {
        var experiment = await _abTestingService.GetExperimentAsync(id);
        return Ok(experiment);
    }

    /// <summary>
    /// Create experiment
    /// </summary>
    /// <param name="request">Create request</param>
    /// <returns>Created experiment</returns>
    [HttpPost("experiments")]
    [ProducesResponseType(typeof(ABTestingExperiment), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ABTestingExperiment>> CreateExperimentAsync(ABTestingExperimentCreateRequest request)
    {
        var userId = GetUserId();
        var experiment = await _abTestingService.CreateExperimentAsync(request, userId);
        return CreatedAtAction(nameof(GetExperimentAsync), new { id = experiment.Id }, experiment);
    }

    /// <summary>
    /// Update experiment
    /// </summary>
    /// <param name="id">Experiment ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated experiment</returns>
    [HttpPut("experiments/{id}")]
    [ProducesResponseType(typeof(ABTestingExperiment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ABTestingExperiment>> UpdateExperimentAsync(string id, ABTestingExperimentUpdateRequest request)
    {
        var experiment = await _abTestingService.UpdateExperimentAsync(id, request);
        return Ok(experiment);
    }

    /// <summary>
    /// Delete experiment
    /// </summary>
    /// <param name="id">Experiment ID</param>
    /// <returns>No content</returns>
    [HttpDelete("experiments/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteExperimentAsync(string id)
    {
        await _abTestingService.DeleteExperimentAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Get experiment results
    /// </summary>
    /// <param name="id">Experiment ID</param>
    /// <returns>List of results</returns>
    [HttpGet("experiments/{id}/results")]
    [ProducesResponseType(typeof(IEnumerable<ABTestingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ABTestingResult>>> GetExperimentResultsAsync(string id)
    {
        var results = await _abTestingService.GetExperimentResultsAsync(id);
        return Ok(results);
    }

    /// <summary>
    /// Create experiment result
    /// </summary>
    /// <param name="request">Create request</param>
    /// <returns>Created result</returns>
    [HttpPost("results")]
    [ProducesResponseType(typeof(ABTestingResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ABTestingResult>> CreateResultAsync(ABTestingResultCreateRequest request)
    {
        var userId = GetUserId();
        var result = await _abTestingService.CreateResultAsync(request, userId);
        return CreatedAtAction(nameof(GetExperimentResultsAsync), new { id = result.ExperimentId }, result);
    }

    /// <summary>
    /// Get experiment statistics
    /// </summary>
    /// <param name="id">Experiment ID</param>
    /// <returns>Experiment statistics</returns>
    [HttpGet("experiments/{id}/statistics")]
    [ProducesResponseType(typeof(ABTestingExperimentStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ABTestingExperimentStatistics>> GetExperimentStatisticsAsync(string id)
    {
        var statistics = await _abTestingService.GetExperimentStatisticsAsync(id);
        return Ok(statistics);
    }

    /// <summary>
    /// Assign user to experiment group
    /// </summary>
    /// <param name="id">Experiment ID</param>
    /// <returns>Group assignment</returns>
    [HttpPost("experiments/{id}/assign")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<string>> AssignUserToGroupAsync(string id)
    {
        var userId = GetUserId();
        var group = await _abTestingService.AssignUserToGroupAsync(id, userId);
        return Ok(group);
    }

    /// <summary>
    /// Get model for user
    /// </summary>
    /// <param name="modelId">Requested model ID</param>
    /// <returns>Model ID to use</returns>
    [HttpGet("model")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetModelForUserAsync([FromQuery] string modelId)
    {
        var userId = GetUserId();
        var actualModelId = await _abTestingService.GetModelForUserAsync(modelId, userId);
        return Ok(actualModelId);
    }

    /// <summary>
    /// Get active experiments for model
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>List of experiments</returns>
    [HttpGet("models/{modelId}/experiments")]
    [ProducesResponseType(typeof(IEnumerable<ABTestingExperiment>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ABTestingExperiment>>> GetActiveExperimentsForModelAsync(string modelId)
    {
        var experiments = await _abTestingService.GetActiveExperimentsForModelAsync(modelId);
        return Ok(experiments);
    }
}
