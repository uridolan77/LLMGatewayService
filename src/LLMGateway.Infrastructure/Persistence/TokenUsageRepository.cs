using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Persistence;

/// <summary>
/// Repository for token usage records
/// </summary>
public class TokenUsageRepository : ITokenUsageRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<TokenUsageRepository> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger</param>
    public TokenUsageRepository(
        LLMGatewayDbContext dbContext,
        ILogger<TokenUsageRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task AddAsync(TokenUsageRecord record)
    {
        _logger.LogDebug("Adding token usage record for model {ModelId}", record.ModelId);
        
        await _dbContext.TokenUsageRecords.AddAsync(record);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TokenUsageRecord>> GetForUserAsync(string userId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        _logger.LogDebug("Getting token usage records for user {UserId}", userId);
        
        return await _dbContext.TokenUsageRecords
            .Where(r => r.UserId == userId && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TokenUsageRecord>> GetForApiKeyAsync(string apiKeyId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        _logger.LogDebug("Getting token usage records for API key {ApiKeyId}", apiKeyId);
        
        return await _dbContext.TokenUsageRecords
            .Where(r => r.ApiKeyId == apiKeyId && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TokenUsageRecord>> GetForModelAsync(string modelId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        _logger.LogDebug("Getting token usage records for model {ModelId}", modelId);
        
        return await _dbContext.TokenUsageRecords
            .Where(r => r.ModelId == modelId && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TokenUsageRecord>> GetForProviderAsync(string provider, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        _logger.LogDebug("Getting token usage records for provider {Provider}", provider);
        
        return await _dbContext.TokenUsageRecords
            .Where(r => r.Provider == provider && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TokenUsageRecord>> GetAllAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        _logger.LogDebug("Getting all token usage records");
        
        return await _dbContext.TokenUsageRecords
            .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOlderThanAsync(DateTimeOffset date)
    {
        _logger.LogDebug("Deleting token usage records older than {Date}", date);
        
        var result = await _dbContext.TokenUsageRecords
            .Where(r => r.Timestamp < date)
            .ExecuteDeleteAsync();
        
        return result;
    }
}
