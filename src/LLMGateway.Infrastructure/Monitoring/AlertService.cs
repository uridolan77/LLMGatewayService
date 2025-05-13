using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Infrastructure.Monitoring;

/// <summary>
/// Alert service
/// </summary>
public class AlertService : IAlertService
{
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<AlertService> _logger;
    private readonly MonitoringOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="telemetryService">Telemetry service</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Monitoring options</param>
    public AlertService(
        ITelemetryService telemetryService,
        ILogger<AlertService> logger,
        IOptions<MonitoringOptions> options)
    {
        _telemetryService = telemetryService;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public Task SendProviderUnavailableAlertAsync(string provider, string? errorMessage = null)
    {
        using var operation = _telemetryService.TrackOperation("AlertService.SendProviderUnavailableAlertAsync");
        
        if (!_options.EnableAlerts)
        {
            return Task.CompletedTask;
        }
        
        _logger.LogWarning("ALERT: Provider {Provider} is unavailable. Error: {ErrorMessage}", 
            provider, errorMessage ?? "Unknown error");
        
        _telemetryService.TrackEvent("ProviderUnavailableAlert",
            new Dictionary<string, string>
            {
                ["Provider"] = provider,
                ["ErrorMessage"] = errorMessage ?? "Unknown error"
            });
        
        // In a real implementation, this would send an email or other notification
        // For example:
        // await _emailService.SendEmailAsync(
        //     _options.AlertEmails,
        //     $"[ALERT] Provider {provider} is unavailable",
        //     $"Provider {provider} is unavailable. Error: {errorMessage ?? "Unknown error"}");
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendModelPerformanceAlertAsync(string modelId, string provider, double successRate, double averageResponseTimeMs)
    {
        using var operation = _telemetryService.TrackOperation("AlertService.SendModelPerformanceAlertAsync");
        
        if (!_options.EnableAlerts)
        {
            return Task.CompletedTask;
        }
        
        _logger.LogWarning("ALERT: Model {ModelId} performance issue. Success rate: {SuccessRate:P}, Average response time: {AverageResponseTime}ms",
            modelId, successRate, averageResponseTimeMs);
        
        _telemetryService.TrackEvent("ModelPerformanceAlert",
            new Dictionary<string, string>
            {
                ["ModelId"] = modelId,
                ["Provider"] = provider
            },
            new Dictionary<string, double>
            {
                ["SuccessRate"] = successRate,
                ["AverageResponseTimeMs"] = averageResponseTimeMs
            });
        
        // In a real implementation, this would send an email or other notification
        // For example:
        // await _emailService.SendEmailAsync(
        //     _options.AlertEmails,
        //     $"[ALERT] Model {modelId} performance issue",
        //     $"Model {modelId} from provider {provider} has a performance issue. " +
        //     $"Success rate: {successRate:P}, Average response time: {averageResponseTimeMs}ms");
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendTokenUsageAlertAsync(string userId, string apiKeyId, double usagePercentage, int limit)
    {
        using var operation = _telemetryService.TrackOperation("AlertService.SendTokenUsageAlertAsync");
        
        if (!_options.EnableAlerts)
        {
            return Task.CompletedTask;
        }
        
        _logger.LogWarning("ALERT: Token usage for user {UserId} (API key {ApiKeyId}) has reached {UsagePercentage:P} of the limit ({Limit} tokens)",
            userId, apiKeyId, usagePercentage, limit);
        
        _telemetryService.TrackEvent("TokenUsageAlert",
            new Dictionary<string, string>
            {
                ["UserId"] = userId,
                ["ApiKeyId"] = apiKeyId
            },
            new Dictionary<string, double>
            {
                ["UsagePercentage"] = usagePercentage,
                ["Limit"] = limit
            });
        
        // In a real implementation, this would send an email or other notification
        // For example:
        // await _emailService.SendEmailAsync(
        //     _options.AlertEmails,
        //     $"[ALERT] Token usage limit approaching for user {userId}",
        //     $"Token usage for user {userId} (API key {apiKeyId}) has reached {usagePercentage:P} of the limit ({limit} tokens)");
        
        return Task.CompletedTask;
    }
}
