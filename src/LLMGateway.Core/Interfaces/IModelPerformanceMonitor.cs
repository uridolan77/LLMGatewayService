using System.Collections.Generic;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for model performance monitor
/// </summary>
public interface IModelPerformanceMonitor
{
    /// <summary>
    /// Start monitoring
    /// </summary>
    void Start();
    
    /// <summary>
    /// Stop monitoring
    /// </summary>
    void Stop();
    
    /// <summary>
    /// Track model performance
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="provider">Provider</param>
    /// <param name="success">Whether the request was successful</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <param name="tokenCount">Token count</param>
    /// <param name="costUsd">Cost in USD</param>
    void TrackModelPerformance(string modelId, string provider, bool success, long responseTimeMs, int tokenCount, decimal costUsd);
    
    /// <summary>
    /// Get model performance metrics
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>Model performance metrics</returns>
    ModelPerformanceMetrics GetModelPerformanceMetrics(string modelId);
    
    /// <summary>
    /// Get all model performance metrics
    /// </summary>
    /// <returns>Model performance metrics</returns>
    Dictionary<string, ModelPerformanceMetrics> GetAllModelPerformanceMetrics();
}

/// <summary>
/// Model performance metrics
/// </summary>
public class ModelPerformanceMetrics
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Request count
    /// </summary>
    public int RequestCount { get; set; }
    
    /// <summary>
    /// Success count
    /// </summary>
    public int SuccessCount { get; set; }
    
    /// <summary>
    /// Failure count
    /// </summary>
    public int FailureCount { get; set; }
    
    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate => RequestCount > 0 ? (double)SuccessCount / RequestCount : 0;
    
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
    
    /// <summary>
    /// Total cost in USD
    /// </summary>
    public decimal TotalCostUsd { get; set; }
}
