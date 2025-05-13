using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for token usage repository
/// </summary>
public interface ITokenUsageRepository : IRepository<TokenUsageRecord>
{
    /// <summary>
    /// Get token usage by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetByUserIdAsync(string userId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
    
    /// <summary>
    /// Get token usage by API key ID
    /// </summary>
    /// <param name="apiKeyId">API key ID</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetByApiKeyIdAsync(string apiKeyId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
    
    /// <summary>
    /// Get token usage by model ID
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetByModelIdAsync(string modelId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
    
    /// <summary>
    /// Get token usage by provider
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetByProviderAsync(string provider, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
    
    /// <summary>
    /// Get total token usage by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Total token usage</returns>
    Task<int> GetTotalTokensByUserIdAsync(string userId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
    
    /// <summary>
    /// Get total token usage by API key ID
    /// </summary>
    /// <param name="apiKeyId">API key ID</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Total token usage</returns>
    Task<int> GetTotalTokensByApiKeyIdAsync(string apiKeyId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
    
    /// <summary>
    /// Get total cost by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Total cost</returns>
    Task<decimal> GetTotalCostByUserIdAsync(string userId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
    
    /// <summary>
    /// Get daily usage statistics
    /// </summary>
    /// <param name="days">Number of days</param>
    /// <returns>Daily usage statistics</returns>
    Task<IEnumerable<DailyUsageStats>> GetDailyUsageStatsAsync(int days);
}

/// <summary>
/// Daily usage statistics
/// </summary>
public class DailyUsageStats
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
    
    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }
    
    /// <summary>
    /// Request count
    /// </summary>
    public int RequestCount { get; set; }
}

/// <summary>
/// Token usage repository
/// </summary>
public class TokenUsageRepository : Repository<TokenUsageRecord>, ITokenUsageRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public TokenUsageRepository(LLMGatewayDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<TokenUsageRecord>> GetByUserIdAsync(string userId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
    {
        var query = _dbSet.Where(r => r.UserId == userId);
        
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
    public async Task<IEnumerable<TokenUsageRecord>> GetByApiKeyIdAsync(string apiKeyId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
    {
        var query = _dbSet.Where(r => r.ApiKeyId == apiKeyId);
        
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
    public async Task<IEnumerable<TokenUsageRecord>> GetByModelIdAsync(string modelId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
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
    public async Task<IEnumerable<TokenUsageRecord>> GetByProviderAsync(string provider, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
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
    public async Task<int> GetTotalTokensByUserIdAsync(string userId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
    {
        var query = _dbSet.Where(r => r.UserId == userId);
        
        if (startTime.HasValue)
        {
            query = query.Where(r => r.Timestamp >= startTime);
        }
        
        if (endTime.HasValue)
        {
            query = query.Where(r => r.Timestamp <= endTime);
        }
        
        return await query.SumAsync(r => r.TotalTokens);
    }
    
    /// <inheritdoc/>
    public async Task<int> GetTotalTokensByApiKeyIdAsync(string apiKeyId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
    {
        var query = _dbSet.Where(r => r.ApiKeyId == apiKeyId);
        
        if (startTime.HasValue)
        {
            query = query.Where(r => r.Timestamp >= startTime);
        }
        
        if (endTime.HasValue)
        {
            query = query.Where(r => r.Timestamp <= endTime);
        }
        
        return await query.SumAsync(r => r.TotalTokens);
    }
    
    /// <inheritdoc/>
    public async Task<decimal> GetTotalCostByUserIdAsync(string userId, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
    {
        var query = _dbSet.Where(r => r.UserId == userId);
        
        if (startTime.HasValue)
        {
            query = query.Where(r => r.Timestamp >= startTime);
        }
        
        if (endTime.HasValue)
        {
            query = query.Where(r => r.Timestamp <= endTime);
        }
        
        return await query.SumAsync(r => r.EstimatedCostUsd);
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<DailyUsageStats>> GetDailyUsageStatsAsync(int days)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        
        return await _dbSet
            .Where(r => r.Timestamp >= new DateTimeOffset(startDate, TimeSpan.Zero))
            .GroupBy(r => new { Day = r.Timestamp.UtcDateTime.Date })
            .Select(g => new DailyUsageStats
            {
                Date = g.Key.Day,
                TotalTokens = g.Sum(r => r.TotalTokens),
                TotalCost = g.Sum(r => r.EstimatedCostUsd),
                RequestCount = g.Count()
            })
            .OrderBy(s => s.Date)
            .ToListAsync();
    }
}
