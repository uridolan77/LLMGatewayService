using System;
using System.Threading.Tasks;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Service for implementing circuit breaker pattern to handle provider failures gracefully
/// </summary>
public interface ICircuitBreakerService
{
    /// <summary>
    /// Execute an operation with circuit breaker protection
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="key">Unique key to identify the circuit (e.g., provider name)</param>
    /// <param name="operation">The operation to execute</param>
    /// <param name="failureThreshold">Number of failures before opening the circuit</param>
    /// <param name="timeout">How long to keep the circuit open</param>
    /// <returns>Result of the operation</returns>
    Task<T> ExecuteAsync<T>(string key, Func<Task<T>> operation, int failureThreshold = 5, TimeSpan timeout = default);

    /// <summary>
    /// Check if a circuit is currently open
    /// </summary>
    /// <param name="key">Circuit key</param>
    /// <returns>True if circuit is open (failing)</returns>
    bool IsCircuitOpen(string key);

    /// <summary>
    /// Get circuit state information
    /// </summary>
    /// <param name="key">Circuit key</param>
    /// <returns>Circuit state information</returns>
    CircuitState GetCircuitState(string key);

    /// <summary>
    /// Reset a circuit to closed state
    /// </summary>
    /// <param name="key">Circuit key</param>
    void ResetCircuit(string key);
}

/// <summary>
/// Represents the state of a circuit breaker
/// </summary>
public class CircuitState
{
    /// <summary>
    /// Whether the circuit is open (failing)
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// When the circuit was opened
    /// </summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    /// When the circuit will be eligible to try again
    /// </summary>
    public DateTime? OpenUntil { get; set; }

    /// <summary>
    /// Last exception that caused the circuit to open
    /// </summary>
    public Exception? LastException { get; set; }

    /// <summary>
    /// Total number of requests through this circuit
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Total number of successful requests
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Success rate (0.0 to 1.0)
    /// </summary>
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0.0;
}
