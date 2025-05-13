using LLMGateway.Core.CQRS;
using LLMGateway.Core.Features.TokenUsage.Commands;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Core.Features.TokenUsage.Handlers;

/// <summary>
/// Handler for TrackTokenUsageCommand
/// </summary>
public class TrackTokenUsageCommandHandler : ICommandHandler<TrackTokenUsageCommand, bool>
{
    private readonly ITokenUsageRepository _tokenUsageRepository;
    private readonly TokenUsageOptions _options;
    private readonly ILogger<TrackTokenUsageCommandHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenUsageRepository">Token usage repository</param>
    /// <param name="options">Token usage options</param>
    /// <param name="logger">Logger</param>
    public TrackTokenUsageCommandHandler(
        ITokenUsageRepository tokenUsageRepository,
        IOptions<TokenUsageOptions> options,
        ILogger<TrackTokenUsageCommandHandler> logger)
    {
        _tokenUsageRepository = tokenUsageRepository;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Handle the command
    /// </summary>
    /// <param name="request">Command request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success flag</returns>
    public async Task<bool> Handle(TrackTokenUsageCommand request, CancellationToken cancellationToken)
    {
        if (!_options.EnableTokenCounting)
        {
            _logger.LogDebug("Token usage tracking is disabled");
            return true;
        }

        _logger.LogDebug("Tracking token usage: {TotalTokens} tokens for model {ModelId}",
            request.Record.TotalTokens, request.Record.ModelId);

        try
        {
            await _tokenUsageRepository.AddAsync(request.Record, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track token usage");
            return false;
        }
    }
}
