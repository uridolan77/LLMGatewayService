using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using LLMGateway.Core.Interfaces;

namespace LLMGateway.Infrastructure.HealthChecks;

/// <summary>
/// Health check for LLM providers
/// </summary>
public class LLMProviderHealthCheck : IHealthCheck
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILogger<LLMProviderHealthCheck> _logger;
    private readonly IMetricsService _metricsService;

    public LLMProviderHealthCheck(
        ILLMProviderFactory providerFactory,
        ILogger<LLMProviderHealthCheck> logger,
        IMetricsService metricsService)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var healthData = new Dictionary<string, object>();
        var unhealthyProviders = new List<string>();
        var degradedProviders = new List<string>();

        try
        {
            _logger.LogDebug("Starting LLM provider health check");

            var providers = _providerFactory.GetAllProviders();
            var healthCheckTasks = providers.Select(async provider =>
            {
                var providerStopwatch = Stopwatch.StartNew();
                try
                {
                    _logger.LogDebug("Checking health for provider {ProviderName}", provider.Name);

                    var isHealthy = await provider.IsAvailableAsync();
                    providerStopwatch.Stop();

                    var responseTime = providerStopwatch.Elapsed.TotalMilliseconds;
                    
                    // Record provider health metrics
                    _metricsService.RecordProviderHealth(provider.Name, isHealthy, responseTime);

                    var status = DetermineProviderStatus(isHealthy, responseTime);
                    
                    healthData[$"{provider.Name}_healthy"] = isHealthy;
                    healthData[$"{provider.Name}_response_time_ms"] = responseTime;
                    healthData[$"{provider.Name}_status"] = status.ToString();

                    _logger.LogDebug("Provider {ProviderName} health check completed: {Status} ({ResponseTime}ms)",
                        provider.Name, status, responseTime);

                    return new { Provider = provider.Name, Status = status, ResponseTime = responseTime };
                }
                catch (Exception ex)
                {
                    providerStopwatch.Stop();
                    var responseTime = providerStopwatch.Elapsed.TotalMilliseconds;
                    
                    _logger.LogWarning(ex, "Health check failed for provider {ProviderName}", provider.Name);
                    
                    // Record failed health check
                    _metricsService.RecordProviderHealth(provider.Name, false, responseTime);

                    healthData[$"{provider.Name}_healthy"] = false;
                    healthData[$"{provider.Name}_response_time_ms"] = responseTime;
                    healthData[$"{provider.Name}_status"] = ProviderHealthStatus.Unhealthy.ToString();
                    healthData[$"{provider.Name}_error"] = ex.Message;

                    return new { Provider = provider.Name, Status = ProviderHealthStatus.Unhealthy, ResponseTime = responseTime };
                }
            });

            var results = await Task.WhenAll(healthCheckTasks);

            // Categorize providers by health status
            foreach (var result in results)
            {
                switch (result.Status)
                {
                    case ProviderHealthStatus.Unhealthy:
                        unhealthyProviders.Add(result.Provider);
                        break;
                    case ProviderHealthStatus.Degraded:
                        degradedProviders.Add(result.Provider);
                        break;
                }
            }

            stopwatch.Stop();

            // Add overall health data
            healthData["total_providers"] = results.Length;
            healthData["healthy_providers"] = results.Count(r => r.Status == ProviderHealthStatus.Healthy);
            healthData["degraded_providers"] = degradedProviders.Count;
            healthData["unhealthy_providers"] = unhealthyProviders.Count;
            healthData["check_duration_ms"] = stopwatch.Elapsed.TotalMilliseconds;

            // Determine overall health status
            var overallStatus = DetermineOverallStatus(unhealthyProviders.Count, degradedProviders.Count, results.Length);
            var description = CreateStatusDescription(overallStatus, unhealthyProviders, degradedProviders);

            _logger.LogInformation("LLM provider health check completed: {Status} - {Description} ({Duration}ms)",
                overallStatus, description, stopwatch.Elapsed.TotalMilliseconds);

            return new HealthCheckResult(overallStatus, description, data: healthData);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LLM provider health check failed");

            healthData["error"] = ex.Message;
            healthData["check_duration_ms"] = stopwatch.Elapsed.TotalMilliseconds;

            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                "Health check failed with exception",
                ex,
                healthData);
        }
    }

    private static ProviderHealthStatus DetermineProviderStatus(bool isHealthy, double responseTimeMs)
    {
        if (!isHealthy)
        {
            return ProviderHealthStatus.Unhealthy;
        }

        // Consider provider degraded if response time is too high
        if (responseTimeMs > 5000) // 5 seconds
        {
            return ProviderHealthStatus.Degraded;
        }

        return ProviderHealthStatus.Healthy;
    }

    private static HealthStatus DetermineOverallStatus(int unhealthyCount, int degradedCount, int totalCount)
    {
        // If more than half are unhealthy, overall status is unhealthy
        if (unhealthyCount > totalCount / 2)
        {
            return HealthStatus.Unhealthy;
        }

        // If any are unhealthy or degraded, overall status is degraded
        if (unhealthyCount > 0 || degradedCount > 0)
        {
            return HealthStatus.Degraded;
        }

        // All providers are healthy
        return HealthStatus.Healthy;
    }

    private static string CreateStatusDescription(HealthStatus status, List<string> unhealthyProviders, List<string> degradedProviders)
    {
        return status switch
        {
            HealthStatus.Healthy => "All LLM providers are healthy",
            HealthStatus.Degraded => CreateDegradedDescription(unhealthyProviders, degradedProviders),
            HealthStatus.Unhealthy => $"Multiple providers unhealthy: {string.Join(", ", unhealthyProviders)}",
            _ => "Unknown health status"
        };
    }

    private static string CreateDegradedDescription(List<string> unhealthyProviders, List<string> degradedProviders)
    {
        var parts = new List<string>();

        if (unhealthyProviders.Any())
        {
            parts.Add($"Unhealthy: {string.Join(", ", unhealthyProviders)}");
        }

        if (degradedProviders.Any())
        {
            parts.Add($"Degraded: {string.Join(", ", degradedProviders)}");
        }

        return string.Join("; ", parts);
    }
}

/// <summary>
/// Provider health status enumeration
/// </summary>
public enum ProviderHealthStatus
{
    /// <summary>
    /// Provider is healthy and responding normally
    /// </summary>
    Healthy,

    /// <summary>
    /// Provider is responding but with degraded performance
    /// </summary>
    Degraded,

    /// <summary>
    /// Provider is not responding or returning errors
    /// </summary>
    Unhealthy
}
