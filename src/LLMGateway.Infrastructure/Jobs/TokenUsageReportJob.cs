using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace LLMGateway.Infrastructure.Jobs;

/// <summary>
/// Job for generating token usage reports
/// </summary>
[DisallowConcurrentExecution]
public class TokenUsageReportJob : IJob
{
    private readonly ITokenUsageService _tokenUsageService;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<TokenUsageReportJob> _logger;
    private readonly BackgroundJobOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenUsageService">Token usage service</param>
    /// <param name="telemetryService">Telemetry service</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Background job options</param>
    public TokenUsageReportJob(
        ITokenUsageService tokenUsageService,
        ITelemetryService telemetryService,
        ILogger<TokenUsageReportJob> logger,
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
        using var operation = _telemetryService.TrackOperation("TokenUsageReportJob.Execute");
        
        try
        {
            _logger.LogInformation("Generating token usage report");
            
            // Get token usage for the last day
            var endDate = DateTimeOffset.UtcNow;
            var startDate = endDate.AddDays(-1);
            
            var summary = await _tokenUsageService.GetUsageSummaryAsync(startDate, endDate);
            
            _logger.LogInformation("Token usage report generated: {TotalTokens} tokens, {TotalCost:C} cost",
                summary.TotalTokens, summary.TotalEstimatedCostUsd);
            
            // In a real implementation, this would send an email or generate a report file
            
            _telemetryService.TrackEvent("TokenUsageReportGenerated", 
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
            _logger.LogError(ex, "Failed to generate token usage report");
            _telemetryService.TrackException(ex);
            throw;
        }
    }
}
