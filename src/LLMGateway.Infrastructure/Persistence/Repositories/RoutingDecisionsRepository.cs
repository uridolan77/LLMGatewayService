using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for routing decisions repository
/// </summary>
public interface IRoutingDecisionsRepository : IRepository<Entities.RoutingDecision>
{
    /// <summary>
    /// Get routing decisions by requested model
    /// </summary>
    /// <param name="modelId">Requested model ID</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Routing decisions</returns>
    Task<IEnumerable<Entities.RoutingDecision>> GetByRequestedModelAsync(string modelId, int limit = 100);
    
    /// <summary>
    /// Get routing decisions by selected model
    /// </summary>
    /// <param name="modelId">Selected model ID</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Routing decisions</returns>
    Task<IEnumerable<Entities.RoutingDecision>> GetBySelectedModelAsync(string modelId, int limit = 100);
    
    /// <summary>
    /// Get routing decisions by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Routing decisions</returns>
    Task<IEnumerable<Entities.RoutingDecision>> GetByUserIdAsync(string userId, int limit = 100);
    
    /// <summary>
    /// Get routing decisions by strategy
    /// </summary>
    /// <param name="strategy">Routing strategy</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Routing decisions</returns>
    Task<IEnumerable<Entities.RoutingDecision>> GetByStrategyAsync(string strategy, int limit = 100);
    
    /// <summary>
    /// Get recent routing decisions
    /// </summary>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Recent routing decisions</returns>
    Task<IEnumerable<Entities.RoutingDecision>> GetRecentAsync(int limit = 100);
    
    /// <summary>
    /// Get routing statistics by requested model
    /// </summary>
    /// <returns>Routing statistics by requested model</returns>
    Task<IEnumerable<RoutingStatistics>> GetStatisticsByRequestedModelAsync();
    
    /// <summary>
    /// Get routing statistics by selected model
    /// </summary>
    /// <returns>Routing statistics by selected model</returns>
    Task<IEnumerable<RoutingStatistics>> GetStatisticsBySelectedModelAsync();
}

/// <summary>
/// Routing statistics
/// </summary>
public class RoutingStatistics
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Request count
    /// </summary>
    public int RequestCount { get; set; }
    
    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }
    
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
}

/// <summary>
/// Routing decisions repository
/// </summary>
public class RoutingDecisionsRepository : Repository<Entities.RoutingDecision>, IRoutingDecisionsRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public RoutingDecisionsRepository(LLMGatewayDbContext context) : base(context)
    {
    }
      /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RoutingDecision>> GetByRequestedModelAsync(string modelId, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.RequestedModelId == modelId)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
      /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RoutingDecision>> GetBySelectedModelAsync(string modelId, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.SelectedModelId == modelId)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
      /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RoutingDecision>> GetByUserIdAsync(string userId, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
      /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RoutingDecision>> GetByStrategyAsync(string strategy, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.Strategy == strategy)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
      /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RoutingDecision>> GetRecentAsync(int limit = 100)
    {
        return await _dbSet
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<RoutingStatistics>> GetStatisticsByRequestedModelAsync()
    {
        return await _dbSet
            .GroupBy(r => r.RequestedModelId)
            .Select(g => new RoutingStatistics
            {
                ModelId = g.Key,
                RequestCount = g.Count(),
                SuccessRate = g.Count() > 0 ? (double)g.Count(r => r.WasSuccessful) / g.Count() : 0,
                AverageResponseTimeMs = g.Count() > 0 ? g.Average(r => r.ResponseTimeMs) : 0
            })
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<RoutingStatistics>> GetStatisticsBySelectedModelAsync()
    {
        return await _dbSet
            .GroupBy(r => r.SelectedModelId)
            .Select(g => new RoutingStatistics
            {
                ModelId = g.Key,
                RequestCount = g.Count(),
                SuccessRate = g.Count() > 0 ? (double)g.Count(r => r.WasSuccessful) / g.Count() : 0,
                AverageResponseTimeMs = g.Count() > 0 ? g.Average(r => r.ResponseTimeMs) : 0
            })
            .ToListAsync();
    }
}
