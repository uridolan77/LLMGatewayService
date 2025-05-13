using LLMGateway.Core.CQRS;

namespace LLMGateway.Core.Features.TokenUsage.Queries;

/// <summary>
/// Query to get token usage statistics
/// </summary>
public record GetTokenUsageStatisticsQuery : IQuery<IEnumerable<object>>
{
    /// <summary>
    /// Start date for the query
    /// </summary>
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>
    /// End date for the query
    /// </summary>
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>
    /// How to group the statistics (day, month, model, user)
    /// </summary>
    public string? GroupBy { get; init; }
}
