using LLMGateway.Core.CQRS;
using LLMGateway.Core.Models.TokenUsage;

namespace LLMGateway.Core.Features.TokenUsage.Commands;

/// <summary>
/// Command to track token usage
/// </summary>
public record TrackTokenUsageCommand : ICommand<bool>
{
    /// <summary>
    /// Token usage record to track
    /// </summary>
    public TokenUsageRecord Record { get; init; } = new();
}
