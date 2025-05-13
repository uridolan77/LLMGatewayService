using LLMGateway.API.Controllers.Base;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.FineTuning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for fine-tuning operations
/// </summary>
[ApiVersion("1.0")]
[Authorize]
public class FineTuningController : BaseApiController
{
    private readonly IFineTuningService _fineTuningService;
    private readonly ILogger<FineTuningController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="fineTuningService">Fine-tuning service</param>
    /// <param name="logger">Logger</param>
    public FineTuningController(
        IFineTuningService fineTuningService,
        ILogger<FineTuningController> logger)
    {
        _fineTuningService = fineTuningService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current user ID from the claims
    /// </summary>
    /// <returns>User ID</returns>
    private string GetUserId()
    {
        return User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "anonymous";
    }

    /// <summary>
    /// Get all fine-tuning jobs
    /// </summary>
    /// <returns>List of fine-tuning jobs</returns>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(IEnumerable<FineTuningJob>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FineTuningJob>>> GetAllJobsAsync()
    {
        var userId = GetUserId();
        var jobs = await _fineTuningService.GetAllJobsAsync(userId);
        return Ok(jobs);
    }

    /// <summary>
    /// Get fine-tuning job by ID
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>Fine-tuning job</returns>
    [HttpGet("jobs/{id}")]
    [ProducesResponseType(typeof(FineTuningJob), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FineTuningJob>> GetJobAsync(string id)
    {
        var userId = GetUserId();
        var job = await _fineTuningService.GetJobAsync(id, userId);
        return Ok(job);
    }

    /// <summary>
    /// Create fine-tuning job
    /// </summary>
    /// <param name="request">Create request</param>
    /// <returns>Created fine-tuning job</returns>
    [HttpPost("jobs")]
    [ProducesResponseType(typeof(FineTuningJob), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FineTuningJob>> CreateJobAsync(CreateFineTuningJobRequest request)
    {
        var userId = GetUserId();
        var job = await _fineTuningService.CreateJobAsync(request, userId);
        return CreatedAtAction(nameof(GetJobAsync), new { id = job.Id }, job);
    }

    /// <summary>
    /// Cancel fine-tuning job
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>Cancelled fine-tuning job</returns>
    [HttpPost("jobs/{id}/cancel")]
    [ProducesResponseType(typeof(FineTuningJob), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FineTuningJob>> CancelJobAsync(string id)
    {
        var userId = GetUserId();
        var job = await _fineTuningService.CancelJobAsync(id, userId);
        return Ok(job);
    }

    /// <summary>
    /// Delete fine-tuning job
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>No content</returns>
    [HttpDelete("jobs/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteJobAsync(string id)
    {
        var userId = GetUserId();
        await _fineTuningService.DeleteJobAsync(id, userId);
        return NoContent();
    }

    /// <summary>
    /// Get fine-tuning job events
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>List of fine-tuning step metrics</returns>
    [HttpGet("jobs/{id}/events")]
    [ProducesResponseType(typeof(IEnumerable<FineTuningStepMetric>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FineTuningStepMetric>>> GetJobEventsAsync(string id)
    {
        var userId = GetUserId();
        var events = await _fineTuningService.GetJobEventsAsync(id, userId);
        return Ok(events);
    }

    /// <summary>
    /// Search fine-tuning jobs
    /// </summary>
    /// <param name="request">Search request</param>
    /// <returns>Search response</returns>
    [HttpPost("jobs/search")]
    [ProducesResponseType(typeof(FineTuningJobSearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FineTuningJobSearchResponse>> SearchJobsAsync(FineTuningJobSearchRequest request)
    {
        var userId = GetUserId();
        var response = await _fineTuningService.SearchJobsAsync(request, userId);
        return Ok(response);
    }

    /// <summary>
    /// Sync job status with provider
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>Updated fine-tuning job</returns>
    [HttpPost("jobs/{id}/sync")]
    [ProducesResponseType(typeof(FineTuningJob), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FineTuningJob>> SyncJobStatusAsync(string id)
    {
        var userId = GetUserId();
        var job = await _fineTuningService.SyncJobStatusAsync(id, userId);
        return Ok(job);
    }

    /// <summary>
    /// Get all fine-tuning files
    /// </summary>
    /// <returns>List of fine-tuning files</returns>
    [HttpGet("files")]
    [ProducesResponseType(typeof(IEnumerable<FineTuningFile>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FineTuningFile>>> GetAllFilesAsync()
    {
        var userId = GetUserId();
        var files = await _fineTuningService.GetAllFilesAsync(userId);
        return Ok(files);
    }

    /// <summary>
    /// Get fine-tuning file by ID
    /// </summary>
    /// <param name="id">File ID</param>
    /// <returns>Fine-tuning file</returns>
    [HttpGet("files/{id}")]
    [ProducesResponseType(typeof(FineTuningFile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FineTuningFile>> GetFileAsync(string id)
    {
        var userId = GetUserId();
        var file = await _fineTuningService.GetFileAsync(id, userId);
        return Ok(file);
    }

    /// <summary>
    /// Upload fine-tuning file
    /// </summary>
    /// <param name="request">Upload request</param>
    /// <returns>Uploaded fine-tuning file</returns>
    [HttpPost("files")]
    [ProducesResponseType(typeof(FineTuningFile), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FineTuningFile>> UploadFileAsync(UploadFineTuningFileRequest request)
    {
        var userId = GetUserId();
        var file = await _fineTuningService.UploadFileAsync(request, userId);
        return CreatedAtAction(nameof(GetFileAsync), new { id = file.Id }, file);
    }

    /// <summary>
    /// Delete fine-tuning file
    /// </summary>
    /// <param name="id">File ID</param>
    /// <returns>No content</returns>
    [HttpDelete("files/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteFileAsync(string id)
    {
        var userId = GetUserId();
        await _fineTuningService.DeleteFileAsync(id, userId);
        return NoContent();
    }

    /// <summary>
    /// Get file content
    /// </summary>
    /// <param name="id">File ID</param>
    /// <returns>File content</returns>
    [HttpGet("files/{id}/content")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<string>> GetFileContentAsync(string id)
    {
        var userId = GetUserId();
        var content = await _fineTuningService.GetFileContentAsync(id, userId);
        return Ok(content);
    }
}
