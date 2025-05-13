using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for provider health repository
/// </summary>
public interface IProviderHealthRepository : IRepository<ProviderHealthRecord>
{
    /// <summary>
    /// Get health records by provider
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Provider health records</returns>
    Task<IEnumerable<ProviderHealthRecord>> GetByProviderAsync(string provider, int limit = 100);
    
    /// <summary>
    /// Get latest health record by provider
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <returns>Latest provider health record</returns>
    Task<ProviderHealthRecord?> GetLatestByProviderAsync(string provider);
    
    /// <summary>
    /// Get latest health records for all providers
    /// </summary>
    /// <returns>Latest provider health records</returns>
    Task<IEnumerable<ProviderHealthRecord>> GetLatestForAllProvidersAsync();
    
    /// <summary>
    /// Get health records for a time period
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Provider health records</returns>
    Task<IEnumerable<ProviderHealthRecord>> GetForTimeRangeAsync(string provider, DateTimeOffset startTime, DateTimeOffset endTime);
    
    /// <summary>
    /// Get providers with availability issues
    /// </summary>
    /// <param name="timeWindow">Time window in minutes</param>
    /// <returns>Providers with availability issues</returns>
    Task<IEnumerable<string>> GetProvidersWithAvailabilityIssuesAsync(int timeWindow = 30);
}

/// <summary>
/// Provider health repository
/// </summary>
public class ProviderHealthRepository : Repository<ProviderHealthRecord>, IProviderHealthRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public ProviderHealthRepository(LLMGatewayDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<ProviderHealthRecord>> GetByProviderAsync(string provider, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.Provider == provider)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<ProviderHealthRecord?> GetLatestByProviderAsync(string provider)
    {
        return await _dbSet
            .Where(r => r.Provider == provider)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<ProviderHealthRecord>> GetLatestForAllProvidersAsync()
    {
        return await _dbSet
            .GroupBy(r => r.Provider)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<ProviderHealthRecord>> GetForTimeRangeAsync(string provider, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return await _dbSet
            .Where(r => r.Provider == provider && r.Timestamp >= startTime && r.Timestamp <= endTime)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetProvidersWithAvailabilityIssuesAsync(int timeWindow = 30)
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-timeWindow);
        
        var recentProviderStatuses = await _dbSet
            .Where(r => r.Timestamp >= cutoffTime)
            .GroupBy(r => r.Provider)
            .Select(g => new 
            {
                Provider = g.Key,
                HasIssues = g.Any(r => !r.IsAvailable),
                IssueCount = g.Count(r => !r.IsAvailable),
                TotalChecks = g.Count()
            })
            .Where(x => x.HasIssues && (double)x.IssueCount / x.TotalChecks > 0.3) // More than 30% failures
            .Select(x => x.Provider)
            .ToListAsync();
            
        return recentProviderStatuses;
    }
}
