using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Cost;
using LLMGateway.Core.Models.Embedding;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for cost management service
/// </summary>
public interface ICostManagementService
{
    /// <summary>
    /// Track completion cost
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="response">Completion response</param>
    /// <param name="userId">User ID</param>
    /// <param name="requestId">Request ID</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <param name="metadata">Metadata</param>
    /// <returns>Cost record</returns>
    Task<CostRecord> TrackCompletionCostAsync(
        CompletionRequest request,
        CompletionResponse response,
        string userId,
        string requestId,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Track embedding cost
    /// </summary>
    /// <param name="request">Embedding request</param>
    /// <param name="response">Embedding response</param>
    /// <param name="userId">User ID</param>
    /// <param name="requestId">Request ID</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <param name="metadata">Metadata</param>
    /// <returns>Cost record</returns>
    Task<CostRecord> TrackEmbeddingCostAsync(
        EmbeddingRequest request,
        EmbeddingResponse response,
        string userId,
        string requestId,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Track fine-tuning cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="trainingTokens">Training tokens</param>
    /// <param name="userId">User ID</param>
    /// <param name="requestId">Request ID</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <param name="metadata">Metadata</param>
    /// <returns>Cost record</returns>
    Task<CostRecord> TrackFineTuningCostAsync(
        string provider,
        string modelId,
        int trainingTokens,
        string userId,
        string requestId,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Get cost records
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="operationType">Operation type</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <returns>Cost records</returns>
    Task<IEnumerable<CostRecord>> GetCostRecordsAsync(
        string userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? provider = null,
        string? modelId = null,
        string? operationType = null,
        string? projectId = null,
        IEnumerable<string>? tags = null);

    /// <summary>
    /// Get cost report
    /// </summary>
    /// <param name="request">Report request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Cost report</returns>
    Task<CostReport> GetCostReportAsync(CostReportRequest request, string userId);

    /// <summary>
    /// Get all budgets
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Budgets</returns>
    Task<IEnumerable<Budget>> GetAllBudgetsAsync(string userId);

    /// <summary>
    /// Get budget by ID
    /// </summary>
    /// <param name="budgetId">Budget ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Budget</returns>
    Task<Budget> GetBudgetAsync(string budgetId, string userId);

    /// <summary>
    /// Create budget
    /// </summary>
    /// <param name="request">Create request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Created budget</returns>
    Task<Budget> CreateBudgetAsync(CreateBudgetRequest request, string userId);

    /// <summary>
    /// Update budget
    /// </summary>
    /// <param name="budgetId">Budget ID</param>
    /// <param name="request">Update request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Updated budget</returns>
    Task<Budget> UpdateBudgetAsync(string budgetId, UpdateBudgetRequest request, string userId);

    /// <summary>
    /// Delete budget
    /// </summary>
    /// <param name="budgetId">Budget ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Task</returns>
    Task DeleteBudgetAsync(string budgetId, string userId);

    /// <summary>
    /// Get budget usage
    /// </summary>
    /// <param name="budgetId">Budget ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Budget usage</returns>
    Task<BudgetUsage> GetBudgetUsageAsync(string budgetId, string userId);

    /// <summary>
    /// Get all budget usages
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Budget usages</returns>
    Task<IEnumerable<BudgetUsage>> GetAllBudgetUsagesAsync(string userId);

    /// <summary>
    /// Check if operation is within budget
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="estimatedCostUsd">Estimated cost in USD</param>
    /// <returns>True if within budget</returns>
    Task<bool> IsWithinBudgetAsync(string userId, string? projectId, decimal estimatedCostUsd);

    /// <summary>
    /// Get model pricing
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <returns>Model pricing</returns>
    Task<(decimal InputPricePerToken, decimal OutputPricePerToken)> GetModelPricingAsync(string provider, string modelId);

    /// <summary>
    /// Estimate completion cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="inputTokens">Input tokens</param>
    /// <param name="outputTokens">Output tokens</param>
    /// <returns>Estimated cost in USD</returns>
    Task<decimal> EstimateCompletionCostAsync(string provider, string modelId, int inputTokens, int outputTokens);

    /// <summary>
    /// Estimate embedding cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="inputTokens">Input tokens</param>
    /// <returns>Estimated cost in USD</returns>
    Task<decimal> EstimateEmbeddingCostAsync(string provider, string modelId, int inputTokens);

    /// <summary>
    /// Estimate fine-tuning cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="trainingTokens">Training tokens</param>
    /// <returns>Estimated cost in USD</returns>
    Task<decimal> EstimateFineTuningCostAsync(string provider, string modelId, int trainingTokens);

    // Phase 3 Advanced Cost Management Features

    /// <summary>
    /// Get advanced cost analytics
    /// </summary>
    /// <param name="request">Cost analytics request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Advanced cost analytics</returns>
    Task<AdvancedCostAnalytics> GetAdvancedCostAnalyticsAsync(AdvancedCostAnalyticsRequest request, string userId);

    /// <summary>
    /// Get cost optimization recommendations
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="timeframe">Analysis timeframe</param>
    /// <returns>Cost optimization recommendations</returns>
    Task<CostOptimizationRecommendations> GetCostOptimizationRecommendationsAsync(string userId, TimeSpan timeframe);

    /// <summary>
    /// Get cost forecasting
    /// </summary>
    /// <param name="request">Cost forecast request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Cost forecast</returns>
    Task<CostForecast> GetCostForecastAsync(CostForecastRequest request, string userId);

    /// <summary>
    /// Create cost alert
    /// </summary>
    /// <param name="request">Cost alert request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Created cost alert</returns>
    Task<CostAlert> CreateCostAlertAsync(CreateCostAlertRequest request, string userId);

    /// <summary>
    /// Get cost alerts
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="isActive">Filter by active status</param>
    /// <returns>Cost alerts</returns>
    Task<IEnumerable<CostAlert>> GetCostAlertsAsync(string userId, bool? isActive = null);

    /// <summary>
    /// Update cost alert
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <param name="request">Update request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Updated cost alert</returns>
    Task<CostAlert> UpdateCostAlertAsync(string alertId, UpdateCostAlertRequest request, string userId);

    /// <summary>
    /// Delete cost alert
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Task</returns>
    Task DeleteCostAlertAsync(string alertId, string userId);

    /// <summary>
    /// Get cost anomaly detection
    /// </summary>
    /// <param name="request">Anomaly detection request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Cost anomalies</returns>
    Task<CostAnomalyDetectionResult> DetectCostAnomaliesAsync(CostAnomalyDetectionRequest request, string userId);

    /// <summary>
    /// Get cost breakdown by dimensions
    /// </summary>
    /// <param name="request">Cost breakdown request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Cost breakdown</returns>
    Task<CostBreakdown> GetCostBreakdownAsync(CostBreakdownRequest request, string userId);

    /// <summary>
    /// Get cost trends analysis
    /// </summary>
    /// <param name="request">Cost trends request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Cost trends</returns>
    Task<CostTrends> GetCostTrendsAsync(CostTrendsRequest request, string userId);

    /// <summary>
    /// Export cost data
    /// </summary>
    /// <param name="request">Export request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Export data</returns>
    Task<ExportData> ExportCostDataAsync(ExportCostDataRequest request, string userId);

    /// <summary>
    /// Get cost efficiency metrics
    /// </summary>
    /// <param name="request">Efficiency metrics request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Cost efficiency metrics</returns>
    Task<CostEfficiencyMetrics> GetCostEfficiencyMetricsAsync(CostEfficiencyRequest request, string userId);

    /// <summary>
    /// Get provider cost comparison
    /// </summary>
    /// <param name="request">Provider comparison request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Provider cost comparison</returns>
    Task<ProviderCostComparison> GetProviderCostComparisonAsync(ProviderCostComparisonRequest request, string userId);

    /// <summary>
    /// Get cost allocation by teams/projects
    /// </summary>
    /// <param name="request">Cost allocation request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Cost allocation</returns>
    Task<CostAllocation> GetCostAllocationAsync(CostAllocationRequest request, string userId);

    /// <summary>
    /// Create cost center
    /// </summary>
    /// <param name="request">Cost center request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Created cost center</returns>
    Task<CostCenter> CreateCostCenterAsync(CreateCostCenterRequest request, string userId);

    /// <summary>
    /// Get cost centers
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Cost centers</returns>
    Task<IEnumerable<CostCenter>> GetCostCentersAsync(string userId);

    /// <summary>
    /// Get real-time cost monitoring
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Real-time cost data</returns>
    Task<RealTimeCostData> GetRealTimeCostDataAsync(string userId);
}
