using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace LLMGateway.Infrastructure.Jobs;

/// <summary>
/// Job for aggregating model metrics
/// </summary>
[DisallowConcurrentExecution]
public class ModelMetricsAggregationJob : IJob
{
    private readonly ITokenUsageService _tokenUsageService;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<ModelMetricsAggregationJob> _logger;
    private readonly MonitoringOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenUsageService">Token usage service</param>
    /// <param name="telemetryService">Telemetry service</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Monitoring options</param>
    public ModelMetricsAggregationJob(
        ITokenUsageService tokenUsageService,
        ITelemetryService telemetryService,
        ILogger<ModelMetricsAggregationJob> logger,
        IOptions<MonitoringOptions> options)
    {
        _tokenUsageService = tokenUsageService;
        _telemetryService = telemetryService;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task Execute(IJobExecutionContext context)
    {
        using var operation = _telemetryService.TrackOperation("ModelMetricsAggregationJob.Execute");
        
        try
        {
            _logger.LogInformation("Aggregating model metrics");
            
            // Get token usage for the last hour
            var endDate = DateTimeOffset.UtcNow;
            var startDate = endDate.AddHours(-1);
            
            var summary = await _tokenUsageService.GetUsageSummaryAsync(startDate, endDate);
            
            _logger.LogInformation("Model metrics aggregated: {ModelCount} models, {TotalTokens} tokens",
                summary.UsageByModel.Count, summary.TotalTokens);
            
            // Track metrics for each model
            foreach (var modelUsage in summary.UsageByModel)
            {
                _telemetryService.TrackEvent("ModelMetrics", 
                    new Dictionary<string, string>
                    {
                        ["ModelId"] = modelUsage.Key,
                        ["Provider"] = modelUsage.Value.Provider
                    },
                    new Dictionary<string, double>
                    {
                        ["PromptTokens"] = modelUsage.Value.PromptTokens,
                        ["CompletionTokens"] = modelUsage.Value.CompletionTokens,
                        ["TotalTokens"] = modelUsage.Value.TotalTokens,
                        ["Cost"] = (double)modelUsage.Value.EstimatedCostUsd
                    });
            }
            
            // In a real implementation, this would store the metrics in a database
            // for later analysis and visualization
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to aggregate model metrics");
            _telemetryService.TrackException(ex);
            throw;
        }
    }
}
