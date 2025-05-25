using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Logging;
using LLMGateway.Core.Interfaces;

namespace LLMGateway.Infrastructure.Telemetry;

/// <summary>
/// Implementation of metrics service using .NET Metrics API
/// </summary>
public class MetricsService : IMetricsService, IDisposable
{
    private readonly ILogger<MetricsService> _logger;
    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _completionCounter;
    private readonly Counter<long> _embeddingCounter;
    private readonly Counter<long> _cacheAccessCounter;
    private readonly Counter<long> _rateLimitCounter;
    private readonly Counter<long> _circuitBreakerCounter;
    private readonly Counter<long> _contentFilterCounter;
    private readonly Counter<long> _tokenUsageCounter;
    private readonly Counter<long> _providerHealthCounter;

    // Histograms
    private readonly Histogram<double> _completionDuration;
    private readonly Histogram<double> _embeddingDuration;
    private readonly Histogram<double> _providerResponseTime;
    private readonly Histogram<long> _tokenCount;
    private readonly Histogram<decimal> _cost;

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
        _meter = new Meter("LLMGateway", "1.0.0");

        // Initialize counters
        _completionCounter = _meter.CreateCounter<long>(
            "llm_completions_total",
            description: "Total number of completion requests");

        _embeddingCounter = _meter.CreateCounter<long>(
            "llm_embeddings_total",
            description: "Total number of embedding requests");

        _cacheAccessCounter = _meter.CreateCounter<long>(
            "cache_access_total",
            description: "Total number of cache access attempts");

        _rateLimitCounter = _meter.CreateCounter<long>(
            "rate_limit_total",
            description: "Total number of rate limit checks");

        _circuitBreakerCounter = _meter.CreateCounter<long>(
            "circuit_breaker_state_changes_total",
            description: "Total number of circuit breaker state changes");

        _contentFilterCounter = _meter.CreateCounter<long>(
            "content_filter_total",
            description: "Total number of content filtering operations");

        _tokenUsageCounter = _meter.CreateCounter<long>(
            "token_usage_total",
            description: "Total number of tokens processed");

        _providerHealthCounter = _meter.CreateCounter<long>(
            "provider_health_checks_total",
            description: "Total number of provider health checks");

        // Initialize histograms
        _completionDuration = _meter.CreateHistogram<double>(
            "llm_completion_duration_ms",
            description: "Duration of completion requests in milliseconds");

        _embeddingDuration = _meter.CreateHistogram<double>(
            "llm_embedding_duration_ms",
            description: "Duration of embedding requests in milliseconds");

        _providerResponseTime = _meter.CreateHistogram<double>(
            "provider_response_time_ms",
            description: "Provider response time in milliseconds");

        _tokenCount = _meter.CreateHistogram<long>(
            "token_count",
            description: "Number of tokens in requests");

        _cost = _meter.CreateHistogram<decimal>(
            "request_cost_usd",
            description: "Estimated cost of requests in USD");
    }

    /// <inheritdoc/>
    public void RecordCompletion(string provider, string model, bool success, double duration, int tokenCount = 0)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("provider", provider),
            new("model", model),
            new("success", success)
        };

        _completionCounter.Add(1, tags);
        _completionDuration.Record(duration, tags);

        if (tokenCount > 0)
        {
            _tokenCount.Record(tokenCount, tags);
        }

        _logger.LogDebug("Recorded completion metric: provider={Provider}, model={Model}, success={Success}, duration={Duration}ms",
            provider, model, success, duration);
    }

    /// <inheritdoc/>
    public void RecordEmbedding(string provider, string model, bool success, double duration, int inputCount = 0)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("provider", provider),
            new("model", model),
            new("success", success)
        };

        _embeddingCounter.Add(1, tags);
        _embeddingDuration.Record(duration, tags);

        if (inputCount > 0)
        {
            var inputTags = new KeyValuePair<string, object?>[]
            {
                new("provider", provider),
                new("model", model),
                new("success", success),
                new("input_count", inputCount)
            };
            _tokenCount.Record(inputCount, inputTags);
        }

        _logger.LogDebug("Recorded embedding metric: provider={Provider}, model={Model}, success={Success}, duration={Duration}ms",
            provider, model, success, duration);
    }

    /// <inheritdoc/>
    public void RecordCacheAccess(string cacheType, bool hit)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("cache_type", cacheType),
            new("hit", hit)
        };

        _cacheAccessCounter.Add(1, tags);

        _logger.LogDebug("Recorded cache access: type={CacheType}, hit={Hit}", cacheType, hit);
    }

    /// <inheritdoc/>
    public void RecordRateLimit(string apiKey, string resource, bool allowed)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("api_key_hash", HashApiKey(apiKey)),
            new("resource", resource),
            new("allowed", allowed)
        };

        _rateLimitCounter.Add(1, tags);

        _logger.LogDebug("Recorded rate limit: resource={Resource}, allowed={Allowed}", resource, allowed);
    }

    /// <inheritdoc/>
    public void RecordCircuitBreakerStateChange(string circuitKey, string state)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("circuit_key", circuitKey),
            new("state", state)
        };

        _circuitBreakerCounter.Add(1, tags);

        _logger.LogDebug("Recorded circuit breaker state change: key={CircuitKey}, state={State}", circuitKey, state);
    }

    /// <inheritdoc/>
    public void RecordContentFiltering(string contentType, bool filtered, string? reason = null)
    {
        var tagsList = new List<KeyValuePair<string, object?>>
        {
            new("content_type", contentType),
            new("filtered", filtered)
        };

        if (!string.IsNullOrEmpty(reason))
        {
            tagsList.Add(new("reason", reason));
        }

        var tags = tagsList.ToArray();
        _contentFilterCounter.Add(1, tags);

        _logger.LogDebug("Recorded content filtering: type={ContentType}, filtered={Filtered}, reason={Reason}",
            contentType, filtered, reason);
    }

    /// <inheritdoc/>
    public void RecordTokenUsage(string provider, string model, int promptTokens, int completionTokens, decimal cost = 0)
    {
        var promptTags = new KeyValuePair<string, object?>[]
        {
            new("provider", provider),
            new("model", model),
            new("token_type", "prompt")
        };

        _tokenUsageCounter.Add(promptTokens, promptTags);

        var completionTags = new KeyValuePair<string, object?>[]
        {
            new("provider", provider),
            new("model", model),
            new("token_type", "completion")
        };
        _tokenUsageCounter.Add(completionTokens, completionTags);

        if (cost > 0)
        {
            var costTags = new KeyValuePair<string, object?>[]
            {
                new("provider", provider),
                new("model", model)
            };
            _cost.Record(cost, costTags);
        }

        _logger.LogDebug("Recorded token usage: provider={Provider}, model={Model}, prompt={PromptTokens}, completion={CompletionTokens}, cost={Cost}",
            provider, model, promptTokens, completionTokens, cost);
    }

    /// <inheritdoc/>
    public void RecordProviderHealth(string provider, bool healthy, double responseTime)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("provider", provider),
            new("healthy", healthy)
        };

        _providerHealthCounter.Add(1, tags);
        _providerResponseTime.Record(responseTime, tags);

        _logger.LogDebug("Recorded provider health: provider={Provider}, healthy={Healthy}, responseTime={ResponseTime}ms",
            provider, healthy, responseTime);
    }

    /// <inheritdoc/>
    public void RecordCustomMetric(string name, double value, Dictionary<string, object>? tags = null)
    {
        // For custom metrics, we'll use a generic histogram
        var histogram = _meter.CreateHistogram<double>(name);

        var tagArray = Array.Empty<KeyValuePair<string, object?>>();
        if (tags != null)
        {
            tagArray = tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray();
        }

        histogram.Record(value, tagArray);

        _logger.LogDebug("Recorded custom metric: name={Name}, value={Value}", name, value);
    }

    /// <inheritdoc/>
    public void IncrementCounter(string name, Dictionary<string, object>? tags = null)
    {
        var counter = _meter.CreateCounter<long>(name);

        var tagArray = Array.Empty<KeyValuePair<string, object?>>();
        if (tags != null)
        {
            tagArray = tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray();
        }

        counter.Add(1, tagArray);

        _logger.LogDebug("Incremented counter: name={Name}", name);
    }

    private string HashApiKey(string apiKey)
    {
        // Simple hash for privacy - in production, use a proper hashing algorithm
        return apiKey.Length > 8 ? $"{apiKey[..4]}****{apiKey[^4..]}" : "****";
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}
