using LLMGateway.Core.CQRS;
using LLMGateway.Core.Models.TokenUsage;

namespace LLMGateway.Core.Features.TokenUsage.Queries;

/// <summary>
/// Query to get token usage for a user
/// </summary>
public record GetUserTokenUsageQuery : IQuery<IEnumerable<TokenUsageRecord>>
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;
    
    /// <summary>
    /// Start date for the query
    /// </summary>
    public DateTimeOffset StartDate { get; init; }
    
    /// <summary>
    /// End date for the query
    /// </summary>
    public DateTimeOffset EndDate { get; init; }
}
