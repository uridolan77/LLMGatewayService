using LLMGateway.Core.CQRS;
using LLMGateway.Core.Models.TokenUsage;

namespace LLMGateway.Core.Features.TokenUsage.Queries;

/// <summary>
/// Query to get token usage summary
/// </summary>
public record GetTokenUsageSummaryQuery : IQuery<TokenUsageSummary>
{
    /// <summary>
    /// Start date for the query
    /// </summary>
    public DateTimeOffset StartDate { get; init; }
    
    /// <summary>
    /// End date for the query
    /// </summary>
    public DateTimeOffset EndDate { get; init; }
}
