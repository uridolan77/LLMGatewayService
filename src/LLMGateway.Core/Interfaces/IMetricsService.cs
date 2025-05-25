using System;
using System.Collections.Generic;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Service for collecting and recording custom metrics
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Record a completion request
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="model">Model name</param>
    /// <param name="success">Whether the request was successful</param>
    /// <param name="duration">Request duration in milliseconds</param>
    /// <param name="tokenCount">Number of tokens processed</param>
    void RecordCompletion(string provider, string model, bool success, double duration, int tokenCount = 0);

    /// <summary>
    /// Record an embedding request
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="model">Model name</param>
    /// <param name="success">Whether the request was successful</param>
    /// <param name="duration">Request duration in milliseconds</param>
    /// <param name="inputCount">Number of inputs processed</param>
    void RecordEmbedding(string provider, string model, bool success, double duration, int inputCount = 0);

    /// <summary>
    /// Record cache hit or miss
    /// </summary>
    /// <param name="cacheType">Type of cache (e.g., "completion", "embedding", "model")</param>
    /// <param name="hit">Whether it was a cache hit</param>
    void RecordCacheAccess(string cacheType, bool hit);

    /// <summary>
    /// Record rate limiting event
    /// </summary>
    /// <param name="apiKey">API key (hashed for privacy)</param>
    /// <param name="resource">Resource being rate limited</param>
    /// <param name="allowed">Whether the request was allowed</param>
    void RecordRateLimit(string apiKey, string resource, bool allowed);

    /// <summary>
    /// Record circuit breaker state change
    /// </summary>
    /// <param name="circuitKey">Circuit breaker key</param>
    /// <param name="state">New state (Open, Closed, HalfOpen)</param>
    void RecordCircuitBreakerStateChange(string circuitKey, string state);

    /// <summary>
    /// Record content filtering result
    /// </summary>
    /// <param name="contentType">Type of content (prompt, completion)</param>
    /// <param name="filtered">Whether content was filtered</param>
    /// <param name="reason">Reason for filtering (if applicable)</param>
    void RecordContentFiltering(string contentType, bool filtered, string? reason = null);

    /// <summary>
    /// Record token usage
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="model">Model name</param>
    /// <param name="promptTokens">Number of prompt tokens</param>
    /// <param name="completionTokens">Number of completion tokens</param>
    /// <param name="cost">Estimated cost</param>
    void RecordTokenUsage(string provider, string model, int promptTokens, int completionTokens, decimal cost = 0);

    /// <summary>
    /// Record provider health status
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="healthy">Whether the provider is healthy</param>
    /// <param name="responseTime">Response time in milliseconds</param>
    void RecordProviderHealth(string provider, bool healthy, double responseTime);

    /// <summary>
    /// Record custom metric
    /// </summary>
    /// <param name="name">Metric name</param>
    /// <param name="value">Metric value</param>
    /// <param name="tags">Additional tags</param>
    void RecordCustomMetric(string name, double value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Increment a counter metric
    /// </summary>
    /// <param name="name">Counter name</param>
    /// <param name="tags">Additional tags</param>
    void IncrementCounter(string name, Dictionary<string, object>? tags = null);
}
