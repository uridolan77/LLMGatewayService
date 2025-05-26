using LLMGateway.Core.Features.TokenUsage.Queries;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.TokenUsage;
using LLMGateway.Core.Models.Analytics;
using LLMGateway.Core.Models.Cost;
using LLMGateway.Core.Models.FineTuning;
using LLMGateway.Core.Models.SDK;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Enhanced admin controller with Phase 3 capabilities
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = "AdminAccess")]
[Route("api/v{version:apiVersion}/admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IAdvancedAnalyticsService _analyticsService;
    private readonly ICostManagementService _costManagementService;
    private readonly IFineTuningService _fineTuningService;
    private readonly ISDKManagementService _sdkManagementService;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator">Mediator</param>
    /// <param name="providerFactory">Provider factory</param>
    /// <param name="tokenUsageService">Token usage service</param>
    /// <param name="analyticsService">Advanced analytics service</param>
    /// <param name="costManagementService">Cost management service</param>
    /// <param name="fineTuningService">Fine-tuning service</param>
    /// <param name="sdkManagementService">SDK management service</param>
    /// <param name="logger">Logger</param>
    public AdminController(
        IMediator mediator,
        ILLMProviderFactory providerFactory,
        ITokenUsageService tokenUsageService,
        IAdvancedAnalyticsService analyticsService,
        ICostManagementService costManagementService,
        IFineTuningService fineTuningService,
        ISDKManagementService sdkManagementService,
        ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _providerFactory = providerFactory;
        _tokenUsageService = tokenUsageService;
        _analyticsService = analyticsService;
        _costManagementService = costManagementService;
        _fineTuningService = fineTuningService;
        _sdkManagementService = sdkManagementService;
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

    // Phase 3 Advanced Analytics Endpoints

    /// <summary>
    /// Get advanced analytics dashboard
    /// </summary>
    /// <param name="request">Analytics request</param>
    /// <returns>Advanced analytics</returns>
    [HttpPost("analytics/advanced")]
    [ProducesResponseType(typeof(UsageAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAdvancedAnalytics([FromBody] AnalyticsRequest request)
    {
        try
        {
            var analytics = await _analyticsService.GetUsageAnalyticsAsync(request);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get advanced analytics");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get real-time dashboard data
    /// </summary>
    /// <returns>Real-time dashboard</returns>
    [HttpGet("dashboard/realtime")]
    [ProducesResponseType(typeof(RealTimeDashboard), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRealTimeDashboard()
    {
        try
        {
            var dashboard = await _analyticsService.GetRealTimeDashboardAsync();
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get real-time dashboard");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get cost analytics
    /// </summary>
    /// <param name="request">Cost analytics request</param>
    /// <returns>Cost analytics</returns>
    [HttpPost("analytics/cost")]
    [ProducesResponseType(typeof(CostAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCostAnalytics([FromBody] CostAnalyticsRequest request)
    {
        try
        {
            var analytics = await _analyticsService.GetCostAnalyticsAsync(request);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost analytics");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get performance analytics
    /// </summary>
    /// <param name="request">Performance analytics request</param>
    /// <returns>Performance analytics</returns>
    [HttpPost("analytics/performance")]
    [ProducesResponseType(typeof(PerformanceAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPerformanceAnalytics([FromBody] PerformanceAnalyticsRequest request)
    {
        try
        {
            var analytics = await _analyticsService.GetPerformanceAnalyticsAsync(request);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance analytics");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Detect anomalies
    /// </summary>
    /// <param name="request">Anomaly detection request</param>
    /// <returns>Anomaly detection results</returns>
    [HttpPost("analytics/anomalies")]
    [ProducesResponseType(typeof(AnomalyDetectionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DetectAnomalies([FromBody] AnomalyDetectionRequest request)
    {
        try
        {
            var result = await _analyticsService.DetectAnomaliesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect anomalies");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    // Phase 3 Advanced Cost Management Endpoints

    /// <summary>
    /// Get cost optimization recommendations
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="timeframeDays">Timeframe in days</param>
    /// <returns>Cost optimization recommendations</returns>
    [HttpGet("cost/optimization/{userId}")]
    [ProducesResponseType(typeof(CostOptimizationRecommendations), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCostOptimizationRecommendations(string userId, [FromQuery] int timeframeDays = 30)
    {
        try
        {
            var timeframe = TimeSpan.FromDays(timeframeDays);
            var recommendations = await _costManagementService.GetCostOptimizationRecommendationsAsync(userId, timeframe);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost optimization recommendations for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get cost forecast
    /// </summary>
    /// <param name="request">Cost forecast request</param>
    /// <returns>Cost forecast</returns>
    [HttpPost("cost/forecast")]
    [ProducesResponseType(typeof(LLMGateway.Core.Models.Cost.CostForecast), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCostForecast([FromBody] CostForecastRequest request)
    {
        try
        {
            var forecast = await _costManagementService.GetCostForecastAsync(request, "admin");
            return Ok(forecast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost forecast");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get cost anomalies
    /// </summary>
    /// <param name="request">Cost anomaly detection request</param>
    /// <returns>Cost anomalies</returns>
    [HttpPost("cost/anomalies")]
    [ProducesResponseType(typeof(CostAnomalyDetectionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DetectCostAnomalies([FromBody] CostAnomalyDetectionRequest request)
    {
        try
        {
            var result = await _costManagementService.DetectCostAnomaliesAsync(request, "admin");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect cost anomalies");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get real-time cost data
    /// </summary>
    /// <returns>Real-time cost data</returns>
    [HttpGet("cost/realtime")]
    [ProducesResponseType(typeof(RealTimeCostData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRealTimeCostData()
    {
        try
        {
            var costData = await _costManagementService.GetRealTimeCostDataAsync("admin");
            return Ok(costData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get real-time cost data");
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
