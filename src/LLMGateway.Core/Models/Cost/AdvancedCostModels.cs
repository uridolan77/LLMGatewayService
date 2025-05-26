namespace LLMGateway.Core.Models.Cost;

/// <summary>
/// Advanced cost analytics request
/// </summary>
public class AdvancedCostAnalyticsRequest
{
    /// <summary>
    /// User ID
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
    /// Granularity (hour, day, week, month)
    /// </summary>
    public string Granularity { get; set; } = "day";

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Include forecasting
    /// </summary>
    public bool IncludeForecast { get; set; } = false;

    /// <summary>
    /// Include breakdown by dimensions
    /// </summary>
    public List<string> BreakdownDimensions { get; set; } = new();

    /// <summary>
    /// Filters
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();
}

/// <summary>
/// Advanced cost analytics response
/// </summary>
public class AdvancedCostAnalytics
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
    /// Cost trends
    /// </summary>
    public CostTrends Trends { get; set; } = new();

    /// <summary>
    /// Cost breakdown by dimensions
    /// </summary>
    public Dictionary<string, CostBreakdown> BreakdownByDimension { get; set; } = new();

    /// <summary>
    /// Cost efficiency metrics
    /// </summary>
    public CostEfficiencyMetrics EfficiencyMetrics { get; set; } = new();

    /// <summary>
    /// Cost forecast (if requested)
    /// </summary>
    public CostForecast? Forecast { get; set; }

    /// <summary>
    /// Optimization opportunities
    /// </summary>
    public List<CostOptimizationOpportunity> OptimizationOpportunities { get; set; } = new();
}

/// <summary>
/// Cost trends analysis
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
/// Cost forecast request
/// </summary>
public class CostForecastRequest
{
    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Forecast period start
    /// </summary>
    public DateTime ForecastStart { get; set; }

    /// <summary>
    /// Forecast period end
    /// </summary>
    public DateTime ForecastEnd { get; set; }

    /// <summary>
    /// Historical data start
    /// </summary>
    public DateTime? HistoricalDataStart { get; set; }

    /// <summary>
    /// Forecast model
    /// </summary>
    public string ForecastModel { get; set; } = "linear";

    /// <summary>
    /// Include confidence intervals
    /// </summary>
    public bool IncludeConfidenceIntervals { get; set; } = true;

    /// <summary>
    /// Include seasonal adjustments
    /// </summary>
    public bool IncludeSeasonalAdjustments { get; set; } = false;

    /// <summary>
    /// Granularity
    /// </summary>
    public string Granularity { get; set; } = "day";

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Filters
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();
}

/// <summary>
/// Cost optimization recommendations
/// </summary>
public class CostOptimizationRecommendations
{
    /// <summary>
    /// Total potential savings
    /// </summary>
    public decimal TotalPotentialSavings { get; set; }

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<CostOptimizationRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// Quick wins (easy to implement)
    /// </summary>
    public List<CostOptimizationRecommendation> QuickWins { get; set; } = new();

    /// <summary>
    /// Long-term optimizations
    /// </summary>
    public List<CostOptimizationRecommendation> LongTermOptimizations { get; set; } = new();

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Cost optimization recommendation
/// </summary>
public class CostOptimizationRecommendation
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
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Potential savings
    /// </summary>
    public decimal PotentialSavings { get; set; }

    /// <summary>
    /// Savings percentage
    /// </summary>
    public double SavingsPercentage { get; set; }

    /// <summary>
    /// Implementation effort (low, medium, high)
    /// </summary>
    public string ImplementationEffort { get; set; } = string.Empty;

    /// <summary>
    /// Priority (low, medium, high, critical)
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Implementation steps
    /// </summary>
    public List<string> ImplementationSteps { get; set; } = new();

    /// <summary>
    /// Affected resources
    /// </summary>
    public List<string> AffectedResources { get; set; } = new();

    /// <summary>
    /// Risk level
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;
}

/// <summary>
/// Cost optimization opportunity
/// </summary>
public class CostOptimizationOpportunity
{
    /// <summary>
    /// Opportunity type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Potential savings
    /// </summary>
    public decimal PotentialSavings { get; set; }

    /// <summary>
    /// Confidence level
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Resource identifier
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;
}

/// <summary>
/// Cost alert
/// </summary>
public class CostAlert
{
    /// <summary>
    /// Alert ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Alert name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Alert type (threshold, anomaly, budget)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Threshold amount
    /// </summary>
    public decimal? ThresholdAmount { get; set; }

    /// <summary>
    /// Threshold percentage
    /// </summary>
    public double? ThresholdPercentage { get; set; }

    /// <summary>
    /// Time period (daily, weekly, monthly)
    /// </summary>
    public string TimePeriod { get; set; } = string.Empty;

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Notification channels
    /// </summary>
    public List<string> NotificationChannels { get; set; } = new();

    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last triggered
    /// </summary>
    public DateTime? LastTriggered { get; set; }

    /// <summary>
    /// Trigger count
    /// </summary>
    public int TriggerCount { get; set; }
}

/// <summary>
/// Create cost alert request
/// </summary>
public class CreateCostAlertRequest
{
    /// <summary>
    /// Alert name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Alert type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Threshold amount
    /// </summary>
    public decimal? ThresholdAmount { get; set; }

    /// <summary>
    /// Threshold percentage
    /// </summary>
    public double? ThresholdPercentage { get; set; }

    /// <summary>
    /// Time period
    /// </summary>
    public string TimePeriod { get; set; } = string.Empty;

    /// <summary>
    /// Notification channels
    /// </summary>
    public List<string> NotificationChannels { get; set; } = new();

    /// <summary>
    /// Filters
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();
}

/// <summary>
/// Update cost alert request
/// </summary>
public class UpdateCostAlertRequest
{
    /// <summary>
    /// Alert name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Threshold amount
    /// </summary>
    public decimal? ThresholdAmount { get; set; }

    /// <summary>
    /// Threshold percentage
    /// </summary>
    public double? ThresholdPercentage { get; set; }

    /// <summary>
    /// Is active
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Notification channels
    /// </summary>
    public List<string>? NotificationChannels { get; set; }
}

/// <summary>
/// Cost anomaly detection request
/// </summary>
public class CostAnomalyDetectionRequest
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
    /// Sensitivity level (1-10)
    /// </summary>
    public int Sensitivity { get; set; } = 5;

    /// <summary>
    /// Minimum anomaly score
    /// </summary>
    public double MinAnomalyScore { get; set; } = 0.7;

    /// <summary>
    /// Dimensions to analyze
    /// </summary>
    public List<string> Dimensions { get; set; } = new();
}

/// <summary>
/// Cost anomaly detection result
/// </summary>
public class CostAnomalyDetectionResult
{
    /// <summary>
    /// Detected anomalies
    /// </summary>
    public List<CostAnomaly> Anomalies { get; set; } = new();

    /// <summary>
    /// Summary
    /// </summary>
    public CostAnomalySummary Summary { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Cost anomaly
/// </summary>
public class CostAnomaly
{
    /// <summary>
    /// Anomaly ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Actual cost
    /// </summary>
    public decimal ActualCost { get; set; }

    /// <summary>
    /// Expected cost
    /// </summary>
    public decimal ExpectedCost { get; set; }

    /// <summary>
    /// Anomaly score (0-1)
    /// </summary>
    public double AnomalyScore { get; set; }

    /// <summary>
    /// Severity (low, medium, high)
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

    /// <summary>
    /// Affected dimension
    /// </summary>
    public string? AffectedDimension { get; set; }

    /// <summary>
    /// Affected resource
    /// </summary>
    public string? AffectedResource { get; set; }
}

/// <summary>
/// Cost anomaly summary
/// </summary>
public class CostAnomalySummary
{
    /// <summary>
    /// Total anomalies
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
    /// Total anomalous cost
    /// </summary>
    public decimal TotalAnomalousCost { get; set; }

    /// <summary>
    /// Total excess cost
    /// </summary>
    public decimal TotalExcessCost { get; set; }

    /// <summary>
    /// Average anomaly score
    /// </summary>
    public double AverageAnomalyScore { get; set; }

    /// <summary>
    /// Most affected dimensions
    /// </summary>
    public List<string> MostAffectedDimensions { get; set; } = new();
}

/// <summary>
/// Cost breakdown request
/// </summary>
public class CostBreakdownRequest
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
    /// Group by dimension
    /// </summary>
    public string GroupBy { get; set; } = "provider";

    /// <summary>
    /// Breakdown dimensions
    /// </summary>
    public List<string> Dimensions { get; set; } = new();

    /// <summary>
    /// Granularity
    /// </summary>
    public string Granularity { get; set; } = "day";

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Filters
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();
}

/// <summary>
/// Cost breakdown
/// </summary>
public class CostBreakdown
{
    /// <summary>
    /// Key (for grouping)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Cost in USD
    /// </summary>
    public decimal CostUsd { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public long Tokens { get; set; }

    /// <summary>
    /// Percentage of total
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Breakdown by dimension
    /// </summary>
    public Dictionary<string, List<CostBreakdownItem>> BreakdownByDimension { get; set; } = new();

    /// <summary>
    /// Time series breakdown
    /// </summary>
    public List<CostBreakdownTimePoint> TimeSeriesBreakdown { get; set; } = new();
}

/// <summary>
/// Cost breakdown item
/// </summary>
public class CostBreakdownItem
{
    /// <summary>
    /// Dimension value
    /// </summary>
    public string DimensionValue { get; set; } = string.Empty;

    /// <summary>
    /// Cost
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Percentage of total
    /// </summary>
    public double PercentageOfTotal { get; set; }

    /// <summary>
    /// Usage metrics
    /// </summary>
    public Dictionary<string, object> UsageMetrics { get; set; } = new();
}

/// <summary>
/// Cost breakdown time point
/// </summary>
public class CostBreakdownTimePoint
{
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Breakdown by dimension
    /// </summary>
    public Dictionary<string, decimal> BreakdownByDimension { get; set; } = new();
}

/// <summary>
/// Cost trends request
/// </summary>
public class CostTrendsRequest
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
    /// Comparison period
    /// </summary>
    public string ComparisonPeriod { get; set; } = "previous_period";

    /// <summary>
    /// Granularity
    /// </summary>
    public string Granularity { get; set; } = "day";

    /// <summary>
    /// Include forecasting
    /// </summary>
    public bool IncludeForecast { get; set; } = false;
}

/// <summary>
/// Cost efficiency request
/// </summary>
public class CostEfficiencyRequest
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
    /// Efficiency metrics to calculate
    /// </summary>
    public List<string> Metrics { get; set; } = new();

    /// <summary>
    /// Benchmark against
    /// </summary>
    public string? BenchmarkAgainst { get; set; }
}

/// <summary>
/// Cost efficiency metrics
/// </summary>
public class CostEfficiencyMetrics
{
    /// <summary>
    /// Cost per request
    /// </summary>
    public decimal CostPerRequest { get; set; }

    /// <summary>
    /// Cost per token
    /// </summary>
    public decimal CostPerToken { get; set; }

    /// <summary>
    /// Cost per successful request
    /// </summary>
    public decimal CostPerSuccessfulRequest { get; set; }

    /// <summary>
    /// Efficiency score (0-100)
    /// </summary>
    public double EfficiencyScore { get; set; }

    /// <summary>
    /// Efficiency trends
    /// </summary>
    public List<EfficiencyTrendPoint> EfficiencyTrends { get; set; } = new();

    /// <summary>
    /// Benchmark comparison
    /// </summary>
    public EfficiencyBenchmark? BenchmarkComparison { get; set; }

    /// <summary>
    /// Optimization opportunities
    /// </summary>
    public List<string> OptimizationOpportunities { get; set; } = new();
}

/// <summary>
/// Efficiency trend point
/// </summary>
public class EfficiencyTrendPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Efficiency score
    /// </summary>
    public double EfficiencyScore { get; set; }

    /// <summary>
    /// Cost per request
    /// </summary>
    public decimal CostPerRequest { get; set; }

    /// <summary>
    /// Cost per token
    /// </summary>
    public decimal CostPerToken { get; set; }
}

/// <summary>
/// Efficiency benchmark
/// </summary>
public class EfficiencyBenchmark
{
    /// <summary>
    /// Benchmark name
    /// </summary>
    public string BenchmarkName { get; set; } = string.Empty;

    /// <summary>
    /// Your efficiency score
    /// </summary>
    public double YourEfficiencyScore { get; set; }

    /// <summary>
    /// Benchmark efficiency score
    /// </summary>
    public double BenchmarkEfficiencyScore { get; set; }

    /// <summary>
    /// Performance vs benchmark
    /// </summary>
    public double PerformanceVsBenchmark { get; set; }

    /// <summary>
    /// Ranking percentile
    /// </summary>
    public double RankingPercentile { get; set; }
}

/// <summary>
/// Provider cost comparison request
/// </summary>
public class ProviderCostComparisonRequest
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
    /// Models to compare
    /// </summary>
    public List<string> Models { get; set; } = new();

    /// <summary>
    /// Include efficiency metrics
    /// </summary>
    public bool IncludeEfficiencyMetrics { get; set; } = true;
}

/// <summary>
/// Provider cost comparison
/// </summary>
public class ProviderCostComparison
{
    /// <summary>
    /// Comparison period
    /// </summary>
    public DateRange ComparisonPeriod { get; set; } = new();

    /// <summary>
    /// Provider comparisons
    /// </summary>
    public List<ProviderCostData> ProviderComparisons { get; set; } = new();

    /// <summary>
    /// Cost savings opportunities
    /// </summary>
    public List<CostSavingsOpportunity> CostSavingsOpportunities { get; set; } = new();

    /// <summary>
    /// Total cost by provider
    /// </summary>
    public Dictionary<string, decimal> TotalCostByProvider { get; set; } = new();

    /// <summary>
    /// Cost per request by provider
    /// </summary>
    public Dictionary<string, decimal> CostPerRequestByProvider { get; set; } = new();

    /// <summary>
    /// Cost per token by provider
    /// </summary>
    public Dictionary<string, decimal> CostPerTokenByProvider { get; set; } = new();

    /// <summary>
    /// Provider rankings
    /// </summary>
    public List<ProviderRanking> ProviderRankings { get; set; } = new();

    /// <summary>
    /// Model comparisons
    /// </summary>
    public List<ModelCostComparison> ModelComparisons { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
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
    /// Rank (1 = best)
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Cost efficiency score
    /// </summary>
    public double CostEfficiencyScore { get; set; }

    /// <summary>
    /// Performance score
    /// </summary>
    public double PerformanceScore { get; set; }

    /// <summary>
    /// Overall score
    /// </summary>
    public double OverallScore { get; set; }
}

/// <summary>
/// Model cost comparison
/// </summary>
public class ModelCostComparison
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
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Cost per request
    /// </summary>
    public decimal CostPerRequest { get; set; }

    /// <summary>
    /// Cost per token
    /// </summary>
    public decimal CostPerToken { get; set; }

    /// <summary>
    /// Usage volume
    /// </summary>
    public long UsageVolume { get; set; }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Cost allocation request
/// </summary>
public class CostAllocationRequest
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
    /// Allocation method (equal, usage-based, custom)
    /// </summary>
    public string AllocationMethod { get; set; } = "usage-based";

    /// <summary>
    /// Cost centers to include
    /// </summary>
    public List<string> CostCenters { get; set; } = new();

    /// <summary>
    /// Include unallocated costs
    /// </summary>
    public bool IncludeUnallocatedCosts { get; set; } = true;

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";
}

/// <summary>
/// Cost allocation
/// </summary>
public class CostAllocation
{
    /// <summary>
    /// Allocation period
    /// </summary>
    public DateRange AllocationPeriod { get; set; } = new();

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Allocation method
    /// </summary>
    public string AllocationMethod { get; set; } = string.Empty;

    /// <summary>
    /// Allocations
    /// </summary>
    public List<CostAllocationItem> Allocations { get; set; } = new();

    /// <summary>
    /// Allocation rules
    /// </summary>
    public List<string> AllocationRules { get; set; } = new();

    /// <summary>
    /// Total allocated cost
    /// </summary>
    public decimal TotalAllocatedCost { get; set; }

    /// <summary>
    /// Unallocated cost
    /// </summary>
    public decimal UnallocatedCost { get; set; }

    /// <summary>
    /// Allocation by cost center
    /// </summary>
    public Dictionary<string, CostCenterAllocation> AllocationByCostCenter { get; set; } = new();

    /// <summary>
    /// Allocation method used
    /// </summary>
    public string AllocationMethodUsed { get; set; } = string.Empty;

    /// <summary>
    /// Allocation rules applied
    /// </summary>
    public List<string> AllocationRulesApplied { get; set; } = new();
}

/// <summary>
/// Cost center allocation
/// </summary>
public class CostCenterAllocation
{
    /// <summary>
    /// Cost center ID
    /// </summary>
    public string CostCenterId { get; set; } = string.Empty;

    /// <summary>
    /// Cost center name
    /// </summary>
    public string CostCenterName { get; set; } = string.Empty;

    /// <summary>
    /// Allocated cost
    /// </summary>
    public decimal AllocatedCost { get; set; }

    /// <summary>
    /// Percentage of total
    /// </summary>
    public double PercentageOfTotal { get; set; }

    /// <summary>
    /// Usage metrics
    /// </summary>
    public Dictionary<string, object> UsageMetrics { get; set; } = new();

    /// <summary>
    /// Allocation basis
    /// </summary>
    public string AllocationBasis { get; set; } = string.Empty;
}

/// <summary>
/// Cost center
/// </summary>
public class CostCenter
{
    /// <summary>
    /// Cost center ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Owner user ID
    /// </summary>
    public string OwnerUserId { get; set; } = string.Empty;

    /// <summary>
    /// Manager user ID
    /// </summary>
    public string? ManagerUserId { get; set; }

    /// <summary>
    /// Budget
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// Budget limit
    /// </summary>
    public decimal? BudgetLimit { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Budget period
    /// </summary>
    public string? BudgetPeriod { get; set; }

    /// <summary>
    /// Tags for allocation rules
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Allocation rules
    /// </summary>
    public List<AllocationRule> AllocationRules { get; set; } = new();

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated at
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create cost center request
/// </summary>
public class CreateCostCenterRequest
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Manager user ID
    /// </summary>
    public string? ManagerUserId { get; set; }

    /// <summary>
    /// Budget limit
    /// </summary>
    public decimal? BudgetLimit { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Budget
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// Budget period
    /// </summary>
    public string? BudgetPeriod { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Allocation rules
    /// </summary>
    public List<AllocationRule> AllocationRules { get; set; } = new();
}

/// <summary>
/// Real-time cost data
/// </summary>
public class RealTimeCostData
{
    /// <summary>
    /// Current hour cost
    /// </summary>
    public decimal CurrentHourCost { get; set; }

    /// <summary>
    /// Current period
    /// </summary>
    public DateRange CurrentPeriod { get; set; } = new();

    /// <summary>
    /// Current cost
    /// </summary>
    public decimal CurrentCost { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Today's cost
    /// </summary>
    public decimal TodaysCost { get; set; }

    /// <summary>
    /// Yesterday's cost
    /// </summary>
    public decimal YesterdaysCost { get; set; }

    /// <summary>
    /// Month to date cost
    /// </summary>
    public decimal MonthToDateCost { get; set; }

    /// <summary>
    /// Daily budget
    /// </summary>
    public decimal DailyBudget { get; set; }

    /// <summary>
    /// Monthly budget
    /// </summary>
    public decimal MonthlyBudget { get; set; }

    /// <summary>
    /// Budget utilization
    /// </summary>
    public double BudgetUtilization { get; set; }

    /// <summary>
    /// Cost velocity
    /// </summary>
    public decimal CostVelocity { get; set; }

    /// <summary>
    /// Recent transactions
    /// </summary>
    public List<RecentTransaction> RecentTransactions { get; set; } = new();

    /// <summary>
    /// Current day cost
    /// </summary>
    public decimal CurrentDayCost { get; set; }

    /// <summary>
    /// Current month cost
    /// </summary>
    public decimal CurrentMonthCost { get; set; }

    /// <summary>
    /// Cost rate (per hour)
    /// </summary>
    public decimal CostRate { get; set; }

    /// <summary>
    /// Projected daily cost
    /// </summary>
    public decimal ProjectedDailyCost { get; set; }

    /// <summary>
    /// Projected monthly cost
    /// </summary>
    public decimal ProjectedMonthlyCost { get; set; }

    /// <summary>
    /// Active alerts
    /// </summary>
    public List<CostAlert> ActiveAlerts { get; set; } = new();

    /// <summary>
    /// Top cost drivers
    /// </summary>
    public List<CostDriver> TopCostDrivers { get; set; } = new();

    /// <summary>
    /// Last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Cost driver
/// </summary>
public class CostDriver
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cost
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Percentage
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Resource type
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Resource identifier
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Cost contribution
    /// </summary>
    public decimal CostContribution { get; set; }

    /// <summary>
    /// Percentage of total
    /// </summary>
    public double PercentageOfTotal { get; set; }

    /// <summary>
    /// Usage metrics
    /// </summary>
    public Dictionary<string, object> UsageMetrics { get; set; } = new();
}

/// <summary>
/// Export cost data request
/// </summary>
public class ExportCostDataRequest
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
    /// Export format
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Csv;

    /// <summary>
    /// Include breakdown
    /// </summary>
    public bool IncludeBreakdown { get; set; } = true;

    /// <summary>
    /// Breakdown dimensions
    /// </summary>
    public List<string> BreakdownDimensions { get; set; } = new();

    /// <summary>
    /// Granularity
    /// </summary>
    public string Granularity { get; set; } = "day";
}

/// <summary>
/// Export format
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// CSV format
    /// </summary>
    Csv,

    /// <summary>
    /// JSON format
    /// </summary>
    Json,

    /// <summary>
    /// Excel format
    /// </summary>
    Excel,

    /// <summary>
    /// PDF format
    /// </summary>
    Pdf
}

/// <summary>
/// Export data
/// </summary>
public class ExportData
{
    /// <summary>
    /// Export ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Download URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Format
    /// </summary>
    public ExportFormat Format { get; set; }

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Expires at
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Record count
    /// </summary>
    public long RecordCount { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Progress percentage
    /// </summary>
    public double ProgressPercentage { get; set; }
}

/// <summary>
/// Allocation rule
/// </summary>
public class AllocationRule
{
    /// <summary>
    /// Rule ID
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Allocation basis (usage, equal, custom)
    /// </summary>
    public string AllocationBasis { get; set; } = "usage";

    /// <summary>
    /// Weight
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Conditions
    /// </summary>
    public Dictionary<string, object> Conditions { get; set; } = new();

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; } = true;
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
/// Provider cost data
/// </summary>
public class ProviderCostData
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

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

    /// <summary>
    /// Average cost per token
    /// </summary>
    public decimal AverageCostPerToken { get; set; }

    /// <summary>
    /// Market share
    /// </summary>
    public double MarketShare { get; set; }
}

/// <summary>
/// Cost savings opportunity
/// </summary>
public class CostSavingsOpportunity
{
    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Potential savings
    /// </summary>
    public decimal PotentialSavings { get; set; }

    /// <summary>
    /// Savings percentage
    /// </summary>
    public double SavingsPercentage { get; set; }

    /// <summary>
    /// Risk level
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Implementation effort
    /// </summary>
    public string ImplementationEffort { get; set; } = string.Empty;
}

/// <summary>
/// Cost allocation item
/// </summary>
public class CostAllocationItem
{
    /// <summary>
    /// Entity ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Entity name
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Entity type
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Allocated cost
    /// </summary>
    public decimal AllocatedCost { get; set; }

    /// <summary>
    /// Cost center ID
    /// </summary>
    public string CostCenterId { get; set; } = string.Empty;

    /// <summary>
    /// Cost center name
    /// </summary>
    public string CostCenterName { get; set; } = string.Empty;

    /// <summary>
    /// Allocated amount
    /// </summary>
    public decimal AllocatedAmount { get; set; }

    /// <summary>
    /// Allocation percentage
    /// </summary>
    public double AllocationPercentage { get; set; }

    /// <summary>
    /// Allocation basis
    /// </summary>
    public string AllocationBasis { get; set; } = string.Empty;
}

/// <summary>
/// Recent transaction
/// </summary>
public class RecentTransaction
{
    /// <summary>
    /// Transaction ID
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Cost
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Operation type
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
}