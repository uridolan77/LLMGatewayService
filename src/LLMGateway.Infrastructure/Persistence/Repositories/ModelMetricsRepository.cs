using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for model metrics repository
/// </summary>
public interface IModelMetricsRepository : IRepository<ModelMetricsRecord>
{
    /// <summary>
    /// Get metrics by model ID
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Model metrics records</returns>
    Task<IEnumerable<ModelMetricsRecord>> GetByModelIdAsync(string modelId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
    
    /// <summary>
    /// Get metrics by provider
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Model metrics records</returns>
    Task<IEnumerable<ModelMetricsRecord>> GetByProviderAsync(string provider, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
    
    /// <summary>
    /// Get latest metrics for all models
    /// </summary>
    /// <returns>Latest model metrics records</returns>
    Task<IEnumerable<ModelMetricsRecord>> GetLatestMetricsAsync();
    
    /// <summary>
    /// Get latest metrics for a specific model
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>Latest model metrics record</returns>
    Task<ModelMetricsRecord?> GetLatestMetricsForModelAsync(string modelId);
    
    /// <summary>
    /// Get aggregated metrics by model ID
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Aggregated model metrics</returns>
    Task<AggregatedModelMetrics> GetAggregatedMetricsByModelIdAsync(string modelId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
}

/// <summary>
/// Aggregated model metrics
/// </summary>
public class AggregatedModelMetrics
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
    /// Total request count
    /// </summary>
    public int TotalRequestCount { get; set; }
    
    /// <summary>
    /// Total success count
    /// </summary>
    public int TotalSuccessCount { get; set; }
    
    /// <summary>
    /// Total failure count
    /// </summary>
    public int TotalFailureCount { get; set; }
    
    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate => TotalRequestCount > 0 ? (double)TotalSuccessCount / TotalRequestCount : 0;
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
    
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
    
    /// <summary>
    /// Total cost in USD
    /// </summary>
    public decimal TotalCostUsd { get; set; }
}

/// <summary>
/// Model metrics repository
/// </summary>
public class ModelMetricsRepository : Repository<ModelMetricsRecord>, IModelMetricsRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public ModelMetricsRepository(LLMGatewayDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<ModelMetricsRecord>> GetByModelIdAsync(string modelId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
    {
        var query = _dbSet.Where(r => r.ModelId == modelId);
        
        if (startTime.HasValue)
        {
            query = query.Where(r => r.Timestamp >= startTime);
        }
        
        if (endTime.HasValue)
        {
            query = query.Where(r => r.Timestamp <= endTime);
        }
        
        return await query.OrderByDescending(r => r.Timestamp).ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<ModelMetricsRecord>> GetByProviderAsync(string provider, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
    {
        var query = _dbSet.Where(r => r.Provider == provider);
        
        if (startTime.HasValue)
        {
            query = query.Where(r => r.Timestamp >= startTime);
        }
        
        if (endTime.HasValue)
        {
            query = query.Where(r => r.Timestamp <= endTime);
        }
        
        return await query.OrderByDescending(r => r.Timestamp).ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<ModelMetricsRecord>> GetLatestMetricsAsync()
    {
        return await _dbSet
            .GroupBy(r => r.ModelId)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<ModelMetricsRecord?> GetLatestMetricsForModelAsync(string modelId)
    {
        return await _dbSet
            .Where(r => r.ModelId == modelId)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync();
    }
    
    /// <inheritdoc/>
    public async Task<AggregatedModelMetrics> GetAggregatedMetricsByModelIdAsync(string modelId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
    {
        var query = _dbSet.Where(r => r.ModelId == modelId);
        
        if (startTime.HasValue)
        {
            query = query.Where(r => r.Timestamp >= startTime);
        }
        
        if (endTime.HasValue)
        {
            query = query.Where(r => r.Timestamp <= endTime);
        }
        
        var metrics = await query.ToListAsync();
        
        if (!metrics.Any())
        {
            return new AggregatedModelMetrics
            {
                ModelId = modelId,
                Provider = string.Empty
            };
        }
        
        return new AggregatedModelMetrics
        {
            ModelId = modelId,
            Provider = metrics.First().Provider,
            TotalRequestCount = metrics.Sum(r => r.RequestCount),
            TotalSuccessCount = metrics.Sum(r => r.SuccessCount),
            TotalFailureCount = metrics.Sum(r => r.FailureCount),
            TotalTokens = metrics.Sum(r => r.TotalTokens),
            AverageResponseTimeMs = metrics.Any() ? metrics.Average(r => r.AverageResponseTimeMs) : 0,
            TotalCostUsd = metrics.Sum(r => r.TotalCostUsd)
        };
    }
}
