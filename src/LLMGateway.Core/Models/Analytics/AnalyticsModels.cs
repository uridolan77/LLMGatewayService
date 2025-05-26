namespace LLMGateway.Core.Models.Analytics;

/// <summary>
/// Base analytics request
/// </summary>
public class AnalyticsRequest
{
    /// <summary>
    /// User ID (optional for admin requests)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Start date for analytics
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for analytics
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Granularity (hour, day, week, month)
    /// </summary>
    public string Granularity { get; set; } = "day";

    /// <summary>
    /// Filters to apply
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();
}

/// <summary>
/// Usage analytics response
/// </summary>
public class UsageAnalytics
{
    /// <summary>
    /// Total requests
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Usage by time period
    /// </summary>
    public List<TimeSeriesDataPoint> UsageOverTime { get; set; } = new();

    /// <summary>
    /// Usage by provider
    /// </summary>
    public Dictionary<string, ProviderUsage> UsageByProvider { get; set; } = new();

    /// <summary>
    /// Usage by model
    /// </summary>
    public Dictionary<string, ModelUsage> UsageByModel { get; set; } = new();

    /// <summary>
    /// Top users by usage
    /// </summary>
    public List<UserUsage> TopUsers { get; set; } = new();
}

/// <summary>
/// Cost analytics request
/// </summary>
public class CostAnalyticsRequest : AnalyticsRequest
{
    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Include forecasting
    /// </summary>
    public bool IncludeForecast { get; set; } = false;
}

/// <summary>
/// Cost analytics response
/// </summary>
public class CostAnalytics
{
    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Cost over time
    /// </summary>
    public List<CostDataPoint> CostOverTime { get; set; } = new();

    /// <summary>
    /// Cost by provider
    /// </summary>
    public Dictionary<string, decimal> CostByProvider { get; set; } = new();

    /// <summary>
    /// Cost by model
    /// </summary>
    public Dictionary<string, decimal> CostByModel { get; set; } = new();

    /// <summary>
    /// Cost trends
    /// </summary>
    public CostTrends Trends { get; set; } = new();

    /// <summary>
    /// Cost forecast (if requested)
    /// </summary>
    public CostForecast? Forecast { get; set; }
}

/// <summary>
/// Performance analytics request
/// </summary>
public class PerformanceAnalyticsRequest : AnalyticsRequest
{
    /// <summary>
    /// Include percentiles
    /// </summary>
    public bool IncludePercentiles { get; set; } = true;

    /// <summary>
    /// Include error analysis
    /// </summary>
    public bool IncludeErrorAnalysis { get; set; } = true;
}

/// <summary>
/// Performance analytics response
/// </summary>
public class PerformanceAnalytics
{
    /// <summary>
    /// Average response time
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Median response time
    /// </summary>
    public double MedianResponseTime { get; set; }

    /// <summary>
    /// Response time percentiles
    /// </summary>
    public Dictionary<string, double> ResponseTimePercentiles { get; set; } = new();

    /// <summary>
    /// Throughput (requests per second)
    /// </summary>
    public double Throughput { get; set; }

    /// <summary>
    /// Error rate
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Performance over time
    /// </summary>
    public List<PerformanceDataPoint> PerformanceOverTime { get; set; } = new();

    /// <summary>
    /// Performance by provider
    /// </summary>
    public Dictionary<string, ProviderPerformance> PerformanceByProvider { get; set; } = new();

    /// <summary>
    /// Error analysis
    /// </summary>
    public ErrorAnalysis? ErrorAnalysis { get; set; }
}

/// <summary>
/// Time series data point
/// </summary>
public class TimeSeriesDataPoint
{
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Cost data point
/// </summary>
public class CostDataPoint : TimeSeriesDataPoint
{
    /// <summary>
    /// Cost value
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Token count
    /// </summary>
    public long TokenCount { get; set; }
}

/// <summary>
/// Performance data point
/// </summary>
public class PerformanceDataPoint : TimeSeriesDataPoint
{
    /// <summary>
    /// Response time
    /// </summary>
    public double ResponseTime { get; set; }

    /// <summary>
    /// Request count
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Error count
    /// </summary>
    public long ErrorCount { get; set; }
}

/// <summary>
/// Provider usage statistics
/// </summary>
public class ProviderUsage
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Request count
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Token count
    /// </summary>
    public long TokenCount { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average response time
    /// </summary>
    public double AverageResponseTime { get; set; }
}

/// <summary>
/// Model usage statistics
/// </summary>
public class ModelUsage
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Request count
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Token count
    /// </summary>
    public long TokenCount { get; set; }

    /// <summary>
    /// Average cost per request
    /// </summary>
    public decimal AverageCostPerRequest { get; set; }
}

/// <summary>
/// User usage statistics
/// </summary>
public class UserUsage
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Request count
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Token count
    /// </summary>
    public long TokenCount { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Last activity
    /// </summary>
    public DateTime LastActivity { get; set; }
}

/// <summary>
/// Provider performance statistics
/// </summary>
public class ProviderPerformance
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Average response time
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Throughput
    /// </summary>
    public double Throughput { get; set; }

    /// <summary>
    /// Availability percentage
    /// </summary>
    public double Availability { get; set; }
}

/// <summary>
/// Error analysis
/// </summary>
public class ErrorAnalysis
{
    /// <summary>
    /// Total errors
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// Error rate
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Errors by type
    /// </summary>
    public Dictionary<string, long> ErrorsByType { get; set; } = new();

    /// <summary>
    /// Errors by provider
    /// </summary>
    public Dictionary<string, long> ErrorsByProvider { get; set; } = new();

    /// <summary>
    /// Most common errors
    /// </summary>
    public List<ErrorSummary> MostCommonErrors { get; set; } = new();
}

/// <summary>
/// Error summary
/// </summary>
public class ErrorSummary
{
    /// <summary>
    /// Error type
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Occurrence count
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// First occurrence
    /// </summary>
    public DateTime FirstOccurrence { get; set; }

    /// <summary>
    /// Last occurrence
    /// </summary>
    public DateTime LastOccurrence { get; set; }
}

/// <summary>
/// Real-time dashboard data
/// </summary>
public class RealTimeDashboard
{
    /// <summary>
    /// Current active requests
    /// </summary>
    public long ActiveRequests { get; set; }

    /// <summary>
    /// Requests per minute
    /// </summary>
    public double RequestsPerMinute { get; set; }

    /// <summary>
    /// Average response time (last 5 minutes)
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Current error rate
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Provider health status
    /// </summary>
    public Dictionary<string, bool> ProviderHealth { get; set; } = new();

    /// <summary>
    /// Recent activity
    /// </summary>
    public List<RecentActivity> RecentActivity { get; set; } = new();

    /// <summary>
    /// System alerts
    /// </summary>
    public List<SystemAlert> SystemAlerts { get; set; } = new();

    /// <summary>
    /// Resource utilization
    /// </summary>
    public ResourceUtilization ResourceUtilization { get; set; } = new();
}

/// <summary>
/// Recent activity item
/// </summary>
public class RecentActivity
{
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Activity type
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Provider
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// System alert
/// </summary>
public class SystemAlert
{
    /// <summary>
    /// Alert ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Alert type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Severity level
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Is acknowledged
    /// </summary>
    public bool IsAcknowledged { get; set; }
}

/// <summary>
/// Resource utilization
/// </summary>
public class ResourceUtilization
{
    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Memory usage percentage
    /// </summary>
    public double MemoryUsage { get; set; }

    /// <summary>
    /// Network I/O
    /// </summary>
    public NetworkIO NetworkIO { get; set; } = new();

    /// <summary>
    /// Cache hit rate
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Queue depth
    /// </summary>
    public long QueueDepth { get; set; }
}

/// <summary>
/// Network I/O statistics
/// </summary>
public class NetworkIO
{
    /// <summary>
    /// Bytes received per second
    /// </summary>
    public long BytesReceivedPerSecond { get; set; }

    /// <summary>
    /// Bytes sent per second
    /// </summary>
    public long BytesSentPerSecond { get; set; }

    /// <summary>
    /// Connections per second
    /// </summary>
    public double ConnectionsPerSecond { get; set; }
}

/// <summary>
/// Anomaly detection request
/// </summary>
public class AnomalyDetectionRequest : AnalyticsRequest
{
    /// <summary>
    /// Metrics to analyze
    /// </summary>
    public List<string> Metrics { get; set; } = new();

    /// <summary>
    /// Sensitivity level (1-10)
    /// </summary>
    public int Sensitivity { get; set; } = 5;

    /// <summary>
    /// Minimum anomaly score threshold
    /// </summary>
    public double MinAnomalyScore { get; set; } = 0.7;
}

/// <summary>
/// Anomaly detection result
/// </summary>
public class AnomalyDetectionResult
{
    /// <summary>
    /// Detected anomalies
    /// </summary>
    public List<Anomaly> Anomalies { get; set; } = new();

    /// <summary>
    /// Analysis summary
    /// </summary>
    public AnomalySummary Summary { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Provider comparison request
/// </summary>
public class ProviderComparisonRequest
{
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Providers to compare
    /// </summary>
    public List<string> Providers { get; set; } = new();

    /// <summary>
    /// Metrics to compare
    /// </summary>
    public List<string> Metrics { get; set; } = new();

    /// <summary>
    /// Include cost analysis
    /// </summary>
    public bool IncludeCostAnalysis { get; set; } = true;

    /// <summary>
    /// Include performance analysis
    /// </summary>
    public bool IncludePerformanceAnalysis { get; set; } = true;

    /// <summary>
    /// Include reliability analysis
    /// </summary>
    public bool IncludeReliabilityAnalysis { get; set; } = true;
}

/// <summary>
/// Provider comparison result
/// </summary>
public class ProviderComparison
{
    /// <summary>
    /// Comparison summary
    /// </summary>
    public ProviderComparisonSummary Summary { get; set; } = new();

    /// <summary>
    /// Provider rankings
    /// </summary>
    public List<ProviderRanking> Rankings { get; set; } = new();

    /// <summary>
    /// Detailed comparisons
    /// </summary>
    public Dictionary<string, ProviderMetrics> DetailedComparisons { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<ProviderRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// Cost comparison
    /// </summary>
    public ProviderCostComparison? CostComparison { get; set; }

    /// <summary>
    /// Performance comparison
    /// </summary>
    public ProviderPerformanceComparison? PerformanceComparison { get; set; }
}

/// <summary>
/// Provider comparison summary
/// </summary>
public class ProviderComparisonSummary
{
    /// <summary>
    /// Best overall provider
    /// </summary>
    public string BestOverallProvider { get; set; } = string.Empty;

    /// <summary>
    /// Most cost-effective provider
    /// </summary>
    public string MostCostEffectiveProvider { get; set; } = string.Empty;

    /// <summary>
    /// Fastest provider
    /// </summary>
    public string FastestProvider { get; set; } = string.Empty;

    /// <summary>
    /// Most reliable provider
    /// </summary>
    public string MostReliableProvider { get; set; } = string.Empty;

    /// <summary>
    /// Total providers compared
    /// </summary>
    public int TotalProvidersCompared { get; set; }

    /// <summary>
    /// Comparison period
    /// </summary>
    public string ComparisonPeriod { get; set; } = string.Empty;
}

/// <summary>
/// Provider ranking
/// </summary>
public class ProviderRanking
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Overall rank
    /// </summary>
    public int OverallRank { get; set; }

    /// <summary>
    /// Overall score (0-100)
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// Cost rank
    /// </summary>
    public int CostRank { get; set; }

    /// <summary>
    /// Performance rank
    /// </summary>
    public int PerformanceRank { get; set; }

    /// <summary>
    /// Reliability rank
    /// </summary>
    public int ReliabilityRank { get; set; }

    /// <summary>
    /// Strengths
    /// </summary>
    public List<string> Strengths { get; set; } = new();

    /// <summary>
    /// Weaknesses
    /// </summary>
    public List<string> Weaknesses { get; set; } = new();
}

/// <summary>
/// Provider metrics
/// </summary>
public class ProviderMetrics
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Total requests
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average response time
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Cost per request
    /// </summary>
    public decimal CostPerRequest { get; set; }

    /// <summary>
    /// Uptime percentage
    /// </summary>
    public double UptimePercentage { get; set; }

    /// <summary>
    /// Error rate
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Throughput (requests per second)
    /// </summary>
    public double Throughput { get; set; }
}

/// <summary>
/// Provider recommendation
/// </summary>
public class ProviderRecommendation
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Recommendation type
    /// </summary>
    public string RecommendationType { get; set; } = string.Empty;

    /// <summary>
    /// Use case
    /// </summary>
    public string UseCase { get; set; } = string.Empty;

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Confidence level (0-1)
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Expected benefits
    /// </summary>
    public List<string> ExpectedBenefits { get; set; } = new();
}

/// <summary>
/// Provider cost comparison
/// </summary>
public class ProviderCostComparison
{
    /// <summary>
    /// Cost by provider
    /// </summary>
    public Dictionary<string, decimal> CostByProvider { get; set; } = new();

    /// <summary>
    /// Cost per request by provider
    /// </summary>
    public Dictionary<string, decimal> CostPerRequestByProvider { get; set; } = new();

    /// <summary>
    /// Cost trends by provider
    /// </summary>
    public Dictionary<string, List<CostTrendPoint>> CostTrendsByProvider { get; set; } = new();

    /// <summary>
    /// Most cost-effective provider
    /// </summary>
    public string MostCostEffectiveProvider { get; set; } = string.Empty;

    /// <summary>
    /// Potential savings
    /// </summary>
    public decimal PotentialSavings { get; set; }
}

/// <summary>
/// Cost trend point
/// </summary>
public class CostTrendPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Cost
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Request count
    /// </summary>
    public long RequestCount { get; set; }
}

/// <summary>
/// Provider performance comparison
/// </summary>
public class ProviderPerformanceComparison
{
    /// <summary>
    /// Response time by provider
    /// </summary>
    public Dictionary<string, double> ResponseTimeByProvider { get; set; } = new();

    /// <summary>
    /// Throughput by provider
    /// </summary>
    public Dictionary<string, double> ThroughputByProvider { get; set; } = new();

    /// <summary>
    /// Success rate by provider
    /// </summary>
    public Dictionary<string, double> SuccessRateByProvider { get; set; } = new();

    /// <summary>
    /// Fastest provider
    /// </summary>
    public string FastestProvider { get; set; } = string.Empty;

    /// <summary>
    /// Most reliable provider
    /// </summary>
    public string MostReliableProvider { get; set; } = string.Empty;

    /// <summary>
    /// Performance trends by provider
    /// </summary>
    public Dictionary<string, List<PerformanceTrendPoint>> PerformanceTrendsByProvider { get; set; } = new();
}

/// <summary>
/// Performance trend point
/// </summary>
public class PerformanceTrendPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Response time
    /// </summary>
    public double ResponseTime { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Throughput
    /// </summary>
    public double Throughput { get; set; }
}

/// <summary>
/// Model analytics request
/// </summary>
public class ModelAnalyticsRequest
{
    /// <summary>
    /// Model IDs to analyze
    /// </summary>
    public List<string> ModelIds { get; set; } = new();

    /// <summary>
    /// Start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Include usage analytics
    /// </summary>
    public bool IncludeUsageAnalytics { get; set; } = true;

    /// <summary>
    /// Include performance analytics
    /// </summary>
    public bool IncludePerformanceAnalytics { get; set; } = true;

    /// <summary>
    /// Include cost analytics
    /// </summary>
    public bool IncludeCostAnalytics { get; set; } = true;

    /// <summary>
    /// Include quality analytics
    /// </summary>
    public bool IncludeQualityAnalytics { get; set; } = false;

    /// <summary>
    /// Granularity
    /// </summary>
    public string Granularity { get; set; } = "day";
}

/// <summary>
/// Model analytics result
/// </summary>
public class ModelAnalytics
{
    /// <summary>
    /// Model analytics by model ID
    /// </summary>
    public Dictionary<string, ModelMetrics> ModelMetrics { get; set; } = new();

    /// <summary>
    /// Model comparison
    /// </summary>
    public ModelComparison Comparison { get; set; } = new();

    /// <summary>
    /// Model rankings
    /// </summary>
    public List<ModelRanking> Rankings { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<ModelRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// Usage trends
    /// </summary>
    public Dictionary<string, List<ModelUsageTrend>> UsageTrends { get; set; } = new();
}

/// <summary>
/// Model metrics
/// </summary>
public class ModelMetrics
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Total requests
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average response time
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Total tokens processed
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Cost per token
    /// </summary>
    public decimal CostPerToken { get; set; }

    /// <summary>
    /// Average quality score
    /// </summary>
    public double? AverageQualityScore { get; set; }

    /// <summary>
    /// Error rate
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Popularity score
    /// </summary>
    public double PopularityScore { get; set; }
}

/// <summary>
/// Model comparison
/// </summary>
public class ModelComparison
{
    /// <summary>
    /// Best performing model
    /// </summary>
    public string BestPerformingModel { get; set; } = string.Empty;

    /// <summary>
    /// Most cost-effective model
    /// </summary>
    public string MostCostEffectiveModel { get; set; } = string.Empty;

    /// <summary>
    /// Most popular model
    /// </summary>
    public string MostPopularModel { get; set; } = string.Empty;

    /// <summary>
    /// Fastest model
    /// </summary>
    public string FastestModel { get; set; } = string.Empty;

    /// <summary>
    /// Highest quality model
    /// </summary>
    public string? HighestQualityModel { get; set; }

    /// <summary>
    /// Performance comparison matrix
    /// </summary>
    public Dictionary<string, Dictionary<string, double>> PerformanceMatrix { get; set; } = new();
}

/// <summary>
/// Model ranking
/// </summary>
public class ModelRanking
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Overall rank
    /// </summary>
    public int OverallRank { get; set; }

    /// <summary>
    /// Overall score
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// Performance rank
    /// </summary>
    public int PerformanceRank { get; set; }

    /// <summary>
    /// Cost rank
    /// </summary>
    public int CostRank { get; set; }

    /// <summary>
    /// Popularity rank
    /// </summary>
    public int PopularityRank { get; set; }

    /// <summary>
    /// Quality rank
    /// </summary>
    public int? QualityRank { get; set; }
}

/// <summary>
/// Model recommendation
/// </summary>
public class ModelRecommendation
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Recommendation type
    /// </summary>
    public string RecommendationType { get; set; } = string.Empty;

    /// <summary>
    /// Use case
    /// </summary>
    public string UseCase { get; set; } = string.Empty;

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Confidence level
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Expected benefits
    /// </summary>
    public List<string> ExpectedBenefits { get; set; } = new();
}

/// <summary>
/// Model usage trend
/// </summary>
public class ModelUsageTrend
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Request count
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Token count
    /// </summary>
    public long TokenCount { get; set; }

    /// <summary>
    /// Cost
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Average response time
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }
}

/// <summary>
/// Compliance report request
/// </summary>
public class ComplianceReportRequest
{
    /// <summary>
    /// Report type
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Compliance standards
    /// </summary>
    public List<string> ComplianceStandards { get; set; } = new();

    /// <summary>
    /// Include detailed findings
    /// </summary>
    public bool IncludeDetailedFindings { get; set; } = true;

    /// <summary>
    /// Include recommendations
    /// </summary>
    public bool IncludeRecommendations { get; set; } = true;

    /// <summary>
    /// Export format
    /// </summary>
    public string ExportFormat { get; set; } = "PDF";
}

/// <summary>
/// Compliance report
/// </summary>
public class ComplianceReport
{
    /// <summary>
    /// Report ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Report type
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Report period
    /// </summary>
    public DateRange ReportPeriod { get; set; } = new();

    /// <summary>
    /// Overall compliance score
    /// </summary>
    public double OverallComplianceScore { get; set; }

    /// <summary>
    /// Compliance status
    /// </summary>
    public string ComplianceStatus { get; set; } = string.Empty;

    /// <summary>
    /// Standards assessed
    /// </summary>
    public List<ComplianceStandard> StandardsAssessed { get; set; } = new();

    /// <summary>
    /// Findings
    /// </summary>
    public List<ComplianceFinding> Findings { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<ComplianceRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// Executive summary
    /// </summary>
    public string ExecutiveSummary { get; set; } = string.Empty;

    /// <summary>
    /// Download URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;
}

/// <summary>
/// Date range
/// </summary>
public class DateRange
{
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Compliance standard
/// </summary>
public class ComplianceStandard
{
    /// <summary>
    /// Standard name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Compliance score
    /// </summary>
    public double ComplianceScore { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Requirements assessed
    /// </summary>
    public int RequirementsAssessed { get; set; }

    /// <summary>
    /// Requirements passed
    /// </summary>
    public int RequirementsPassed { get; set; }

    /// <summary>
    /// Requirements failed
    /// </summary>
    public int RequirementsFailed { get; set; }
}

/// <summary>
/// Compliance finding
/// </summary>
public class ComplianceFinding
{
    /// <summary>
    /// Finding ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Standard
    /// </summary>
    public string Standard { get; set; } = string.Empty;

    /// <summary>
    /// Requirement
    /// </summary>
    public string Requirement { get; set; } = string.Empty;

    /// <summary>
    /// Severity
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Evidence
    /// </summary>
    public List<string> Evidence { get; set; } = new();

    /// <summary>
    /// Remediation steps
    /// </summary>
    public List<string> RemediationSteps { get; set; } = new();

    /// <summary>
    /// Due date
    /// </summary>
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// Compliance recommendation
/// </summary>
public class ComplianceRecommendation
{
    /// <summary>
    /// Recommendation ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Priority
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Implementation effort
    /// </summary>
    public string ImplementationEffort { get; set; } = string.Empty;

    /// <summary>
    /// Expected impact
    /// </summary>
    public string ExpectedImpact { get; set; } = string.Empty;

    /// <summary>
    /// Implementation steps
    /// </summary>
    public List<string> ImplementationSteps { get; set; } = new();

    /// <summary>
    /// Related standards
    /// </summary>
    public List<string> RelatedStandards { get; set; } = new();
}

/// <summary>
/// Cost trends
/// </summary>
public class CostTrends
{
    /// <summary>
    /// Period over period change
    /// </summary>
    public decimal PeriodOverPeriodChange { get; set; }

    /// <summary>
    /// Period over period percentage change
    /// </summary>
    public double PeriodOverPeriodPercentageChange { get; set; }

    /// <summary>
    /// Trend direction (increasing, decreasing, stable)
    /// </summary>
    public string TrendDirection { get; set; } = string.Empty;

    /// <summary>
    /// Average daily cost
    /// </summary>
    public decimal AverageDailyCost { get; set; }

    /// <summary>
    /// Peak cost day
    /// </summary>
    public DateTime? PeakCostDay { get; set; }

    /// <summary>
    /// Peak cost amount
    /// </summary>
    public decimal PeakCostAmount { get; set; }

    /// <summary>
    /// Cost volatility (standard deviation)
    /// </summary>
    public double CostVolatility { get; set; }

    /// <summary>
    /// Monthly trends
    /// </summary>
    public List<MonthlyTrend> MonthlyTrends { get; set; } = new();

    /// <summary>
    /// Weekly trends
    /// </summary>
    public List<WeeklyTrend> WeeklyTrends { get; set; } = new();

    /// <summary>
    /// Daily trends
    /// </summary>
    public List<DailyTrend> DailyTrends { get; set; } = new();
}

/// <summary>
/// Monthly trend
/// </summary>
public class MonthlyTrend
{
    /// <summary>
    /// Month
    /// </summary>
    public DateTime Month { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Change from previous month
    /// </summary>
    public decimal ChangeFromPreviousMonth { get; set; }

    /// <summary>
    /// Percentage change
    /// </summary>
    public double PercentageChange { get; set; }
}

/// <summary>
/// Weekly trend
/// </summary>
public class WeeklyTrend
{
    /// <summary>
    /// Week start date
    /// </summary>
    public DateTime WeekStartDate { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Change from previous week
    /// </summary>
    public decimal ChangeFromPreviousWeek { get; set; }

    /// <summary>
    /// Percentage change
    /// </summary>
    public double PercentageChange { get; set; }
}

/// <summary>
/// Daily trend
/// </summary>
public class DailyTrend
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Change from previous day
    /// </summary>
    public decimal ChangeFromPreviousDay { get; set; }

    /// <summary>
    /// Percentage change
    /// </summary>
    public double PercentageChange { get; set; }
}

/// <summary>
/// Cost forecast
/// </summary>
public class CostForecast
{
    /// <summary>
    /// Forecast period start
    /// </summary>
    public DateTime ForecastStart { get; set; }

    /// <summary>
    /// Forecast period end
    /// </summary>
    public DateTime ForecastEnd { get; set; }

    /// <summary>
    /// Predicted total cost
    /// </summary>
    public decimal PredictedTotalCost { get; set; }

    /// <summary>
    /// Confidence level (0-1)
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Daily forecast points
    /// </summary>
    public List<CostForecastPoint> DailyForecasts { get; set; } = new();

    /// <summary>
    /// Forecast methodology
    /// </summary>
    public string Methodology { get; set; } = string.Empty;

    /// <summary>
    /// Factors considered
    /// </summary>
    public List<string> FactorsConsidered { get; set; } = new();

    /// <summary>
    /// Accuracy metrics
    /// </summary>
    public ForecastAccuracyMetrics? AccuracyMetrics { get; set; }
}

/// <summary>
/// Cost forecast point
/// </summary>
public class CostForecastPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Predicted cost
    /// </summary>
    public decimal PredictedCost { get; set; }

    /// <summary>
    /// Lower bound
    /// </summary>
    public decimal LowerBound { get; set; }

    /// <summary>
    /// Upper bound
    /// </summary>
    public decimal UpperBound { get; set; }

    /// <summary>
    /// Confidence level
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// Forecast accuracy metrics
/// </summary>
public class ForecastAccuracyMetrics
{
    /// <summary>
    /// Mean absolute error
    /// </summary>
    public double MeanAbsoluteError { get; set; }

    /// <summary>
    /// Mean absolute percentage error
    /// </summary>
    public double MeanAbsolutePercentageError { get; set; }

    /// <summary>
    /// Root mean square error
    /// </summary>
    public double RootMeanSquareError { get; set; }

    /// <summary>
    /// R-squared
    /// </summary>
    public double RSquared { get; set; }
}

/// <summary>
/// User analytics request
/// </summary>
public class UserAnalyticsRequest
{
    /// <summary>
    /// User ID (optional, if not provided returns aggregated data)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Include behavior analysis
    /// </summary>
    public bool IncludeBehaviorAnalysis { get; set; } = true;

    /// <summary>
    /// Include usage patterns
    /// </summary>
    public bool IncludeUsagePatterns { get; set; } = true;

    /// <summary>
    /// Include cost analysis
    /// </summary>
    public bool IncludeCostAnalysis { get; set; } = true;

    /// <summary>
    /// Include performance metrics
    /// </summary>
    public bool IncludePerformanceMetrics { get; set; } = true;
}

/// <summary>
/// User analytics
/// </summary>
public class UserAnalytics
{
    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Total requests
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Average requests per day
    /// </summary>
    public double AverageRequestsPerDay { get; set; }

    /// <summary>
    /// Most used models
    /// </summary>
    public List<ModelUsageStats> MostUsedModels { get; set; } = new();

    /// <summary>
    /// Most used providers
    /// </summary>
    public List<ProviderUsageStats> MostUsedProviders { get; set; } = new();

    /// <summary>
    /// Usage patterns
    /// </summary>
    public UsagePatterns? UsagePatterns { get; set; }

    /// <summary>
    /// Behavior analysis
    /// </summary>
    public UserBehaviorAnalysis? BehaviorAnalysis { get; set; }

    /// <summary>
    /// Cost analysis
    /// </summary>
    public UserCostAnalysis? CostAnalysis { get; set; }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public UserPerformanceMetrics? PerformanceMetrics { get; set; }
}

/// <summary>
/// Model usage stats
/// </summary>
public class ModelUsageStats
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Request count
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Token count
    /// </summary>
    public long TokenCount { get; set; }

    /// <summary>
    /// Cost
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Percentage of total usage
    /// </summary>
    public double PercentageOfTotal { get; set; }
}

/// <summary>
/// Provider usage stats
/// </summary>
public class ProviderUsageStats
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Request count
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Token count
    /// </summary>
    public long TokenCount { get; set; }

    /// <summary>
    /// Cost
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Percentage of total usage
    /// </summary>
    public double PercentageOfTotal { get; set; }

    /// <summary>
    /// Average response time
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }
}

/// <summary>
/// Usage patterns
/// </summary>
public class UsagePatterns
{
    /// <summary>
    /// Peak usage hours
    /// </summary>
    public List<int> PeakUsageHours { get; set; } = new();

    /// <summary>
    /// Peak usage days
    /// </summary>
    public List<DayOfWeek> PeakUsageDays { get; set; } = new();

    /// <summary>
    /// Hourly usage distribution
    /// </summary>
    public Dictionary<int, long> HourlyUsageDistribution { get; set; } = new();

    /// <summary>
    /// Daily usage distribution
    /// </summary>
    public Dictionary<DayOfWeek, long> DailyUsageDistribution { get; set; } = new();

    /// <summary>
    /// Monthly usage trends
    /// </summary>
    public List<MonthlyUsageTrend> MonthlyTrends { get; set; } = new();

    /// <summary>
    /// Seasonal patterns
    /// </summary>
    public SeasonalPatterns SeasonalPatterns { get; set; } = new();
}

/// <summary>
/// Monthly usage trend
/// </summary>
public class MonthlyUsageTrend
{
    /// <summary>
    /// Month
    /// </summary>
    public DateTime Month { get; set; }

    /// <summary>
    /// Request count
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Token count
    /// </summary>
    public long TokenCount { get; set; }

    /// <summary>
    /// Cost
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Growth rate
    /// </summary>
    public double GrowthRate { get; set; }
}

/// <summary>
/// Seasonal patterns
/// </summary>
public class SeasonalPatterns
{
    /// <summary>
    /// Spring usage
    /// </summary>
    public SeasonUsage Spring { get; set; } = new();

    /// <summary>
    /// Summer usage
    /// </summary>
    public SeasonUsage Summer { get; set; } = new();

    /// <summary>
    /// Fall usage
    /// </summary>
    public SeasonUsage Fall { get; set; } = new();

    /// <summary>
    /// Winter usage
    /// </summary>
    public SeasonUsage Winter { get; set; } = new();
}

/// <summary>
/// Season usage
/// </summary>
public class SeasonUsage
{
    /// <summary>
    /// Average daily requests
    /// </summary>
    public double AverageDailyRequests { get; set; }

    /// <summary>
    /// Peak day
    /// </summary>
    public DateTime? PeakDay { get; set; }

    /// <summary>
    /// Peak requests
    /// </summary>
    public long PeakRequests { get; set; }
}

/// <summary>
/// User behavior analysis
/// </summary>
public class UserBehaviorAnalysis
{
    /// <summary>
    /// User type (power_user, regular_user, occasional_user)
    /// </summary>
    public string UserType { get; set; } = string.Empty;

    /// <summary>
    /// Engagement score (0-100)
    /// </summary>
    public double EngagementScore { get; set; }

    /// <summary>
    /// Preferred models
    /// </summary>
    public List<string> PreferredModels { get; set; } = new();

    /// <summary>
    /// Preferred providers
    /// </summary>
    public List<string> PreferredProviders { get; set; } = new();

    /// <summary>
    /// Usage consistency score
    /// </summary>
    public double UsageConsistencyScore { get; set; }

    /// <summary>
    /// Experimentation tendency
    /// </summary>
    public double ExperimentationTendency { get; set; }

    /// <summary>
    /// Cost sensitivity
    /// </summary>
    public string CostSensitivity { get; set; } = string.Empty;

    /// <summary>
    /// Behavioral insights
    /// </summary>
    public List<string> BehavioralInsights { get; set; } = new();
}

/// <summary>
/// User cost analysis
/// </summary>
public class UserCostAnalysis
{
    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Average cost per request
    /// </summary>
    public decimal AverageCostPerRequest { get; set; }

    /// <summary>
    /// Cost trend
    /// </summary>
    public string CostTrend { get; set; } = string.Empty;

    /// <summary>
    /// Cost efficiency score
    /// </summary>
    public double CostEfficiencyScore { get; set; }

    /// <summary>
    /// Potential savings
    /// </summary>
    public decimal PotentialSavings { get; set; }

    /// <summary>
    /// Cost optimization opportunities
    /// </summary>
    public List<string> OptimizationOpportunities { get; set; } = new();
}

/// <summary>
/// User performance metrics
/// </summary>
public class UserPerformanceMetrics
{
    /// <summary>
    /// Average response time
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Error rate
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Throughput (requests per hour)
    /// </summary>
    public double Throughput { get; set; }

    /// <summary>
    /// Performance score
    /// </summary>
    public double PerformanceScore { get; set; }

    /// <summary>
    /// Performance trends
    /// </summary>
    public List<PerformanceTrendPoint> PerformanceTrends { get; set; } = new();
}

/// <summary>
/// Predictive analytics request
/// </summary>
public class PredictiveAnalyticsRequest
{
    /// <summary>
    /// Prediction type (usage, cost, performance, demand)
    /// </summary>
    public string PredictionType { get; set; } = string.Empty;

    /// <summary>
    /// Prediction horizon (days)
    /// </summary>
    public int PredictionHorizonDays { get; set; } = 30;

    /// <summary>
    /// Historical data period (days)
    /// </summary>
    public int HistoricalDataPeriodDays { get; set; } = 90;

    /// <summary>
    /// Confidence level
    /// </summary>
    public double ConfidenceLevel { get; set; } = 0.95;

    /// <summary>
    /// Include seasonal adjustments
    /// </summary>
    public bool IncludeSeasonalAdjustments { get; set; } = true;

    /// <summary>
    /// Include external factors
    /// </summary>
    public bool IncludeExternalFactors { get; set; } = false;

    /// <summary>
    /// Granularity (hour, day, week)
    /// </summary>
    public string Granularity { get; set; } = "day";
}

/// <summary>
/// Predictive analytics
/// </summary>
public class PredictiveAnalytics
{
    /// <summary>
    /// Prediction type
    /// </summary>
    public string PredictionType { get; set; } = string.Empty;

    /// <summary>
    /// Prediction horizon
    /// </summary>
    public TimeSpan PredictionHorizon { get; set; }

    /// <summary>
    /// Confidence level
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Model accuracy
    /// </summary>
    public double ModelAccuracy { get; set; }

    /// <summary>
    /// Predictions
    /// </summary>
    public List<PredictionPoint> Predictions { get; set; } = new();

    /// <summary>
    /// Trends
    /// </summary>
    public PredictiveTrends Trends { get; set; } = new();

    /// <summary>
    /// Risk factors
    /// </summary>
    public List<RiskFactor> RiskFactors { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<PredictiveRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// Model metadata
    /// </summary>
    public PredictiveModelMetadata ModelMetadata { get; set; } = new();
}

/// <summary>
/// Prediction point
/// </summary>
public class PredictionPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Predicted value
    /// </summary>
    public double PredictedValue { get; set; }

    /// <summary>
    /// Lower bound
    /// </summary>
    public double LowerBound { get; set; }

    /// <summary>
    /// Upper bound
    /// </summary>
    public double UpperBound { get; set; }

    /// <summary>
    /// Confidence
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Factors
    /// </summary>
    public Dictionary<string, double> Factors { get; set; } = new();
}

/// <summary>
/// Predictive trends
/// </summary>
public class PredictiveTrends
{
    /// <summary>
    /// Overall trend direction
    /// </summary>
    public string OverallTrend { get; set; } = string.Empty;

    /// <summary>
    /// Growth rate
    /// </summary>
    public double GrowthRate { get; set; }

    /// <summary>
    /// Volatility
    /// </summary>
    public double Volatility { get; set; }

    /// <summary>
    /// Seasonality strength
    /// </summary>
    public double SeasonalityStrength { get; set; }

    /// <summary>
    /// Trend changes
    /// </summary>
    public List<TrendChange> TrendChanges { get; set; } = new();
}

/// <summary>
/// Trend change
/// </summary>
public class TrendChange
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Change type
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Magnitude
    /// </summary>
    public double Magnitude { get; set; }

    /// <summary>
    /// Confidence
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// Risk factor
/// </summary>
public class RiskFactor
{
    /// <summary>
    /// Factor name
    /// </summary>
    public string FactorName { get; set; } = string.Empty;

    /// <summary>
    /// Risk level
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Impact
    /// </summary>
    public double Impact { get; set; }

    /// <summary>
    /// Probability
    /// </summary>
    public double Probability { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Mitigation strategies
    /// </summary>
    public List<string> MitigationStrategies { get; set; } = new();
}

/// <summary>
/// Predictive recommendation
/// </summary>
public class PredictiveRecommendation
{
    /// <summary>
    /// Recommendation type
    /// </summary>
    public string RecommendationType { get; set; } = string.Empty;

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Priority
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Expected impact
    /// </summary>
    public double ExpectedImpact { get; set; }

    /// <summary>
    /// Time frame
    /// </summary>
    public string TimeFrame { get; set; } = string.Empty;

    /// <summary>
    /// Action items
    /// </summary>
    public List<string> ActionItems { get; set; } = new();
}

/// <summary>
/// Predictive model metadata
/// </summary>
public class PredictiveModelMetadata
{
    /// <summary>
    /// Model type
    /// </summary>
    public string ModelType { get; set; } = string.Empty;

    /// <summary>
    /// Training data size
    /// </summary>
    public long TrainingDataSize { get; set; }

    /// <summary>
    /// Training period
    /// </summary>
    public DateRange TrainingPeriod { get; set; } = new();

    /// <summary>
    /// Model version
    /// </summary>
    public string ModelVersion { get; set; } = string.Empty;

    /// <summary>
    /// Last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Anomaly
/// </summary>
public class Anomaly
{
    /// <summary>
    /// Anomaly ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Metric name
    /// </summary>
    public string Metric { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Actual value
    /// </summary>
    public double ActualValue { get; set; }

    /// <summary>
    /// Expected value
    /// </summary>
    public double ExpectedValue { get; set; }

    /// <summary>
    /// Anomaly score (0-1)
    /// </summary>
    public double AnomalyScore { get; set; }

    /// <summary>
    /// Severity
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Potential causes
    /// </summary>
    public List<string> PotentialCauses { get; set; } = new();
}

/// <summary>
/// Anomaly summary
/// </summary>
public class AnomalySummary
{
    /// <summary>
    /// Total anomalies detected
    /// </summary>
    public int TotalAnomalies { get; set; }

    /// <summary>
    /// High severity anomalies
    /// </summary>
    public int HighSeverityAnomalies { get; set; }

    /// <summary>
    /// Medium severity anomalies
    /// </summary>
    public int MediumSeverityAnomalies { get; set; }

    /// <summary>
    /// Low severity anomalies
    /// </summary>
    public int LowSeverityAnomalies { get; set; }

    /// <summary>
    /// Most affected metrics
    /// </summary>
    public List<string> MostAffectedMetrics { get; set; } = new();
}
