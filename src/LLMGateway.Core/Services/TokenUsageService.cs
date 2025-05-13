using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.TokenUsage;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for tracking token usage
/// </summary>
public class TokenUsageService : ITokenUsageService
{
    private readonly ILogger<TokenUsageService> _logger;
    private readonly TokenUsageOptions _options;
    private ConcurrentBag<TokenUsageRecord> _inMemoryRecords = new();

    /// <summary>
    /// Constructor
    /// </summary>
    public TokenUsageService(
        IOptions<TokenUsageOptions> options,
        ILogger<TokenUsageService> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public Task TrackUsageAsync(TokenUsageRecord record)
    {
        if (!_options.EnableTokenCounting)
        {
            return Task.CompletedTask;
        }

        _logger.LogDebug("Tracking token usage: {Tokens} tokens for model {ModelId}", record.TotalTokens, record.ModelId);

        // For in-memory storage, just add to the concurrent bag
        if (_options.StorageProvider == "InMemory")
        {
            _inMemoryRecords.Add(record);
            
            // Clean up old records
            CleanupOldRecords();
        }
        else
        {
            // For database storage, this would be implemented in the infrastructure layer
            _logger.LogDebug("Token usage tracking with provider {Provider} is handled by the infrastructure layer", _options.StorageProvider);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TokenUsageRecord>> GetUsageForUserAsync(string userId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        if (_options.StorageProvider == "InMemory")
        {
            var records = _inMemoryRecords
                .Where(r => r.UserId == userId && r.Timestamp >= startDate && r.Timestamp <= endDate)
                .ToList();
            
            return Task.FromResult<IEnumerable<TokenUsageRecord>>(records);
        }
        
        // For database storage, this would be implemented in the infrastructure layer
        _logger.LogDebug("Token usage retrieval with provider {Provider} is handled by the infrastructure layer", _options.StorageProvider);
        return Task.FromResult<IEnumerable<TokenUsageRecord>>(new List<TokenUsageRecord>());
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TokenUsageRecord>> GetUsageForApiKeyAsync(string apiKeyId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        if (_options.StorageProvider == "InMemory")
        {
            var records = _inMemoryRecords
                .Where(r => r.ApiKeyId == apiKeyId && r.Timestamp >= startDate && r.Timestamp <= endDate)
                .ToList();
            
            return Task.FromResult<IEnumerable<TokenUsageRecord>>(records);
        }
        
        // For database storage, this would be implemented in the infrastructure layer
        _logger.LogDebug("Token usage retrieval with provider {Provider} is handled by the infrastructure layer", _options.StorageProvider);
        return Task.FromResult<IEnumerable<TokenUsageRecord>>(new List<TokenUsageRecord>());
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TokenUsageRecord>> GetUsageForModelAsync(string modelId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        if (_options.StorageProvider == "InMemory")
        {
            var records = _inMemoryRecords
                .Where(r => r.ModelId == modelId && r.Timestamp >= startDate && r.Timestamp <= endDate)
                .ToList();
            
            return Task.FromResult<IEnumerable<TokenUsageRecord>>(records);
        }
        
        // For database storage, this would be implemented in the infrastructure layer
        _logger.LogDebug("Token usage retrieval with provider {Provider} is handled by the infrastructure layer", _options.StorageProvider);
        return Task.FromResult<IEnumerable<TokenUsageRecord>>(new List<TokenUsageRecord>());
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TokenUsageRecord>> GetUsageForProviderAsync(string provider, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        if (_options.StorageProvider == "InMemory")
        {
            var records = _inMemoryRecords
                .Where(r => r.Provider == provider && r.Timestamp >= startDate && r.Timestamp <= endDate)
                .ToList();
            
            return Task.FromResult<IEnumerable<TokenUsageRecord>>(records);
        }
        
        // For database storage, this would be implemented in the infrastructure layer
        _logger.LogDebug("Token usage retrieval with provider {Provider} is handled by the infrastructure layer", _options.StorageProvider);
        return Task.FromResult<IEnumerable<TokenUsageRecord>>(new List<TokenUsageRecord>());
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TokenUsageRecord>> GetTotalUsageAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        if (_options.StorageProvider == "InMemory")
        {
            var records = _inMemoryRecords
                .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
                .ToList();
            
            return Task.FromResult<IEnumerable<TokenUsageRecord>>(records);
        }
        
        // For database storage, this would be implemented in the infrastructure layer
        _logger.LogDebug("Token usage retrieval with provider {Provider} is handled by the infrastructure layer", _options.StorageProvider);
        return Task.FromResult<IEnumerable<TokenUsageRecord>>(new List<TokenUsageRecord>());
    }

    /// <inheritdoc/>
    public async Task<TokenUsageSummary> GetUsageSummaryAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var records = await GetTotalUsageAsync(startDate, endDate);
        
        var summary = new TokenUsageSummary
        {
            TotalPromptTokens = records.Sum(r => r.PromptTokens),
            TotalCompletionTokens = records.Sum(r => r.CompletionTokens),
            TotalTokens = records.Sum(r => r.TotalTokens),
            TotalEstimatedCostUsd = records.Sum(r => r.EstimatedCostUsd)
        };
        
        // Group by model
        var modelGroups = records.GroupBy(r => r.ModelId);
        foreach (var group in modelGroups)
        {
            var modelId = group.Key;
            var modelRecords = group.ToList();
            var provider = modelRecords.FirstOrDefault()?.Provider ?? "unknown";
            
            summary.UsageByModel[modelId] = new ModelUsage
            {
                ModelId = modelId,
                Provider = provider,
                PromptTokens = modelRecords.Sum(r => r.PromptTokens),
                CompletionTokens = modelRecords.Sum(r => r.CompletionTokens),
                TotalTokens = modelRecords.Sum(r => r.TotalTokens),
                EstimatedCostUsd = modelRecords.Sum(r => r.EstimatedCostUsd)
            };
        }
        
        // Group by provider
        var providerGroups = records.GroupBy(r => r.Provider);
        foreach (var group in providerGroups)
        {
            var provider = group.Key;
            var providerRecords = group.ToList();
            
            summary.UsageByProvider[provider] = new ProviderUsage
            {
                Provider = provider,
                PromptTokens = providerRecords.Sum(r => r.PromptTokens),
                CompletionTokens = providerRecords.Sum(r => r.CompletionTokens),
                TotalTokens = providerRecords.Sum(r => r.TotalTokens),
                EstimatedCostUsd = providerRecords.Sum(r => r.EstimatedCostUsd)
            };
        }
        
        // Group by user
        var userGroups = records.GroupBy(r => r.UserId);
        foreach (var group in userGroups)
        {
            var userId = group.Key;
            var userRecords = group.ToList();
            
            summary.UsageByUser[userId] = new UserUsage
            {
                UserId = userId,
                PromptTokens = userRecords.Sum(r => r.PromptTokens),
                CompletionTokens = userRecords.Sum(r => r.CompletionTokens),
                TotalTokens = userRecords.Sum(r => r.TotalTokens),
                EstimatedCostUsd = userRecords.Sum(r => r.EstimatedCostUsd)
            };
        }
        
        return summary;
    }

    /// <inheritdoc/>
    public Task TrackCompletionTokenUsageAsync(Models.Completion.CompletionRequest request, Models.Completion.CompletionResponse response)
    {
        try
        {
            var record = new TokenUsageRecord
            {
                RequestId = response.Id,
                ModelId = response.Model,
                Provider = response.Provider,
                RequestType = "completion",
                PromptTokens = response.Usage.PromptTokens,
                CompletionTokens = response.Usage.CompletionTokens,
                TotalTokens = response.Usage.TotalTokens,
                UserId = request.User ?? "anonymous",
                ApiKeyId = "unknown" // This would be set by middleware
            };

            // Calculate estimated cost if token prices are available
            // This would be based on the model's token prices

            return TrackUsageAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track token usage for completion");
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<object>> GetTokenUsageStatisticsAsync(DateTimeOffset? startDate, DateTimeOffset? endDate, string? groupBy)
    {
        var effectiveStartDate = startDate ?? DateTimeOffset.UtcNow.AddDays(-30);
        var effectiveEndDate = endDate ?? DateTimeOffset.UtcNow;
        
        if (_options.StorageProvider == "InMemory")
        {
            var records = _inMemoryRecords
                .Where(r => r.Timestamp >= effectiveStartDate && r.Timestamp <= effectiveEndDate)
                .ToList();
            
            // Group by selected criteria
            if (string.Equals(groupBy, "day", StringComparison.OrdinalIgnoreCase))
            {
                var grouped = records
                    .GroupBy(r => r.Timestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        PromptTokens = g.Sum(r => r.PromptTokens),
                        CompletionTokens = g.Sum(r => r.CompletionTokens),
                        TotalTokens = g.Sum(r => r.TotalTokens),
                        Cost = g.Sum(r => r.EstimatedCostUsd)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();
                
                return Task.FromResult<IEnumerable<object>>(grouped);
            }
            else if (string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase))
            {
                var grouped = records
                    .GroupBy(r => new { r.Timestamp.Year, r.Timestamp.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        PromptTokens = g.Sum(r => r.PromptTokens),
                        CompletionTokens = g.Sum(r => r.CompletionTokens),
                        TotalTokens = g.Sum(r => r.TotalTokens),
                        Cost = g.Sum(r => r.EstimatedCostUsd)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();
                
                return Task.FromResult<IEnumerable<object>>(grouped);
            }
            else if (string.Equals(groupBy, "model", StringComparison.OrdinalIgnoreCase))
            {
                var grouped = records
                    .GroupBy(r => r.ModelId)
                    .Select(g => new
                    {
                        ModelId = g.Key,
                        PromptTokens = g.Sum(r => r.PromptTokens),
                        CompletionTokens = g.Sum(r => r.CompletionTokens),
                        TotalTokens = g.Sum(r => r.TotalTokens),
                        Cost = g.Sum(r => r.EstimatedCostUsd)
                    })
                    .OrderByDescending(x => x.TotalTokens)
                    .ToList();
                
                return Task.FromResult<IEnumerable<object>>(grouped);
            }
            else if (string.Equals(groupBy, "user", StringComparison.OrdinalIgnoreCase))
            {
                var grouped = records
                    .GroupBy(r => r.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        PromptTokens = g.Sum(r => r.PromptTokens),
                        CompletionTokens = g.Sum(r => r.CompletionTokens),
                        TotalTokens = g.Sum(r => r.TotalTokens),
                        Cost = g.Sum(r => r.EstimatedCostUsd)
                    })
                    .OrderByDescending(x => x.TotalTokens)
                    .ToList();
                
                return Task.FromResult<IEnumerable<object>>(grouped);
            }
            else
            {
                // Default to total summary
                var summary = new
                {
                    PromptTokens = records.Sum(r => r.PromptTokens),
                    CompletionTokens = records.Sum(r => r.CompletionTokens),
                    TotalTokens = records.Sum(r => r.TotalTokens),
                    Cost = records.Sum(r => r.EstimatedCostUsd),
                    RequestCount = records.Count()
                };
                
                return Task.FromResult<IEnumerable<object>>(new[] { summary });
            }
        }
        
        // For database storage, this would be implemented in the infrastructure layer
        _logger.LogDebug("Token usage statistics retrieval with provider {Provider} is handled by the infrastructure layer", _options.StorageProvider);
        return Task.FromResult<IEnumerable<object>>(new List<object>());
    }

    private void CleanupOldRecords()
    {
        if (_options.StorageProvider != "InMemory")
        {
            return;
        }
        
        var cutoffDate = DateTimeOffset.UtcNow - _options.DataRetentionPeriod;
        var newRecords = new ConcurrentBag<TokenUsageRecord>();
        
        foreach (var record in _inMemoryRecords)
        {
            if (record.Timestamp >= cutoffDate)
            {
                newRecords.Add(record);
            }
        }
        
        // This is a simplistic approach - in a real implementation, we would use a more efficient
        // data structure or a background job to clean up old records
        Interlocked.Exchange(ref _inMemoryRecords, newRecords);
    }
}
