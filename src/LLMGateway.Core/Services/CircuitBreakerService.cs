using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using LLMGateway.Core.Interfaces;

namespace LLMGateway.Core.Services;

/// <summary>
/// Implementation of circuit breaker pattern using Polly
/// </summary>
public class CircuitBreakerService : ICircuitBreakerService
{
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly ConcurrentDictionary<string, IAsyncPolicy> _policies = new();
    private readonly ConcurrentDictionary<string, CircuitState> _circuitStates = new();

    public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteAsync<T>(string key, Func<Task<T>> operation, int failureThreshold = 5, TimeSpan timeout = default)
    {
        if (timeout == default)
        {
            timeout = TimeSpan.FromMinutes(1);
        }

        var policy = _policies.GetOrAdd(key, k => CreateCircuitBreakerPolicy(k, failureThreshold, timeout));
        var state = _circuitStates.GetOrAdd(key, _ => new CircuitState());

        try
        {
            state.TotalRequests++;
            var result = await policy.ExecuteAsync(operation);
            state.SuccessfulRequests++;
            return result;
        }
        catch (Exception ex)
        {
            state.LastException = ex;
            _logger.LogWarning(ex, "Circuit breaker {Key} operation failed", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public bool IsCircuitOpen(string key)
    {
        if (!_circuitStates.TryGetValue(key, out var state))
        {
            return false;
        }

        return state.IsOpen && DateTime.UtcNow < state.OpenUntil;
    }

    /// <inheritdoc/>
    public CircuitState GetCircuitState(string key)
    {
        return _circuitStates.GetOrAdd(key, _ => new CircuitState());
    }

    /// <inheritdoc/>
    public void ResetCircuit(string key)
    {
        if (_circuitStates.TryGetValue(key, out var state))
        {
            state.IsOpen = false;
            state.FailureCount = 0;
            state.OpenedAt = null;
            state.OpenUntil = null;
            state.LastException = null;
        }

        // Remove and recreate the policy to reset Polly's internal state
        _policies.TryRemove(key, out _);
        
        _logger.LogInformation("Circuit breaker {Key} has been reset", key);
    }

    private IAsyncPolicy CreateCircuitBreakerPolicy(string key, int failureThreshold, TimeSpan timeout)
    {
        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                failureThreshold,
                timeout,
                onBreak: (exception, timespan) =>
                {
                    var state = _circuitStates.GetOrAdd(key, _ => new CircuitState());
                    state.IsOpen = true;
                    state.FailureCount++;
                    state.OpenedAt = DateTime.UtcNow;
                    state.OpenUntil = DateTime.UtcNow.Add(timespan);
                    state.LastException = exception;

                    _logger.LogWarning(exception, 
                        "Circuit breaker {Key} opened for {TimeSpan}s after {FailureCount} failures", 
                        key, timespan.TotalSeconds, state.FailureCount);
                },
                onReset: () =>
                {
                    var state = _circuitStates.GetOrAdd(key, _ => new CircuitState());
                    state.IsOpen = false;
                    state.FailureCount = 0;
                    state.OpenedAt = null;
                    state.OpenUntil = null;

                    _logger.LogInformation("Circuit breaker {Key} reset to closed state", key);
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker {Key} is half-open, testing with next request", key);
                });
    }
}
