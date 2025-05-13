using LLMGateway.Core.CQRS;
using LLMGateway.Core.Features.TokenUsage.Queries;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.TokenUsage;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Features.TokenUsage.Handlers;

/// <summary>
/// Handler for GetTokenUsageStatisticsQuery
/// </summary>
public class GetTokenUsageStatisticsQueryHandler : IQueryHandler<GetTokenUsageStatisticsQuery, IEnumerable<object>>
{
    private readonly ITokenUsageRepository _tokenUsageRepository;
    private readonly ILogger<GetTokenUsageStatisticsQueryHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenUsageRepository">Token usage repository</param>
    /// <param name="logger">Logger</param>
    public GetTokenUsageStatisticsQueryHandler(
        ITokenUsageRepository tokenUsageRepository,
        ILogger<GetTokenUsageStatisticsQueryHandler> logger)
    {
        _tokenUsageRepository = tokenUsageRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handle the query
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage statistics</returns>
    public async Task<IEnumerable<object>> Handle(GetTokenUsageStatisticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetTokenUsageStatisticsQuery with groupBy: {GroupBy}", request.GroupBy);

        // Get the date range
        var startDate = request.StartDate ?? DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTimeOffset.UtcNow;

        // Get all token usage records in the range
        var records = await _tokenUsageRepository.GetAllAsync(
            r => r.Timestamp >= startDate && r.Timestamp <= endDate,
            cancellationToken: cancellationToken);

        // Group by requested dimension
        return request.GroupBy?.ToLower() switch
        {
            "day" => GroupByDay(records, startDate, endDate),
            "month" => GroupByMonth(records, startDate, endDate),
            "model" => GroupByModel(records),
            "user" => GroupByUser(records),
            "provider" => GroupByProvider(records),
            _ => GroupByDay(records, startDate, endDate) // Default to grouping by day
        };
    }

    private static IEnumerable<object> GroupByDay(IEnumerable<TokenUsageRecord> records,
        DateTimeOffset startDate, DateTimeOffset endDate)
    {
        // Create a dictionary with all days in the range, initialized to zero counts
        var dayGroups = new Dictionary<DateTimeOffset, int>();
        for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
        {
            dayGroups[day] = 0;
        }

        // Aggregate token counts by day
        foreach (var record in records)
        {
            var day = record.Timestamp.Date;
            if (dayGroups.ContainsKey(day))
            {
                dayGroups[day] += record.TotalTokens;
            }
        }

        // Convert to result format
        return dayGroups.Select(kv => new
        {
            Date = kv.Key.ToString("yyyy-MM-dd"),
            Tokens = kv.Value
        });
    }

    private static IEnumerable<object> GroupByMonth(IEnumerable<TokenUsageRecord> records,
        DateTimeOffset startDate, DateTimeOffset endDate)
    {
        // Create a dictionary for all months in the range
        var monthGroups = new Dictionary<string, int>();
        for (var month = new DateTime(startDate.Year, startDate.Month, 1);
             month <= new DateTime(endDate.Year, endDate.Month, 1);
             month = month.AddMonths(1))
        {
            monthGroups[month.ToString("yyyy-MM")] = 0;
        }

        // Aggregate token counts by month
        foreach (var record in records)
        {
            var month = record.Timestamp.ToString("yyyy-MM");
            if (monthGroups.ContainsKey(month))
            {
                monthGroups[month] += record.TotalTokens;
            }
        }

        // Convert to result format
        return monthGroups.Select(kv => new
        {
            Month = kv.Key,
            Tokens = kv.Value
        });
    }

    private static IEnumerable<object> GroupByModel(IEnumerable<TokenUsageRecord> records)
    {
        return records
            .GroupBy(r => r.ModelId)
            .Select(g => new
            {
                Model = g.Key,
                Tokens = g.Sum(r => r.TotalTokens),
                Requests = g.Count(),
                PromptTokens = g.Sum(r => r.PromptTokens),
                CompletionTokens = g.Sum(r => r.CompletionTokens)
            });
    }

    private static IEnumerable<object> GroupByUser(IEnumerable<TokenUsageRecord> records)
    {
        return records
            .GroupBy(r => r.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Tokens = g.Sum(r => r.TotalTokens),
                Requests = g.Count(),
                PromptTokens = g.Sum(r => r.PromptTokens),
                CompletionTokens = g.Sum(r => r.CompletionTokens)
            });
    }

    private static IEnumerable<object> GroupByProvider(IEnumerable<TokenUsageRecord> records)
    {
        return records
            .GroupBy(r => r.Provider)
            .Select(g => new
            {
                Provider = g.Key,
                Tokens = g.Sum(r => r.TotalTokens),
                Requests = g.Count(),
                PromptTokens = g.Sum(r => r.PromptTokens),
                CompletionTokens = g.Sum(r => r.CompletionTokens)
            });
    }
}
