using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for request log repository
/// </summary>
public interface IRequestLogRepository : IRepository<Entities.RequestLog>
{
    /// <summary>
    /// Get request logs by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Request logs</returns>
    Task<IEnumerable<Entities.RequestLog>> GetByUserIdAsync(string userId, int limit = 100);
      /// <summary>
    /// Get request logs by API key ID
    /// </summary>
    /// <param name="apiKeyId">API key ID</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Request logs</returns>
    Task<IEnumerable<Entities.RequestLog>> GetByApiKeyIdAsync(string apiKeyId, int limit = 100);
    
    /// <summary>
    /// Get request logs by model ID
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Request logs</returns>
    Task<IEnumerable<Entities.RequestLog>> GetByModelIdAsync(string modelId, int limit = 100);
    
    /// <summary>
    /// Get request logs by request type
    /// </summary>
    /// <param name="requestType">Request type</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Request logs</returns>
    Task<IEnumerable<Entities.RequestLog>> GetByRequestTypeAsync(string requestType, int limit = 100);
    
    /// <summary>
    /// Get request logs by status code
    /// </summary>
    /// <param name="statusCode">Status code</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Request logs</returns>
    Task<IEnumerable<Entities.RequestLog>> GetByStatusCodeAsync(int statusCode, int limit = 100);
    
    /// <summary>
    /// Get request logs by time range
    /// </summary>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Request logs</returns>
    Task<IEnumerable<Entities.RequestLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, int limit = 100);
    
    /// <summary>
    /// Get recent request logs
    /// </summary>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Recent request logs</returns>
    Task<IEnumerable<Entities.RequestLog>> GetRecentAsync(int limit = 100);
    
    /// <summary>
    /// Get error logs
    /// </summary>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>Error logs</returns>
    Task<IEnumerable<Entities.RequestLog>> GetErrorLogsAsync(int limit = 100);
    
    /// <summary>
    /// Get request statistics for a time period
    /// </summary>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Request statistics</returns>
    Task<RequestStatistics> GetStatisticsAsync(DateTimeOffset startTime, DateTimeOffset endTime);
}

/// <summary>
/// Request statistics
/// </summary>
public class RequestStatistics
{
    /// <summary>
    /// Total requests
    /// </summary>
    public int TotalRequests { get; set; }
    
    /// <summary>
    /// Success count
    /// </summary>
    public int SuccessCount { get; set; }
    
    /// <summary>
    /// Error count
    /// </summary>
    public int ErrorCount { get; set; }
    
    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessCount / TotalRequests : 0;
    
    /// <summary>
    /// Total request size in bytes
    /// </summary>
    public long TotalRequestSizeBytes { get; set; }
    
    /// <summary>
    /// Total response size in bytes
    /// </summary>
    public long TotalResponseSizeBytes { get; set; }
    
    /// <summary>
    /// Average duration in milliseconds
    /// </summary>
    public double AverageDurationMs { get; set; }
    
    /// <summary>
    /// Request type counts
    /// </summary>
    public Dictionary<string, int> RequestTypeCounts { get; set; } = new();
    
    /// <summary>
    /// Model usage counts
    /// </summary>
    public Dictionary<string, int> ModelUsageCounts { get; set; } = new();
}

/// <summary>
/// Request log repository
/// </summary>
public class RequestLogRepository : Repository<Entities.RequestLog>, IRequestLogRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public RequestLogRepository(LLMGatewayDbContext context) : base(context)
    {
    }
      /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RequestLog>> GetByUserIdAsync(string userId, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RequestLog>> GetByApiKeyIdAsync(string apiKeyId, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.ApiKeyId == apiKeyId)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RequestLog>> GetByModelIdAsync(string modelId, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.ModelId == modelId)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RequestLog>> GetByRequestTypeAsync(string requestType, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.RequestType == requestType)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RequestLog>> GetByStatusCodeAsync(int statusCode, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.StatusCode == statusCode)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RequestLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, int limit = 100)
    {
        return await _dbSet
            .Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RequestLog>> GetRecentAsync(int limit = 100)
    {
        return await _dbSet
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Entities.RequestLog>> GetErrorLogsAsync(int limit = 100)
    {
        return await _dbSet
            .Where(r => r.StatusCode >= 400)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<RequestStatistics> GetStatisticsAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var logs = await _dbSet
            .Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime)
            .ToListAsync();
        
        if (!logs.Any())
        {
            return new RequestStatistics();
        }
        
        var requestTypeCounts = logs
            .GroupBy(r => r.RequestType)
            .ToDictionary(g => g.Key, g => g.Count());
        
        var modelUsageCounts = logs
            .Where(r => !string.IsNullOrEmpty(r.ModelId))
            .GroupBy(r => r.ModelId ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());
        
        return new RequestStatistics
        {
            TotalRequests = logs.Count,
            SuccessCount = logs.Count(r => r.StatusCode < 400),
            ErrorCount = logs.Count(r => r.StatusCode >= 400),
            TotalRequestSizeBytes = logs.Sum(r => r.RequestSizeBytes),
            TotalResponseSizeBytes = logs.Sum(r => r.ResponseSizeBytes),
            AverageDurationMs = logs.Average(r => r.DurationMs),
            RequestTypeCounts = requestTypeCounts,
            ModelUsageCounts = modelUsageCounts
        };
    }
}
