namespace LLMGateway.Core.Models.Analytics;

/// <summary>
/// Custom report request
/// </summary>
public class CustomReportRequest
{
    /// <summary>
    /// Report name
    /// </summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// Report type
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Data sources
    /// </summary>
    public List<string> DataSources { get; set; } = new();

    /// <summary>
    /// Metrics to include
    /// </summary>
    public List<string> Metrics { get; set; } = new();

    /// <summary>
    /// Dimensions
    /// </summary>
    public List<string> Dimensions { get; set; } = new();

    /// <summary>
    /// Filters
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();

    /// <summary>
    /// Date range
    /// </summary>
    public DateRange DateRange { get; set; } = new();

    /// <summary>
    /// Granularity
    /// </summary>
    public string Granularity { get; set; } = "day";

    /// <summary>
    /// Output format
    /// </summary>
    public string OutputFormat { get; set; } = "json";

    /// <summary>
    /// Include visualizations
    /// </summary>
    public bool IncludeVisualizations { get; set; } = false;

    /// <summary>
    /// Schedule (optional)
    /// </summary>
    public ReportSchedule? Schedule { get; set; }
}

/// <summary>
/// Custom report
/// </summary>
public class CustomReport
{
    /// <summary>
    /// Report ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Report name
    /// </summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// Report type
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Data
    /// </summary>
    public ReportData Data { get; set; } = new();

    /// <summary>
    /// Visualizations
    /// </summary>
    public List<ReportVisualization> Visualizations { get; set; } = new();

    /// <summary>
    /// Summary
    /// </summary>
    public ReportSummary Summary { get; set; } = new();

    /// <summary>
    /// Download URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Expires at
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Report data
/// </summary>
public class ReportData
{
    /// <summary>
    /// Headers
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// Rows
    /// </summary>
    public List<List<object>> Rows { get; set; } = new();

    /// <summary>
    /// Metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Total rows
    /// </summary>
    public long TotalRows { get; set; }
}

/// <summary>
/// Report visualization
/// </summary>
public class ReportVisualization
{
    /// <summary>
    /// Visualization type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Data
    /// </summary>
    public object Data { get; set; } = new();

    /// <summary>
    /// Configuration
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Report summary
/// </summary>
public class ReportSummary
{
    /// <summary>
    /// Key metrics
    /// </summary>
    public Dictionary<string, object> KeyMetrics { get; set; } = new();

    /// <summary>
    /// Insights
    /// </summary>
    public List<string> Insights { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Report schedule
/// </summary>
public class ReportSchedule
{
    /// <summary>
    /// Frequency (daily, weekly, monthly)
    /// </summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>
    /// Time of day
    /// </summary>
    public TimeSpan TimeOfDay { get; set; }

    /// <summary>
    /// Day of week (for weekly)
    /// </summary>
    public DayOfWeek? DayOfWeek { get; set; }

    /// <summary>
    /// Day of month (for monthly)
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Recipients
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Export analytics request
/// </summary>
public class ExportAnalyticsRequest
{
    /// <summary>
    /// Export type
    /// </summary>
    public string ExportType { get; set; } = string.Empty;

    /// <summary>
    /// Data sources
    /// </summary>
    public List<string> DataSources { get; set; } = new();

    /// <summary>
    /// Date range
    /// </summary>
    public DateRange DateRange { get; set; } = new();

    /// <summary>
    /// Format
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Csv;

    /// <summary>
    /// Include raw data
    /// </summary>
    public bool IncludeRawData { get; set; } = true;

    /// <summary>
    /// Include aggregated data
    /// </summary>
    public bool IncludeAggregatedData { get; set; } = true;

    /// <summary>
    /// Include visualizations
    /// </summary>
    public bool IncludeVisualizations { get; set; } = false;

    /// <summary>
    /// Compression
    /// </summary>
    public bool UseCompression { get; set; } = true;

    /// <summary>
    /// Filters
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();
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

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }
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
    Pdf,

    /// <summary>
    /// Parquet format
    /// </summary>
    Parquet
}

/// <summary>
/// Usage forecast request
/// </summary>
public class UsageForecastRequest
{
    /// <summary>
    /// Forecast type (requests, tokens, cost)
    /// </summary>
    public string ForecastType { get; set; } = string.Empty;

    /// <summary>
    /// Forecast horizon (days)
    /// </summary>
    public int ForecastHorizonDays { get; set; } = 30;

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
    /// Include growth factors
    /// </summary>
    public bool IncludeGrowthFactors { get; set; } = true;

    /// <summary>
    /// Granularity
    /// </summary>
    public string Granularity { get; set; } = "day";

    /// <summary>
    /// Filters
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();
}

/// <summary>
/// Usage forecast
/// </summary>
public class UsageForecast
{
    /// <summary>
    /// Forecast type
    /// </summary>
    public string ForecastType { get; set; } = string.Empty;

    /// <summary>
    /// Forecast horizon
    /// </summary>
    public TimeSpan ForecastHorizon { get; set; }

    /// <summary>
    /// Confidence level
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Model accuracy
    /// </summary>
    public double ModelAccuracy { get; set; }

    /// <summary>
    /// Forecast points
    /// </summary>
    public List<UsageForecastPoint> ForecastPoints { get; set; } = new();

    /// <summary>
    /// Summary statistics
    /// </summary>
    public UsageForecastSummary Summary { get; set; } = new();

    /// <summary>
    /// Seasonal patterns
    /// </summary>
    public SeasonalPatterns SeasonalPatterns { get; set; } = new();

    /// <summary>
    /// Growth trends
    /// </summary>
    public GrowthTrends GrowthTrends { get; set; } = new();

    /// <summary>
    /// Model metadata
    /// </summary>
    public ForecastModelMetadata ModelMetadata { get; set; } = new();
}

/// <summary>
/// Usage forecast point
/// </summary>
public class UsageForecastPoint
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
    /// Seasonal component
    /// </summary>
    public double SeasonalComponent { get; set; }

    /// <summary>
    /// Trend component
    /// </summary>
    public double TrendComponent { get; set; }
}

/// <summary>
/// Usage forecast summary
/// </summary>
public class UsageForecastSummary
{
    /// <summary>
    /// Total predicted usage
    /// </summary>
    public double TotalPredictedUsage { get; set; }

    /// <summary>
    /// Average daily usage
    /// </summary>
    public double AverageDailyUsage { get; set; }

    /// <summary>
    /// Peak usage day
    /// </summary>
    public DateTime PeakUsageDay { get; set; }

    /// <summary>
    /// Peak usage value
    /// </summary>
    public double PeakUsageValue { get; set; }

    /// <summary>
    /// Growth rate
    /// </summary>
    public double GrowthRate { get; set; }

    /// <summary>
    /// Volatility
    /// </summary>
    public double Volatility { get; set; }
}

/// <summary>
/// Growth trends
/// </summary>
public class GrowthTrends
{
    /// <summary>
    /// Overall growth rate
    /// </summary>
    public double OverallGrowthRate { get; set; }

    /// <summary>
    /// Monthly growth rates
    /// </summary>
    public List<MonthlyGrowthRate> MonthlyGrowthRates { get; set; } = new();

    /// <summary>
    /// Acceleration
    /// </summary>
    public double Acceleration { get; set; }

    /// <summary>
    /// Trend stability
    /// </summary>
    public double TrendStability { get; set; }
}

/// <summary>
/// Monthly growth rate
/// </summary>
public class MonthlyGrowthRate
{
    /// <summary>
    /// Month
    /// </summary>
    public DateTime Month { get; set; }

    /// <summary>
    /// Growth rate
    /// </summary>
    public double GrowthRate { get; set; }

    /// <summary>
    /// Confidence
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// Forecast model metadata
/// </summary>
public class ForecastModelMetadata
{
    /// <summary>
    /// Model type
    /// </summary>
    public string ModelType { get; set; } = string.Empty;

    /// <summary>
    /// Algorithm
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Training data points
    /// </summary>
    public long TrainingDataPoints { get; set; }

    /// <summary>
    /// Model parameters
    /// </summary>
    public Dictionary<string, object> ModelParameters { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();

    /// <summary>
    /// Last trained
    /// </summary>
    public DateTime LastTrained { get; set; }
}

/// <summary>
/// Security analytics request
/// </summary>
public class SecurityAnalyticsRequest
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
    /// Include threat analysis
    /// </summary>
    public bool IncludeThreatAnalysis { get; set; } = true;

    /// <summary>
    /// Include access patterns
    /// </summary>
    public bool IncludeAccessPatterns { get; set; } = true;

    /// <summary>
    /// Include compliance status
    /// </summary>
    public bool IncludeComplianceStatus { get; set; } = true;

    /// <summary>
    /// Include vulnerability assessment
    /// </summary>
    public bool IncludeVulnerabilityAssessment { get; set; } = false;

    /// <summary>
    /// Security levels to analyze
    /// </summary>
    public List<string> SecurityLevels { get; set; } = new();

    /// <summary>
    /// Threat types to analyze
    /// </summary>
    public List<string> ThreatTypes { get; set; } = new();
}

/// <summary>
/// Security analytics
/// </summary>
public class SecurityAnalytics
{
    /// <summary>
    /// Overall security score (0-100)
    /// </summary>
    public double OverallSecurityScore { get; set; }

    /// <summary>
    /// Security status
    /// </summary>
    public string SecurityStatus { get; set; } = string.Empty;

    /// <summary>
    /// Threat analysis
    /// </summary>
    public ThreatAnalysis? ThreatAnalysis { get; set; }

    /// <summary>
    /// Access patterns
    /// </summary>
    public AccessPatternAnalysis? AccessPatterns { get; set; }

    /// <summary>
    /// Compliance status
    /// </summary>
    public ComplianceStatus? ComplianceStatus { get; set; }

    /// <summary>
    /// Vulnerability assessment
    /// </summary>
    public VulnerabilityAssessment? VulnerabilityAssessment { get; set; }

    /// <summary>
    /// Security incidents
    /// </summary>
    public List<SecurityIncident> SecurityIncidents { get; set; } = new();

    /// <summary>
    /// Security recommendations
    /// </summary>
    public List<SecurityRecommendation> SecurityRecommendations { get; set; } = new();

    /// <summary>
    /// Security trends
    /// </summary>
    public SecurityTrends SecurityTrends { get; set; } = new();
}

/// <summary>
/// Threat analysis
/// </summary>
public class ThreatAnalysis
{
    /// <summary>
    /// Threat level (low, medium, high, critical)
    /// </summary>
    public string ThreatLevel { get; set; } = string.Empty;

    /// <summary>
    /// Active threats
    /// </summary>
    public int ActiveThreats { get; set; }

    /// <summary>
    /// Mitigated threats
    /// </summary>
    public int MitigatedThreats { get; set; }

    /// <summary>
    /// Threat categories
    /// </summary>
    public List<ThreatCategory> ThreatCategories { get; set; } = new();

    /// <summary>
    /// Risk assessment
    /// </summary>
    public RiskAssessment RiskAssessment { get; set; } = new();

    /// <summary>
    /// Attack vectors
    /// </summary>
    public List<AttackVector> AttackVectors { get; set; } = new();
}

/// <summary>
/// Threat category
/// </summary>
public class ThreatCategory
{
    /// <summary>
    /// Category name
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Threat count
    /// </summary>
    public int ThreatCount { get; set; }

    /// <summary>
    /// Severity level
    /// </summary>
    public string SeverityLevel { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Mitigation status
    /// </summary>
    public string MitigationStatus { get; set; } = string.Empty;
}

/// <summary>
/// Risk assessment
/// </summary>
public class RiskAssessment
{
    /// <summary>
    /// Overall risk score (0-100)
    /// </summary>
    public double OverallRiskScore { get; set; }

    /// <summary>
    /// Risk level
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Risk factors
    /// </summary>
    public List<RiskFactor> RiskFactors { get; set; } = new();

    /// <summary>
    /// Risk trends
    /// </summary>
    public List<RiskTrendPoint> RiskTrends { get; set; } = new();
}

/// <summary>
/// Risk trend point
/// </summary>
public class RiskTrendPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Risk score
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// Risk level
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;
}

/// <summary>
/// Attack vector
/// </summary>
public class AttackVector
{
    /// <summary>
    /// Vector name
    /// </summary>
    public string VectorName { get; set; } = string.Empty;

    /// <summary>
    /// Frequency
    /// </summary>
    public int Frequency { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Impact level
    /// </summary>
    public string ImpactLevel { get; set; } = string.Empty;

    /// <summary>
    /// Countermeasures
    /// </summary>
    public List<string> Countermeasures { get; set; } = new();
}

/// <summary>
/// Access pattern analysis
/// </summary>
public class AccessPatternAnalysis
{
    /// <summary>
    /// Normal access patterns
    /// </summary>
    public List<AccessPattern> NormalPatterns { get; set; } = new();

    /// <summary>
    /// Anomalous access patterns
    /// </summary>
    public List<AccessPattern> AnomalousPatterns { get; set; } = new();

    /// <summary>
    /// Access frequency analysis
    /// </summary>
    public AccessFrequencyAnalysis FrequencyAnalysis { get; set; } = new();

    /// <summary>
    /// Geographic access analysis
    /// </summary>
    public GeographicAccessAnalysis GeographicAnalysis { get; set; } = new();

    /// <summary>
    /// Time-based access analysis
    /// </summary>
    public TimeBasedAccessAnalysis TimeBasedAnalysis { get; set; } = new();
}

/// <summary>
/// Access pattern
/// </summary>
public class AccessPattern
{
    /// <summary>
    /// Pattern ID
    /// </summary>
    public string PatternId { get; set; } = string.Empty;

    /// <summary>
    /// Pattern type
    /// </summary>
    public string PatternType { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Frequency
    /// </summary>
    public int Frequency { get; set; }

    /// <summary>
    /// Risk level
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// First observed
    /// </summary>
    public DateTime FirstObserved { get; set; }

    /// <summary>
    /// Last observed
    /// </summary>
    public DateTime LastObserved { get; set; }
}

/// <summary>
/// Access frequency analysis
/// </summary>
public class AccessFrequencyAnalysis
{
    /// <summary>
    /// Average requests per hour
    /// </summary>
    public double AverageRequestsPerHour { get; set; }

    /// <summary>
    /// Peak access hours
    /// </summary>
    public List<int> PeakAccessHours { get; set; } = new();

    /// <summary>
    /// Unusual frequency patterns
    /// </summary>
    public List<string> UnusualPatterns { get; set; } = new();
}

/// <summary>
/// Geographic access analysis
/// </summary>
public class GeographicAccessAnalysis
{
    /// <summary>
    /// Access by country
    /// </summary>
    public Dictionary<string, int> AccessByCountry { get; set; } = new();

    /// <summary>
    /// Suspicious locations
    /// </summary>
    public List<SuspiciousLocation> SuspiciousLocations { get; set; } = new();

    /// <summary>
    /// Geographic anomalies
    /// </summary>
    public List<string> GeographicAnomalies { get; set; } = new();
}

/// <summary>
/// Suspicious location
/// </summary>
public class SuspiciousLocation
{
    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// City
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Access count
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// Risk score
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Time-based access analysis
/// </summary>
public class TimeBasedAccessAnalysis
{
    /// <summary>
    /// Hourly access distribution
    /// </summary>
    public Dictionary<int, int> HourlyDistribution { get; set; } = new();

    /// <summary>
    /// Daily access distribution
    /// </summary>
    public Dictionary<DayOfWeek, int> DailyDistribution { get; set; } = new();

    /// <summary>
    /// Off-hours access
    /// </summary>
    public List<OffHoursAccess> OffHoursAccess { get; set; } = new();
}

/// <summary>
/// Off-hours access
/// </summary>
public class OffHoursAccess
{
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Risk score
    /// </summary>
    public double RiskScore { get; set; }
}

/// <summary>
/// Compliance status
/// </summary>
public class ComplianceStatus
{
    /// <summary>
    /// Overall compliance score
    /// </summary>
    public double OverallComplianceScore { get; set; }

    /// <summary>
    /// Compliance frameworks
    /// </summary>
    public List<ComplianceFramework> ComplianceFrameworks { get; set; } = new();

    /// <summary>
    /// Compliance violations
    /// </summary>
    public List<ComplianceViolation> ComplianceViolations { get; set; } = new();

    /// <summary>
    /// Remediation actions
    /// </summary>
    public List<RemediationAction> RemediationActions { get; set; } = new();
}

/// <summary>
/// Compliance framework
/// </summary>
public class ComplianceFramework
{
    /// <summary>
    /// Framework name
    /// </summary>
    public string FrameworkName { get; set; } = string.Empty;

    /// <summary>
    /// Compliance score
    /// </summary>
    public double ComplianceScore { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Last assessment
    /// </summary>
    public DateTime LastAssessment { get; set; }

    /// <summary>
    /// Next assessment
    /// </summary>
    public DateTime NextAssessment { get; set; }
}

/// <summary>
/// Compliance violation
/// </summary>
public class ComplianceViolation
{
    /// <summary>
    /// Violation ID
    /// </summary>
    public string ViolationId { get; set; } = string.Empty;

    /// <summary>
    /// Framework
    /// </summary>
    public string Framework { get; set; } = string.Empty;

    /// <summary>
    /// Rule
    /// </summary>
    public string Rule { get; set; } = string.Empty;

    /// <summary>
    /// Severity
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Detected at
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Remediation action
/// </summary>
public class RemediationAction
{
    /// <summary>
    /// Action ID
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

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
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Due date
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Assigned to
    /// </summary>
    public string? AssignedTo { get; set; }
}

/// <summary>
/// Vulnerability assessment
/// </summary>
public class VulnerabilityAssessment
{
    /// <summary>
    /// Overall vulnerability score
    /// </summary>
    public double OverallVulnerabilityScore { get; set; }

    /// <summary>
    /// Vulnerabilities
    /// </summary>
    public List<Vulnerability> Vulnerabilities { get; set; } = new();

    /// <summary>
    /// Vulnerability trends
    /// </summary>
    public List<VulnerabilityTrendPoint> VulnerabilityTrends { get; set; } = new();

    /// <summary>
    /// Patch status
    /// </summary>
    public PatchStatus PatchStatus { get; set; } = new();
}

/// <summary>
/// Vulnerability
/// </summary>
public class Vulnerability
{
    /// <summary>
    /// Vulnerability ID
    /// </summary>
    public string VulnerabilityId { get; set; } = string.Empty;

    /// <summary>
    /// CVE ID
    /// </summary>
    public string? CveId { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Severity
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// CVSS score
    /// </summary>
    public double? CvssScore { get; set; }

    /// <summary>
    /// Affected components
    /// </summary>
    public List<string> AffectedComponents { get; set; } = new();

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Discovered at
    /// </summary>
    public DateTime DiscoveredAt { get; set; }
}

/// <summary>
/// Vulnerability trend point
/// </summary>
public class VulnerabilityTrendPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Vulnerability count
    /// </summary>
    public int VulnerabilityCount { get; set; }

    /// <summary>
    /// Average severity score
    /// </summary>
    public double AverageSeverityScore { get; set; }
}

/// <summary>
/// Patch status
/// </summary>
public class PatchStatus
{
    /// <summary>
    /// Total patches available
    /// </summary>
    public int TotalPatchesAvailable { get; set; }

    /// <summary>
    /// Patches applied
    /// </summary>
    public int PatchesApplied { get; set; }

    /// <summary>
    /// Patches pending
    /// </summary>
    public int PatchesPending { get; set; }

    /// <summary>
    /// Patch compliance percentage
    /// </summary>
    public double PatchCompliancePercentage { get; set; }

    /// <summary>
    /// Last patch date
    /// </summary>
    public DateTime? LastPatchDate { get; set; }
}

/// <summary>
/// Security incident
/// </summary>
public class SecurityIncident
{
    /// <summary>
    /// Incident ID
    /// </summary>
    public string IncidentId { get; set; } = string.Empty;

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Severity
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Detected at
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Resolved at
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Affected systems
    /// </summary>
    public List<string> AffectedSystems { get; set; } = new();

    /// <summary>
    /// Response actions
    /// </summary>
    public List<string> ResponseActions { get; set; } = new();
}

/// <summary>
/// Security recommendation
/// </summary>
public class SecurityRecommendation
{
    /// <summary>
    /// Recommendation ID
    /// </summary>
    public string RecommendationId { get; set; } = string.Empty;

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
}

/// <summary>
/// Security trends
/// </summary>
public class SecurityTrends
{
    /// <summary>
    /// Security score trends
    /// </summary>
    public List<SecurityTrendPoint> SecurityScoreTrends { get; set; } = new();

    /// <summary>
    /// Incident trends
    /// </summary>
    public List<IncidentTrendPoint> IncidentTrends { get; set; } = new();

    /// <summary>
    /// Threat trends
    /// </summary>
    public List<ThreatTrendPoint> ThreatTrends { get; set; } = new();
}

/// <summary>
/// Security trend point
/// </summary>
public class SecurityTrendPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Security score
    /// </summary>
    public double SecurityScore { get; set; }

    /// <summary>
    /// Trend direction
    /// </summary>
    public string TrendDirection { get; set; } = string.Empty;
}

/// <summary>
/// Incident trend point
/// </summary>
public class IncidentTrendPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Incident count
    /// </summary>
    public int IncidentCount { get; set; }

    /// <summary>
    /// Average severity
    /// </summary>
    public double AverageSeverity { get; set; }
}

/// <summary>
/// Threat trend point
/// </summary>
public class ThreatTrendPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Threat count
    /// </summary>
    public int ThreatCount { get; set; }

    /// <summary>
    /// Threat level
    /// </summary>
    public string ThreatLevel { get; set; } = string.Empty;
}
