using LLMGateway.Core.Features.TokenUsage.Queries;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.TokenUsage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for admin operations
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = "AdminAccess")]
[Route("api/v{version:apiVersion}/admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator">Mediator</param>
    /// <param name="providerFactory">Provider factory</param>
    /// <param name="tokenUsageService">Token usage service</param>
    /// <param name="logger">Logger</param>
    public AdminController(
        IMediator mediator,
        ILLMProviderFactory providerFactory,
        ITokenUsageService tokenUsageService,
        ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _providerFactory = providerFactory;
        _tokenUsageService = tokenUsageService;
        _logger = logger;
    }

    /// <summary>
    /// Get token usage summary
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Token usage summary</returns>
    [HttpGet("token-usage")]
    [ProducesResponseType(typeof(TokenUsageSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TokenUsageSummary>> GetTokenUsageSummaryAsync(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate)
    {
        _logger.LogInformation("Getting token usage summary");

        try
        {
            var start = startDate ?? DateTimeOffset.UtcNow.AddDays(-30);
            var end = endDate ?? DateTimeOffset.UtcNow;

            var query = new GetTokenUsageSummaryQuery
            {
                StartDate = start,
                EndDate = end
            };

            var summary = await _mediator.Send(query);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token usage summary");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get token usage for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Token usage records</returns>
    [HttpGet("token-usage/user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<TokenUsageRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<TokenUsageRecord>>> GetTokenUsageForUserAsync(
        string userId,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate)
    {
        _logger.LogInformation("Getting token usage for user {UserId}", userId);

        try
        {
            var start = startDate ?? DateTimeOffset.UtcNow.AddDays(-30);
            var end = endDate ?? DateTimeOffset.UtcNow;

            var query = new GetTokenUsageForUserQuery
            {
                UserId = userId,
                StartDate = start,
                EndDate = end
            };

            var records = await _mediator.Send(query);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token usage for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get token usage for a model
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Token usage records</returns>
    [HttpGet("token-usage/model/{modelId}")]
    [ProducesResponseType(typeof(IEnumerable<TokenUsageRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<TokenUsageRecord>>> GetTokenUsageForModelAsync(
        string modelId,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate)
    {
        _logger.LogInformation("Getting token usage for model {ModelId}", modelId);

        try
        {
            var start = startDate ?? DateTimeOffset.UtcNow.AddDays(-30);
            var end = endDate ?? DateTimeOffset.UtcNow;

            var query = new GetTokenUsageForModelQuery
            {
                ModelId = modelId,
                StartDate = start,
                EndDate = end
            };

            var records = await _mediator.Send(query);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token usage for model {ModelId}", modelId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get provider health status
    /// </summary>
    /// <returns>Provider health status</returns>
    [HttpGet("provider-health")]
    [ProducesResponseType(typeof(Dictionary<string, bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Dictionary<string, bool>>> GetProviderHealthStatusAsync()
    {
        _logger.LogInformation("Getting provider health status");

        try
        {
            var providers = _providerFactory.GetAllProviders();
            var healthStatus = new Dictionary<string, bool>();

            foreach (var provider in providers)
            {
                var isAvailable = await provider.IsAvailableAsync();
                healthStatus[provider.Name] = isAvailable;
            }

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider health status");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get dashboard summary
    /// </summary>
    /// <returns>Dashboard summary</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardSummary>> GetDashboardSummaryAsync()
    {
        _logger.LogInformation("Getting dashboard summary");

        try
        {
            // Get token usage summary for the last 30 days
            var tokenUsageSummary = await _tokenUsageService.GetUsageSummaryAsync(
                DateTimeOffset.UtcNow.AddDays(-30),
                DateTimeOffset.UtcNow);

            // Get provider health status
            var providers = _providerFactory.GetAllProviders();
            var providerHealth = new Dictionary<string, bool>();

            foreach (var provider in providers)
            {
                var isAvailable = await provider.IsAvailableAsync();
                providerHealth[provider.Name] = isAvailable;
            }

            // Create the dashboard summary
            var summary = new DashboardSummary
            {
                TotalTokens = tokenUsageSummary.TotalTokens,
                TotalCost = tokenUsageSummary.TotalEstimatedCostUsd,
                ProviderHealth = providerHealth,
                TopModels = tokenUsageSummary.UsageByModel
                    .OrderByDescending(m => m.Value.TotalTokens)
                    .Take(5)
                    .ToDictionary(m => m.Key, m => m.Value.TotalTokens),
                TopUsers = tokenUsageSummary.UsageByUser
                    .OrderByDescending(u => u.Value.TotalTokens)
                    .Take(5)
                    .ToDictionary(u => u.Key, u => u.Value.TotalTokens)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard summary");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}

/// <summary>
/// Dashboard summary
/// </summary>
public class DashboardSummary
{
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Provider health
    /// </summary>
    public Dictionary<string, bool> ProviderHealth { get; set; } = new();

    /// <summary>
    /// Top models by usage
    /// </summary>
    public Dictionary<string, int> TopModels { get; set; } = new();

    /// <summary>
    /// Top users by usage
    /// </summary>
    public Dictionary<string, int> TopUsers { get; set; } = new();
}
