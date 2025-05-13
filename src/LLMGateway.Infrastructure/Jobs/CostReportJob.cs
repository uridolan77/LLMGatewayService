using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace LLMGateway.Infrastructure.Jobs;

/// <summary>
/// Job for generating cost reports
/// </summary>
[DisallowConcurrentExecution]
public class CostReportJob : IJob
{
    private readonly ITokenUsageService _tokenUsageService;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<CostReportJob> _logger;
    private readonly BackgroundJobOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenUsageService">Token usage service</param>
    /// <param name="telemetryService">Telemetry service</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Background job options</param>
    public CostReportJob(
        ITokenUsageService tokenUsageService,
        ITelemetryService telemetryService,
        ILogger<CostReportJob> logger,
        IOptions<BackgroundJobOptions> options)
    {
        _tokenUsageService = tokenUsageService;
        _telemetryService = telemetryService;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task Execute(IJobExecutionContext context)
    {
        using var operation = _telemetryService.TrackOperation("CostReportJob.Execute");
        
        try
        {
            _logger.LogInformation("Generating cost report");
            
            // Get token usage for the last month
            var endDate = DateTimeOffset.UtcNow;
            var startDate = new DateTimeOffset(endDate.Year, endDate.Month, 1, 0, 0, 0, TimeSpan.Zero);
            if (endDate.Day > 1)
            {
                startDate = startDate.AddMonths(-1);
            }
            
            var summary = await _tokenUsageService.GetUsageSummaryAsync(startDate, endDate);
            
            _logger.LogInformation("Cost report generated: {TotalTokens} tokens, {TotalCost:C} cost",
                summary.TotalTokens, summary.TotalEstimatedCostUsd);
            
            // In a real implementation, this would generate a detailed cost report
            // and send it to the specified recipients
            
            _telemetryService.TrackEvent("CostReportGenerated", 
                new Dictionary<string, string>
                {
                    ["StartDate"] = startDate.ToString("yyyy-MM-dd"),
                    ["EndDate"] = endDate.ToString("yyyy-MM-dd")
                },
                new Dictionary<string, double>
                {
                    ["TotalTokens"] = summary.TotalTokens,
                    ["TotalCost"] = (double)summary.TotalEstimatedCostUsd
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate cost report");
            _telemetryService.TrackException(ex);
            throw;
        }
    }
}
