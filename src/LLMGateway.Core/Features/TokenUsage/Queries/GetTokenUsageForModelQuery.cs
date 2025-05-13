using LLMGateway.Core.CQRS;
using LLMGateway.Core.Models.TokenUsage;

namespace LLMGateway.Core.Features.TokenUsage.Queries;

/// <summary>
/// Query to get token usage for a model
/// </summary>
public record GetTokenUsageForModelQuery : IQuery<IEnumerable<TokenUsageRecord>>
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; init; } = string.Empty;
    
    /// <summary>
    /// Start date for the query
    /// </summary>
    public DateTimeOffset StartDate { get; init; }
    
    /// <summary>
    /// End date for the query
    /// </summary>
    public DateTimeOffset EndDate { get; init; }
}
