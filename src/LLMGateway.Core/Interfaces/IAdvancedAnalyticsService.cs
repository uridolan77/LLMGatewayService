using LLMGateway.Core.Models.Analytics;
using LLMGateway.Core.Models.Cost;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Advanced analytics service for comprehensive insights
/// </summary>
public interface IAdvancedAnalyticsService
{
    /// <summary>
    /// Get comprehensive usage analytics
    /// </summary>
    /// <param name="request">Analytics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage analytics</returns>
    Task<UsageAnalytics> GetUsageAnalyticsAsync(AnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cost analytics and trends
    /// </summary>
    /// <param name="request">Cost analytics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cost analytics</returns>
    Task<CostAnalytics> GetCostAnalyticsAsync(CostAnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get performance analytics
    /// </summary>
    /// <param name="request">Performance analytics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance analytics</returns>
    Task<PerformanceAnalytics> GetPerformanceAnalyticsAsync(PerformanceAnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get provider comparison analytics
    /// </summary>
    /// <param name="request">Provider comparison request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider comparison</returns>
    Task<ProviderComparison> GetProviderComparisonAsync(ProviderComparisonRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get model usage patterns
    /// </summary>
    /// <param name="request">Model analytics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Model analytics</returns>
    Task<ModelAnalytics> GetModelAnalyticsAsync(ModelAnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user behavior analytics
    /// </summary>
    /// <param name="request">User analytics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User analytics</returns>
    Task<UserAnalytics> GetUserAnalyticsAsync(UserAnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time dashboard data
    /// </summary>
    /// <param name="userId">User ID (optional for admin)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Real-time dashboard data</returns>
    Task<RealTimeDashboard> GetRealTimeDashboardAsync(string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get anomaly detection results
    /// </summary>
    /// <param name="request">Anomaly detection request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Anomaly detection results</returns>
    Task<AnomalyDetectionResult> DetectAnomaliesAsync(AnomalyDetectionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get predictive analytics
    /// </summary>
    /// <param name="request">Predictive analytics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Predictive analytics</returns>
    Task<PredictiveAnalytics> GetPredictiveAnalyticsAsync(PredictiveAnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate custom report
    /// </summary>
    /// <param name="request">Custom report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Custom report</returns>
    Task<CustomReport> GenerateCustomReportAsync(CustomReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export analytics data
    /// </summary>
    /// <param name="request">Export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export data</returns>
    Task<Models.Analytics.ExportData> ExportAnalyticsAsync(ExportAnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get usage forecasting
    /// </summary>
    /// <param name="request">Forecasting request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage forecast</returns>
    Task<UsageForecast> GetUsageForecastAsync(UsageForecastRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cost optimization recommendations
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cost optimization recommendations</returns>
    Task<CostOptimizationRecommendations> GetCostOptimizationRecommendationsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get security analytics
    /// </summary>
    /// <param name="request">Security analytics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Security analytics</returns>
    Task<SecurityAnalytics> GetSecurityAnalyticsAsync(SecurityAnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get compliance report
    /// </summary>
    /// <param name="request">Compliance report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compliance report</returns>
    Task<ComplianceReport> GetComplianceReportAsync(ComplianceReportRequest request, CancellationToken cancellationToken = default);
}
