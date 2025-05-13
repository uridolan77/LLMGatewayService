using LLMGateway.Core.CQRS;
using LLMGateway.Core.Features.TokenUsage.Queries;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.TokenUsage;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Features.TokenUsage.Handlers;

/// <summary>
/// Handler for GetUserTokenUsageQuery
/// </summary>
public class GetUserTokenUsageQueryHandler : IQueryHandler<GetUserTokenUsageQuery, IEnumerable<TokenUsageRecord>>
{
    private readonly ITokenUsageRepository _tokenUsageRepository;
    private readonly ILogger<GetUserTokenUsageQueryHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenUsageRepository">Token usage repository</param>
    /// <param name="logger">Logger</param>
    public GetUserTokenUsageQueryHandler(
        ITokenUsageRepository tokenUsageRepository,
        ILogger<GetUserTokenUsageQueryHandler> logger)
    {
        _tokenUsageRepository = tokenUsageRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handle the query
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage records for the user</returns>
    public async Task<IEnumerable<TokenUsageRecord>> Handle(GetUserTokenUsageQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetUserTokenUsageQuery for user: {UserId}", request.UserId);

        var records = await _tokenUsageRepository.GetForUserAsync(
            request.UserId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return records;
    }
}
