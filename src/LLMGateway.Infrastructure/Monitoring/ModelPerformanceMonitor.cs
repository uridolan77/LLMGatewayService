using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Persistence;
using LLMGateway.Infrastructure.Persistence.Entities;
using LLMGateway.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace LLMGateway.Infrastructure.Monitoring;

/// <summary>
/// Model performance monitor
/// </summary>
public class ModelPerformanceMonitor : IModelPerformanceMonitor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IAlertService? _alertService;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<ModelPerformanceMonitor> _logger;
    private readonly MonitoringOptions _options;
    private readonly ConcurrentDictionary<string, ModelPerformanceMetrics> _modelMetrics = new();
    private Timer? _timer;
    private bool _isRunning;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceScopeFactory">Service scope factory</param>
    /// <param name="alertService">Alert service</param>
    /// <param name="telemetryService">Telemetry service</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Monitoring options</param>
    public ModelPerformanceMonitor(
        IServiceScopeFactory serviceScopeFactory,
        IAlertService? alertService,
        ITelemetryService telemetryService,
        ILogger<ModelPerformanceMonitor> logger,
        IOptions<MonitoringOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _alertService = alertService;
        _telemetryService = telemetryService;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public void Start()
    {
        if (_isRunning)
        {
            return;
        }
        
        _logger.LogInformation("Starting model performance monitoring");
        
        _timer = new Timer(
            AggregateMetricsCallback,
            null,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));
        
        _isRunning = true;
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }
        
        _logger.LogInformation("Stopping model performance monitoring");
        
        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();
        _timer = null;
        
        _isRunning = false;
    }

    /// <inheritdoc/>
    public void TrackModelPerformance(string modelId, string provider, bool success, long responseTimeMs, int tokenCount, decimal costUsd)
    {
        using var operation = _telemetryService.TrackOperation("ModelPerformanceMonitor.TrackModelPerformance");
        
        _logger.LogDebug("Tracking performance for model {ModelId}: success={Success}, responseTime={ResponseTime}ms, tokens={Tokens}, cost={Cost}",
            modelId, success, responseTimeMs, tokenCount, costUsd);
        
        var metrics = _modelMetrics.GetOrAdd(modelId, _ => new ModelPerformanceMetrics
        {
            ModelId = modelId,
            Provider = provider
        });
        
        // Update metrics
        metrics.RequestCount++;
        
        if (success)
        {
            metrics.SuccessCount++;
        }
        else
        {
            metrics.FailureCount++;
        }
        
        // Update average response time using a weighted average
        var currentAverage = metrics.AverageResponseTimeMs;
        var newAverage = metrics.RequestCount == 1 ? 
            responseTimeMs : 
            (currentAverage * (metrics.RequestCount - 1) + responseTimeMs) / metrics.RequestCount;
        
        metrics.AverageResponseTimeMs = newAverage;
        
        // Update token count and cost
        metrics.TotalTokens += tokenCount;
        
        // Update cost (not thread-safe, but acceptable for this use case)
        metrics.TotalCostUsd += costUsd;
        
        // Track telemetry
        _telemetryService.TrackEvent("ModelPerformance",
            new Dictionary<string, string>
            {
                ["ModelId"] = modelId,
                ["Provider"] = provider,
                ["Success"] = success.ToString()
            },
            new Dictionary<string, double>
            {
                ["ResponseTimeMs"] = responseTimeMs,
                ["TokenCount"] = tokenCount,
                ["Cost"] = (double)costUsd
            });
        
        // Check for alerts
        if (_options.EnableAlerts && _alertService != null)
        {
            // Alert on low success rate
            if (metrics.RequestCount >= 10 && metrics.SuccessRate < 0.8)
            {
                _alertService.SendModelPerformanceAlertAsync(
                    modelId, provider, metrics.SuccessRate, metrics.AverageResponseTimeMs);
            }
            
            // Alert on high response time
            if (metrics.RequestCount >= 10 && metrics.AverageResponseTimeMs > 5000)
            {
                _alertService.SendModelPerformanceAlertAsync(
                    modelId, provider, metrics.SuccessRate, metrics.AverageResponseTimeMs);
            }
        }
    }

    /// <inheritdoc/>
    public ModelPerformanceMetrics GetModelPerformanceMetrics(string modelId)
    {
        if (_modelMetrics.TryGetValue(modelId, out var metrics))
        {
            return metrics;
        }
        
        return new ModelPerformanceMetrics
        {
            ModelId = modelId
        };
    }

    /// <inheritdoc/>
    public Dictionary<string, ModelPerformanceMetrics> GetAllModelPerformanceMetrics()
    {
        return new Dictionary<string, ModelPerformanceMetrics>(_modelMetrics);
    }

    private async void AggregateMetricsCallback(object? state)
    {
        try
        {
            await AggregateMetricsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to aggregate model metrics");
        }
    }

    private async Task AggregateMetricsAsync()
    {
        using var operation = _telemetryService.TrackOperation("ModelPerformanceMonitor.AggregateMetricsAsync");
        
        _logger.LogInformation("Aggregating model metrics");
        
        if (!_options.TrackModelPerformance)
        {
            _logger.LogInformation("Model performance tracking is not enabled, skipping aggregation");
            return;
        }
        
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LLMGatewayDbContext>();
        
            foreach (var metrics in _modelMetrics)
            {
                try
                {
                    var record = new ModelMetricsRecord
                    {
                        ModelId = metrics.Value.ModelId,
                        Provider = metrics.Value.Provider,
                        Timestamp = DateTimeOffset.UtcNow,
                        RequestCount = metrics.Value.RequestCount,
                        SuccessCount = metrics.Value.SuccessCount,
                        FailureCount = metrics.Value.FailureCount,
                        TotalTokens = metrics.Value.TotalTokens,
                        AverageResponseTimeMs = (long)metrics.Value.AverageResponseTimeMs,
                        TotalCostUsd = metrics.Value.TotalCostUsd
                    };
                    
                    await dbContext.ModelMetricsRecords.AddAsync(record);
                    
                    _logger.LogInformation("Aggregated metrics for model {ModelId}: requests={Requests}, success={Success}, failure={Failure}, tokens={Tokens}, cost={Cost}",
                        metrics.Value.ModelId, metrics.Value.RequestCount, metrics.Value.SuccessCount, metrics.Value.FailureCount, metrics.Value.TotalTokens, metrics.Value.TotalCostUsd);
                    
                    // Reset metrics
                    _modelMetrics[metrics.Key] = new ModelPerformanceMetrics
                    {
                        ModelId = metrics.Value.ModelId,
                        Provider = metrics.Value.Provider
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to store model metrics record for {ModelId}", metrics.Key);
                }
            }
            
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save model metrics records");
            }
        }
    }
}
