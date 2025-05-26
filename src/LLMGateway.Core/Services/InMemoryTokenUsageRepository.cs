using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.TokenUsage;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LLMGateway.Core.Services;

/// <summary>
/// In-memory implementation of ITokenUsageRepository for when database is not available
/// </summary>
public class InMemoryTokenUsageRepository : ITokenUsageRepository
{
    private readonly ILogger<InMemoryTokenUsageRepository> _logger;
    private readonly ConcurrentBag<TokenUsageRecord> _records = new();

    public InMemoryTokenUsageRepository(ILogger<InMemoryTokenUsageRepository> logger)
    {
        _logger = logger;
    }

    public Task AddAsync(TokenUsageRecord record, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding token usage record for model {ModelId}", record.ModelId);
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<TokenUsageRecord>> GetForUserAsync(string userId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        var query = _records.Where(r => r.UserId == userId && r.Timestamp >= startDate && r.Timestamp <= endDate);
        return Task.FromResult(query.AsEnumerable());
    }

    public Task<IEnumerable<TokenUsageRecord>> GetForApiKeyAsync(string apiKeyId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        var query = _records.Where(r => r.ApiKeyId == apiKeyId && r.Timestamp >= startDate && r.Timestamp <= endDate);
        return Task.FromResult(query.AsEnumerable());
    }

    public Task<IEnumerable<TokenUsageRecord>> GetForModelAsync(string modelId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        var query = _records.Where(r => r.ModelId == modelId && r.Timestamp >= startDate && r.Timestamp <= endDate);
        return Task.FromResult(query.AsEnumerable());
    }

    public Task<IEnumerable<TokenUsageRecord>> GetForProviderAsync(string provider, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        var query = _records.Where(r => r.Provider == provider && r.Timestamp >= startDate && r.Timestamp <= endDate);
        return Task.FromResult(query.AsEnumerable());
    }

    public Task<IEnumerable<TokenUsageRecord>> GetAllAsync(Func<TokenUsageRecord, bool>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = predicate != null ? _records.Where(predicate) : _records;
        return Task.FromResult(query.AsEnumerable());
    }

    public Task<IEnumerable<TokenUsageRecord>> GetTotalUsageAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        var query = _records.Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate);
        return Task.FromResult(query.AsEnumerable());
    }


}
