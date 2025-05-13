namespace LLMGateway.Core.Options;

/// <summary>
/// Options for background jobs
/// </summary>
public class BackgroundJobOptions
{
    /// <summary>
    /// Whether to enable token usage reports
    /// </summary>
    public bool EnableTokenUsageReports { get; set; } = true;
    
    /// <summary>
    /// Cron schedule for token usage reports
    /// </summary>
    public string TokenUsageReportSchedule { get; set; } = "0 0 0 ? * * *";
    
    /// <summary>
    /// Whether to enable provider health checks
    /// </summary>
    public bool EnableProviderHealthChecks { get; set; } = true;
    
    /// <summary>
    /// Provider health check interval in minutes
    /// </summary>
    public int ProviderHealthCheckIntervalMinutes { get; set; } = 5;
    
    /// <summary>
    /// Whether to enable model metrics aggregation
    /// </summary>
    public bool EnableModelMetricsAggregation { get; set; } = true;
    
    /// <summary>
    /// Cron schedule for model metrics aggregation
    /// </summary>
    public string ModelMetricsAggregationSchedule { get; set; } = "0 0 * ? * * *";
    
    /// <summary>
    /// Whether to enable database maintenance
    /// </summary>
    public bool EnableDatabaseMaintenance { get; set; } = true;
    
    /// <summary>
    /// Cron schedule for database maintenance
    /// </summary>
    public string DatabaseMaintenanceSchedule { get; set; } = "0 0 1 ? * SUN *";
    
    /// <summary>
    /// Whether to enable cost reports
    /// </summary>
    public bool EnableCostReports { get; set; } = true;
    
    /// <summary>
    /// Cron schedule for cost reports
    /// </summary>
    public string CostReportSchedule { get; set; } = "0 0 0 1 * ? *";
    
    /// <summary>
    /// Email addresses to send reports to
    /// </summary>
    public List<string> ReportRecipients { get; set; } = new();
    
    /// <summary>
    /// Email subject prefix for reports
    /// </summary>
    public string ReportEmailSubjectPrefix { get; set; } = "[LLM Gateway] ";
    
    /// <summary>
    /// Whether to include attachments in reports
    /// </summary>
    public bool IncludeAttachments { get; set; } = true;
}
