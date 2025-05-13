// src/LLMGateway.API/Controllers/V1/AdminDashboardController.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models;
using LLMGateway.Core.Routing;
using LLMGateway.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/dashboard")]
[Authorize(Policy = "AdminAccess")]
public class AdminDashboardController : ControllerBase
{
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IRoutingRepository _routingRepository;
    private readonly IModelMetricsRepository _modelMetricsRepository;
    private readonly IProviderHealthMonitorService _healthMonitorService;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILogger<AdminDashboardController> _logger;
    
    public AdminDashboardController(
        ITokenUsageService tokenUsageService,
        IRoutingRepository routingRepository,
        IModelMetricsRepository modelMetricsRepository,
        IProviderHealthMonitorService healthMonitorService,
        ILLMProviderFactory providerFactory,
        ILogger<AdminDashboardController> logger)
    {
        _tokenUsageService = tokenUsageService;
        _routingRepository = routingRepository;
        _modelMetricsRepository = modelMetricsRepository;
        _healthMonitorService = healthMonitorService;
        _providerFactory = providerFactory;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets dashboard summary statistics
    /// </summary>
    /// <returns>Summary dashboard data</returns>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardSummary()
    {
        try
        {
            var now = DateTime.UtcNow;
            
            // Get token usage for the last 30 days
            var tokenUsage = await _tokenUsageService.GetTokenUsageAsync(
                now.AddDays(-30), now);
            
            // Get provider health status
            var providerHealth = await _healthMonitorService.GetProviderHealthStatusAsync();
            
            // Get recent routing decisions
            var routingDecisions = await _routingRepository.GetRecentRoutingDecisionsAsync(20);
            
            // Get model metrics
            var allModels = await _providerFactory.GetAllModelsAsync();
            var modelMetrics = await _modelMetricsRepository.GetModelMetricsAsync(
                allModels.Select(m => m.Id).ToList());
            
            // Calculate success rates by provider
            var providerSuccessRates = modelMetrics
                .GroupBy(m => m.Provider)
                .Select(g => new
                {
                    Provider = g.Key,
                    SuccessRate = g.Sum(m => m.SuccessCount) / (double)(g.Sum(m => m.SuccessCount) + g.Sum(m => m.ErrorCount)) * 100,
                    TotalRequests = g.Sum(m => m.SuccessCount) + g.Sum(m => m.ErrorCount),
                    AverageLatencyMs = g.Average(m => m.AverageLatencyMs)
                })
                .ToList();
            
            // Calculate token usage by day
            var dailyUsage = tokenUsage.DailyUsage
                .OrderBy(d => d.Date)
                .Select(d => new
                {
                    Date = d.Date.ToString("yyyy-MM-dd"),
                    PromptTokens = d.PromptTokens,
                    CompletionTokens = d.CompletionTokens,
                    TotalTokens = d.TotalTokens
                })
                .ToList();
            
            // Prepare the dashboard data
            var dashboardData = new
            {
                Summary = new
                {
                    TotalTokens = tokenUsage.TotalTokens,
                    PromptTokens = tokenUsage.TotalPromptTokens,
                    CompletionTokens = tokenUsage.TotalCompletionTokens,
                    ActiveProviders = providerHealth.Count(p => p.Status == "Healthy"),
                    TotalProviders = providerHealth.Count,
                    AverageLatencyMs = modelMetrics.Any() ? modelMetrics.Average(m => m.AverageLatencyMs) : 0,
                    TotalRoutingDecisions = routingDecisions.Count,
                    TotalFallbacks = routingDecisions.Count(d => d.IsFallback),
                    EstimatedCost = modelMetrics.Sum(m => m.AvgCostPerRequest * (m.SuccessCount + m.ErrorCount))
                },
                ProviderHealth = providerHealth.Select(p => new
                {
                    p.ProviderName,
                    p.Status,
                    p.LatencyMs,
                    p.ErrorMessage,
                    p.LastChecked
                }).ToList(),
                ProviderMetrics = providerSuccessRates,
                TopModels = modelMetrics
                    .OrderByDescending(m => m.SuccessCount + m.ErrorCount)
                    .Take(5)
                    .Select(m => new
                    {
                        m.ModelId,
                        m.Provider,
                        m.AverageLatencyMs,
                        SuccessRate = m.SuccessCount / (double)(m.SuccessCount + m.ErrorCount) * 100,
                        TotalRequests = m.SuccessCount + m.ErrorCount,
                        m.AvgCostPerRequest
                    }).ToList(),
                RecentRoutingDecisions = routingDecisions.Select(d => new
                {
                    d.OriginalModelId,
                    d.SelectedModelId,
                    d.RoutingStrategy,
                    d.IsFallback,
                    d.FallbackReason,
                    d.LatencyMs,
                    Timestamp = d.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList(),
                DailyTokenUsage = dailyUsage
            };
            
            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error retrieving dashboard data" });
        }
    }
    
    /// <summary>
    /// Gets detailed token usage statistics
    /// </summary>
    /// <param name="startDate">The start date for the report</param>
    /// <param name="endDate">The end date for the report</param>
    /// <returns>Token usage statistics</returns>
    [HttpGet("usage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTokenUsage(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;
        
        try
        {
            var tokenUsage = await _tokenUsageService.GetTokenUsageAsync(startDate.Value, endDate.Value);
            
            // Calculate estimated cost by model
            var costByModel = tokenUsage.UsageByModel.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    var modelMapping = await _providerFactory.GetModelAsync(kvp.Key);
                    
                    if (modelMapping != null &&
                        modelMapping.Properties.TryGetValue("TokenPriceInput", out string? inputPrice) &&
                        modelMapping.Properties.TryGetValue("TokenPriceOutput", out string? outputPrice) &&
                        double.TryParse(inputPrice, out double inputPriceValue) &&
                        double.TryParse(outputPrice, out double outputPriceValue))
                    {
                        return (inputPriceValue * kvp.Value.PromptTokens) + (outputPriceValue * kvp.Value.CompletionTokens);
                    }
                    
                    return 0.0;
                });
            
            var response = new
            {
                TokenUsage = tokenUsage,
                CostByModel = costByModel,
                TotalEstimatedCost = costByModel.Values.Sum()
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token usage: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error retrieving token usage data" });
        }
    }
    
    /// <summary>
    /// Gets model performance metrics
    /// </summary>
    /// <returns>Model performance metrics</returns>
    [HttpGet("models/metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetModelMetrics()
    {
        try
        {
            var allModels = await _providerFactory.GetAllModelsAsync();
            var modelMetrics = await _modelMetricsRepository.GetModelMetricsAsync(
                allModels.Select(m => m.Id).ToList());
            
            var enrichedMetrics = modelMetrics.Select(m => new
            {
                m.ModelId,
                m.Provider,
                m.AverageLatencyMs,
                m.SuccessCount,
                m.ErrorCount,
                m.AvgCostPerRequest,
                SuccessRate = m.SuccessCount / (double)(m.SuccessCount + m.ErrorCount) * 100,
                TotalRequests = m.SuccessCount + m.ErrorCount,
                Model = allModels.FirstOrDefault(model => model.Id == m.ModelId)
            });
            
            return Ok(enrichedMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model metrics: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error retrieving model metrics" });
        }
    }
    
    /// <summary>
    /// Gets provider health status
    /// </summary>
    /// <returns>Provider health status</returns>
    [HttpGet("providers/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProviderHealth()
    {
        try
        {
            var healthStatus = await _healthMonitorService.GetProviderHealthStatusAsync();
            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider health: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error retrieving provider health status" });
        }
    }
    
    /// <summary>
    /// Gets routing statistics
    /// </summary>
    /// <param name="startDate">The start date for the report</param>
    /// <param name="endDate">The end date for the report</param>
    /// <returns>Routing statistics</returns>
    [HttpGet("routing/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRoutingStats(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;
        
        try
        {
            var routingStats = await _routingRepository.GetRoutingStatisticsAsync(startDate.Value, endDate.Value);
            return Ok(routingStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting routing statistics: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error retrieving routing statistics" });
        }
    }
    
    /// <summary>
    /// Triggers an immediate health check of all providers
    /// </summary>
    /// <returns>Provider health status</returns>
    [HttpPost("providers/health/check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunProviderHealthCheck()
    {
        try
        {
            var healthStatus = await _healthMonitorService.RunHealthChecksAsync();
            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running provider health checks: {ErrorMessage}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error running provider health checks" });
        }
    }
}

// src/LLMGateway.Infrastructure/Persistence/Repositories/IRoutingRepository.cs (updated)
using LLMGateway.Core.Routing;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

public interface IRoutingRepository
{
    Task AddRoutingDecisionAsync(RoutingDecision decision);
    Task<List<RoutingDecision>> GetUserRoutingHistoryAsync(string userId, int limit);
    Task<List<RoutingDecision>> GetRecentRoutingDecisionsAsync(int limit);
    Task<List<ProviderHealthStatus>> GetProviderHealthStatusAsync();
    Task<object> GetRoutingStatisticsAsync(DateTime startDate, DateTime endDate);
}

// src/LLMGateway.Infrastructure/Persistence/Repositories/RoutingRepository.cs (updated)
using LLMGateway.Core.Routing;
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

public class RoutingRepository : IRoutingRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<RoutingRepository> _logger;
    
    public RoutingRepository(
        LLMGatewayDbContext dbContext,
        ILogger<RoutingRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    // Existing methods...
    
    public async Task<List<RoutingDecision>> GetRecentRoutingDecisionsAsync(int limit)
    {
        try
        {
            var entities = await _dbContext.RoutingHistory
                .OrderByDescending(r => r.Timestamp)
                .Take(limit)
                .ToListAsync();
            
            return entities.Select(e => new RoutingDecision
            {
                OriginalModelId = e.OriginalModelId,
                SelectedModelId = e.SelectedModelId,
                RoutingStrategy = e.RoutingStrategy,
                UserId = e.UserId,
                RequestContent = e.RequestContent,
                RequestTokenCount = e.RequestTokenCount,
                IsFallback = e.IsFallback,
                FallbackReason = e.FallbackReason,
                LatencyMs = e.LatencyMs,
                Timestamp = e.Timestamp
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent routing decisions from database: {ErrorMessage}", ex.Message);
            return new List<RoutingDecision>();
        }
    }
    
    public async Task<object> GetRoutingStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var routingData = await _dbContext.RoutingHistory
                .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
                .ToListAsync();
            
            // Strategy usage percentages
            var strategyStats = routingData
                .GroupBy(r => r.RoutingStrategy)
                .Select(g => new
                {
                    Strategy = g.Key,
                    Count = g.Count(),
                    Percentage = routingData.Count > 0 ? g.Count() / (double)routingData.Count * 100 : 0
                })
                .OrderByDescending(s => s.Count)
                .ToList();
            
            // Fallback rate
            var fallbackRate = routingData.Count > 0 ? 
                routingData.Count(r => r.IsFallback) / (double)routingData.Count * 100 : 0;
            
            // Top original to selected model mappings
            var modelMappings = routingData
                .Where(r => r.OriginalModelId != r.SelectedModelId)
                .GroupBy(r => new { r.OriginalModelId, r.SelectedModelId })
                .Select(g => new
                {
                    OriginalModel = g.Key.OriginalModelId,
                    SelectedModel = g.Key.SelectedModelId,
                    Count = g.Count(),
                    Percentage = routingData.Count > 0 ? g.Count() / (double)routingData.Count * 100 : 0
                })
                .OrderByDescending(m => m.Count)
                .Take(10)
                .ToList();
            
            // Average latency by strategy
            var latencyByStrategy = routingData
                .GroupBy(r => r.RoutingStrategy)
                .Select(g => new
                {
                    Strategy = g.Key,
                    AverageLatencyMs = g.Average(r => r.LatencyMs)
                })
                .OrderBy(l => l.Strategy)
                .ToList();
            
            // Statistics by day
            var dailyStats = routingData
                .GroupBy(r => r.Timestamp.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    TotalRoutings = g.Count(),
                    Fallbacks = g.Count(r => r.IsFallback),
                    AverageLatencyMs = g.Average(r => r.LatencyMs),
                    TopStrategy = g.GroupBy(r => r.RoutingStrategy)
                        .OrderByDescending(sg => sg.Count())
                        .First().Key
                })
                .OrderBy(d => d.Date)
                .ToList();
            
            return new
            {
                TotalRoutingDecisions = routingData.Count,
                StrategyUsage = strategyStats,
                FallbackRate = fallbackRate,
                TopModelMappings = modelMappings,
                LatencyByStrategy = latencyByStrategy,
                DailyStatistics = dailyStats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving routing statistics from database: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}
