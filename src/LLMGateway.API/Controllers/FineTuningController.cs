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

    // Phase 3 Advanced Fine-Tuning Features

    /// <summary>
    /// Get fine-tuning analytics
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Fine-tuning analytics</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(FineTuningAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAnalytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetUserId();
            var analytics = await _fineTuningService.GetAnalyticsAsync(userId, startDate, endDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning analytics");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get cost breakdown for fine-tuning
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Cost breakdown</returns>
    [HttpGet("cost-breakdown")]
    [ProducesResponseType(typeof(FineTuningCostBreakdown), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCostBreakdown([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetUserId();
            var breakdown = await _fineTuningService.GetCostBreakdownAsync(userId, startDate, endDate);
            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning cost breakdown");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Estimate fine-tuning cost
    /// </summary>
    /// <param name="request">Cost estimation request</param>
    /// <returns>Cost estimate</returns>
    [HttpPost("estimate-cost")]
    [ProducesResponseType(typeof(FineTuningCostEstimate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EstimateCost([FromBody] EstimateFineTuningCostRequest request)
    {
        try
        {
            var userId = GetUserId();
            var estimate = await _fineTuningService.EstimateCostAsync(request, userId);
            return Ok(estimate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate fine-tuning cost");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Compare model performance
    /// </summary>
    /// <param name="baseModelId">Base model ID</param>
    /// <param name="fineTunedModelId">Fine-tuned model ID</param>
    /// <returns>Performance comparison</returns>
    [HttpGet("compare/{baseModelId}/{fineTunedModelId}")]
    [ProducesResponseType(typeof(ModelPerformanceComparison), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompareModelPerformance(string baseModelId, string fineTunedModelId)
    {
        try
        {
            var userId = GetUserId();
            var comparison = await _fineTuningService.CompareModelPerformanceAsync(baseModelId, fineTunedModelId, userId);
            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare model performance");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get fine-tuning recommendations
    /// </summary>
    /// <param name="useCase">Use case description</param>
    /// <returns>Recommendations</returns>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(FineTuningRecommendations), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRecommendations([FromQuery] string useCase)
    {
        try
        {
            var userId = GetUserId();
            var recommendations = await _fineTuningService.GetRecommendationsAsync(userId, useCase);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning recommendations");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validate data quality
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <returns>Data quality report</returns>
    [HttpPost("validate-data/{fileId}")]
    [ProducesResponseType(typeof(DataQualityReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateDataQuality(string fileId)
    {
        try
        {
            var userId = GetUserId();
            var report = await _fineTuningService.ValidateDataQualityAsync(fileId, userId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate data quality for file {FileId}", fileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get fine-tuning templates
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="useCase">Use case</param>
    /// <returns>Templates</returns>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IEnumerable<FineTuningTemplate>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTemplates([FromQuery] string provider, [FromQuery] string useCase)
    {
        try
        {
            var templates = await _fineTuningService.GetTemplatesAsync(provider, useCase);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning templates");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create job from template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="request">Job creation request</param>
    /// <returns>Created job</returns>
    [HttpPost("templates/{templateId}/create-job")]
    [ProducesResponseType(typeof(FineTuningJob), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateJobFromTemplate(string templateId, [FromBody] CreateJobFromTemplateRequest request)
    {
        try
        {
            var userId = GetUserId();
            var job = await _fineTuningService.CreateJobFromTemplateAsync(templateId, request, userId);
            return CreatedAtAction(nameof(GetJobInsights), new { jobId = job.Id }, job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job from template {TemplateId}", templateId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get job insights
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Job insights</returns>
    [HttpGet("jobs/{jobId}/insights")]
    [ProducesResponseType(typeof(FineTuningJobInsights), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetJobInsights(string jobId)
    {
        try
        {
            var userId = GetUserId();
            var insights = await _fineTuningService.GetJobInsightsAsync(jobId, userId);
            return Ok(insights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job insights for {JobId}", jobId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Export job data
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="format">Export format</param>
    /// <returns>Export data</returns>
    [HttpGet("jobs/{jobId}/export")]
    [ProducesResponseType(typeof(LLMGateway.Core.Models.Analytics.ExportData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportJobData(string jobId, [FromQuery] LLMGateway.Core.Models.Analytics.ExportFormat format = LLMGateway.Core.Models.Analytics.ExportFormat.Json)
    {
        try
        {
            var userId = GetUserId();
            var exportData = await _fineTuningService.ExportJobDataAsync(jobId, format, userId);
            return Ok(exportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export job data for {JobId}", jobId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
