using LLMGateway.Core.CQRS;
using LLMGateway.Core.Features.TokenUsage.Queries;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.TokenUsage;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Features.TokenUsage.Handlers;

/// <summary>
/// Handler for GetTokenUsageForModelQuery
/// </summary>
public class GetTokenUsageForModelQueryHandler : IQueryHandler<GetTokenUsageForModelQuery, IEnumerable<TokenUsageRecord>>
{
    private readonly ITokenUsageRepository _tokenUsageRepository;
    private readonly ILogger<GetTokenUsageForModelQueryHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenUsageRepository">Token usage repository</param>
    /// <param name="logger">Logger</param>
    public GetTokenUsageForModelQueryHandler(
        ITokenUsageRepository tokenUsageRepository,
        ILogger<GetTokenUsageForModelQueryHandler> logger)
    {
        _tokenUsageRepository = tokenUsageRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handle the query
    /// </summary>
    /// <param name="request">Query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage records for the model</returns>
    public async Task<IEnumerable<TokenUsageRecord>> Handle(GetTokenUsageForModelQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetTokenUsageForModelQuery for model: {ModelId}", request.ModelId);

        var records = await _tokenUsageRepository.GetForModelAsync(
            request.ModelId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return records;
    }
}
