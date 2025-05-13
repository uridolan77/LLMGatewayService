using LLMGateway.Core.CQRS;
using LLMGateway.Core.Features.TokenUsage.Queries;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.TokenUsage;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Features.TokenUsage.Handlers;

/// <summary>
/// Handler for GetTokenUsageSummaryQuery
/// </summary>
public class GetTokenUsageSummaryQueryHandler : IQueryHandler<GetTokenUsageSummaryQuery, TokenUsageSummary>
{
    private readonly ITokenUsageRepository _tokenUsageRepository;
    private readonly ILogger<GetTokenUsageSummaryQueryHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenUsageRepository">Token usage repository</param>
    /// <param name="logger">Logger</param>
    public GetTokenUsageSummaryQueryHandler(
        ITokenUsageRepository tokenUsageRepository,
        ILogger<GetTokenUsageSummaryQueryHandler> logger)
    {
        _tokenUsageRepository = tokenUsageRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handle the query
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage summary</returns>
    public async Task<TokenUsageSummary> Handle(GetTokenUsageSummaryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetTokenUsageSummaryQuery for date range: {StartDate} to {EndDate}",
            request.StartDate, request.EndDate);

        // Get all token usage records in the range
        var records = await _tokenUsageRepository.GetAllAsync(
            r => r.Timestamp >= request.StartDate && r.Timestamp <= request.EndDate,
            cancellationToken: cancellationToken);

        // Count unique users and models
        var uniqueUsers = records.Select(r => r.UserId).Distinct().Count();
        var uniqueModels = records.Select(r => r.ModelId).Distinct().Count();

        // Get top models by usage
        var topModels = records
            .GroupBy(r => r.ModelId)
            .Select(g => new ModelUsage
            {
                ModelId = g.Key,
                TotalTokens = g.Sum(r => r.TotalTokens),
                RequestCount = g.Count()
            })
            .OrderByDescending(m => m.TotalTokens)
            .Take(5)
            .ToList();

        // Get top users by usage
        var topUsers = records
            .GroupBy(r => r.UserId)
            .Select(g => new UserUsage
            {
                UserId = g.Key,
                TotalTokens = g.Sum(r => r.TotalTokens),
                RequestCount = g.Count()
            })
            .OrderByDescending(u => u.TotalTokens)
            .Take(5)
            .ToList();

        // Get usage by provider
        var providerUsage = records
            .GroupBy(r => r.Provider)
            .Select(g => new ProviderUsage
            {
                Provider = g.Key,
                TotalTokens = g.Sum(r => r.TotalTokens),
                RequestCount = g.Count()
            })
            .OrderByDescending(p => p.TotalTokens)
            .ToList();

        // Create and return the summary
        return new TokenUsageSummary
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalTokens = records.Sum(r => r.TotalTokens),
            TotalPromptTokens = records.Sum(r => r.PromptTokens),
            TotalCompletionTokens = records.Sum(r => r.CompletionTokens),
            RequestCount = records.Count(),
            UniqueUsers = uniqueUsers,
            UniqueModels = uniqueModels,
            TopModels = topModels,
            TopUsers = topUsers,
            ProviderUsage = providerUsage
        };
    }
}
