using LLMGateway.API.Controllers.Base;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Cost;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for cost management
/// </summary>
[ApiVersion("1.0")]
[Authorize]
public class CostController : BaseApiController
{
    private readonly ICostManagementService _costManagementService;
    private readonly ILogger<CostController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="costManagementService">Cost management service</param>
    /// <param name="logger">Logger</param>
    public CostController(
        ICostManagementService costManagementService,
        ILogger<CostController> logger)
    {
        _costManagementService = costManagementService;
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
    /// Get cost records
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="operationType">Operation type</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <returns>Cost records</returns>
    [HttpGet("records")]
    [ProducesResponseType(typeof(IEnumerable<CostRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CostRecord>>> GetCostRecordsAsync(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? provider = null,
        [FromQuery] string? modelId = null,
        [FromQuery] string? operationType = null,
        [FromQuery] string? projectId = null,
        [FromQuery] string? tags = null)
    {
        var userId = GetUserId();
        var tagsList = tags?.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

        var records = await _costManagementService.GetCostRecordsAsync(
            userId,
            startDate,
            endDate,
            provider,
            modelId,
            operationType,
            projectId,
            tagsList);

        return Ok(records);
    }

    /// <summary>
    /// Get cost report
    /// </summary>
    /// <param name="request">Report request</param>
    /// <returns>Cost report</returns>
    [HttpPost("report")]
    [ProducesResponseType(typeof(CostReport), StatusCodes.Status200OK)]
    public async Task<ActionResult<CostReport>> GetCostReportAsync(CostReportRequest request)
    {
        var userId = GetUserId();
        var report = await _costManagementService.GetCostReportAsync(request, userId);
        return Ok(report);
    }

    /// <summary>
    /// Get all budgets
    /// </summary>
    /// <returns>Budgets</returns>
    [HttpGet("budgets")]
    [ProducesResponseType(typeof(IEnumerable<Budget>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Budget>>> GetAllBudgetsAsync()
    {
        var userId = GetUserId();
        var budgets = await _costManagementService.GetAllBudgetsAsync(userId);
        return Ok(budgets);
    }

    /// <summary>
    /// Get budget by ID
    /// </summary>
    /// <param name="id">Budget ID</param>
    /// <returns>Budget</returns>
    [HttpGet("budgets/{id}")]
    [ProducesResponseType(typeof(Budget), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Budget>> GetBudgetAsync(string id)
    {
        var userId = GetUserId();
        var budget = await _costManagementService.GetBudgetAsync(id, userId);
        return Ok(budget);
    }

    /// <summary>
    /// Create budget
    /// </summary>
    /// <param name="request">Create request</param>
    /// <returns>Created budget</returns>
    [HttpPost("budgets")]
    [ProducesResponseType(typeof(Budget), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Budget>> CreateBudgetAsync(CreateBudgetRequest request)
    {
        var userId = GetUserId();
        var budget = await _costManagementService.CreateBudgetAsync(request, userId);
        return CreatedAtAction(nameof(GetBudgetAsync), new { id = budget.Id }, budget);
    }

    /// <summary>
    /// Update budget
    /// </summary>
    /// <param name="id">Budget ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated budget</returns>
    [HttpPut("budgets/{id}")]
    [ProducesResponseType(typeof(Budget), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Budget>> UpdateBudgetAsync(string id, UpdateBudgetRequest request)
    {
        var userId = GetUserId();
        var budget = await _costManagementService.UpdateBudgetAsync(id, request, userId);
        return Ok(budget);
    }

    /// <summary>
    /// Delete budget
    /// </summary>
    /// <param name="id">Budget ID</param>
    /// <returns>No content</returns>
    [HttpDelete("budgets/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteBudgetAsync(string id)
    {
        var userId = GetUserId();
        await _costManagementService.DeleteBudgetAsync(id, userId);
        return NoContent();
    }

    /// <summary>
    /// Get budget usage
    /// </summary>
    /// <param name="id">Budget ID</param>
    /// <returns>Budget usage</returns>
    [HttpGet("budgets/{id}/usage")]
    [ProducesResponseType(typeof(BudgetUsage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BudgetUsage>> GetBudgetUsageAsync(string id)
    {
        var userId = GetUserId();
        var usage = await _costManagementService.GetBudgetUsageAsync(id, userId);
        return Ok(usage);
    }

    /// <summary>
    /// Get all budget usages
    /// </summary>
    /// <returns>Budget usages</returns>
    [HttpGet("budgets/usage")]
    [ProducesResponseType(typeof(IEnumerable<BudgetUsage>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BudgetUsage>>> GetAllBudgetUsagesAsync()
    {
        var userId = GetUserId();
        var usages = await _costManagementService.GetAllBudgetUsagesAsync(userId);
        return Ok(usages);
    }

    /// <summary>
    /// Estimate completion cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="inputTokens">Input tokens</param>
    /// <param name="outputTokens">Output tokens</param>
    /// <returns>Estimated cost in USD</returns>
    [HttpGet("estimate/completion")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<ActionResult<decimal>> EstimateCompletionCostAsync(
        [FromQuery] string provider,
        [FromQuery] string modelId,
        [FromQuery] int inputTokens,
        [FromQuery] int outputTokens)
    {
        var cost = await _costManagementService.EstimateCompletionCostAsync(provider, modelId, inputTokens, outputTokens);
        return Ok(cost);
    }

    /// <summary>
    /// Estimate embedding cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="inputTokens">Input tokens</param>
    /// <returns>Estimated cost in USD</returns>
    [HttpGet("estimate/embedding")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<ActionResult<decimal>> EstimateEmbeddingCostAsync(
        [FromQuery] string provider,
        [FromQuery] string modelId,
        [FromQuery] int inputTokens)
    {
        var cost = await _costManagementService.EstimateEmbeddingCostAsync(provider, modelId, inputTokens);
        return Ok(cost);
    }

    /// <summary>
    /// Estimate fine-tuning cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="trainingTokens">Training tokens</param>
    /// <returns>Estimated cost in USD</returns>
    [HttpGet("estimate/fine-tuning")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<ActionResult<decimal>> EstimateFineTuningCostAsync(
        [FromQuery] string provider,
        [FromQuery] string modelId,
        [FromQuery] int trainingTokens)
    {
        var cost = await _costManagementService.EstimateFineTuningCostAsync(provider, modelId, trainingTokens);
        return Ok(cost);
    }

    /// <summary>
    /// Get model pricing
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <returns>Model pricing</returns>
    [HttpGet("pricing")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetModelPricingAsync(
        [FromQuery] string provider,
        [FromQuery] string modelId)
    {
        var (inputPricePerToken, outputPricePerToken) = await _costManagementService.GetModelPricingAsync(provider, modelId);

        return Ok(new
        {
            Provider = provider,
            ModelId = modelId,
            InputPricePerToken = inputPricePerToken,
            OutputPricePerToken = outputPricePerToken
        });
    }
}
