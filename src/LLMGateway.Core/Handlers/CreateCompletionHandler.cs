using System.Diagnostics;
using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.Logging;
using LLMGateway.Core.Commands;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Constants;

namespace LLMGateway.Core.Handlers;

/// <summary>
/// Handler for creating completions
/// </summary>
public class CreateCompletionHandler : IRequestHandler<CreateCompletionCommand, CompletionResponse>
{
    private readonly ILogger<CreateCompletionHandler> _logger;
    private readonly ICompletionService _completionService;
    private readonly IContentFilteringService _contentFilteringService;
    private readonly ITokenCountingService _tokenCountingService;
    private readonly IMetricsService _metricsService;
    private readonly IEnhancedCacheService _cacheService;

    public CreateCompletionHandler(
        ILogger<CreateCompletionHandler> logger,
        ICompletionService completionService,
        IContentFilteringService contentFilteringService,
        ITokenCountingService tokenCountingService,
        IMetricsService metricsService,
        IEnhancedCacheService cacheService)
    {
        _logger = logger;
        _completionService = completionService;
        _contentFilteringService = contentFilteringService;
        _tokenCountingService = tokenCountingService;
        _metricsService = metricsService;
        _cacheService = cacheService;
    }

    public async Task<CompletionResponse> Handle(CreateCompletionCommand command, CancellationToken cancellationToken)
    {
        using var activity = new Activity("CreateCompletion").Start();
        activity?.SetTag("model", command.Request.ModelId);
        activity?.SetTag("user", command.UserId);
        activity?.SetTag("request_id", command.RequestId);

        var stopwatch = Stopwatch.StartNew();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["ModelId"] = command.Request.ModelId,
            ["UserId"] = command.UserId ?? "anonymous",
            ["RequestId"] = command.RequestId,
            ["ApiKey"] = HashApiKey(command.ApiKey)
        }))
        {
            try
            {
                _logger.LogInformation("Creating completion for model {ModelId}", command.Request.ModelId);

                // 1. Content filtering for prompt
                await FilterPromptAsync(command.Request);

                // 2. Check cache first
                var cacheKey = GenerateCacheKey(command.Request);
                var cachedResponse = await _cacheService.GetAsync<CompletionResponse>(cacheKey);
                if (cachedResponse != null)
                {
                    _logger.LogInformation("Returning cached completion for request {RequestId}", command.RequestId);
                    _metricsService.RecordCacheAccess("completion", true);
                    return cachedResponse;
                }

                _metricsService.RecordCacheAccess("completion", false);

                // 3. Estimate token usage
                var tokenEstimate = await _tokenCountingService.EstimateTokensAsync(command.Request);
                _logger.LogDebug("Estimated tokens: {PromptTokens} prompt, {CompletionTokens} completion",
                    tokenEstimate.PromptTokens, tokenEstimate.EstimatedCompletionTokens);

                // 4. Create completion
                var response = await _completionService.CreateCompletionAsync(command.Request, cancellationToken);

                // 5. Content filtering for completion
                if (response.Choices?.Any() == true)
                {
                    foreach (var choice in response.Choices)
                    {
                        if (!string.IsNullOrEmpty(choice.Message?.Content))
                        {
                            var filterResult = await _contentFilteringService.FilterCompletionAsync(choice.Message.Content);
                            if (!filterResult.IsAllowed)
                            {
                                _logger.LogWarning("Completion filtered for request {RequestId}: {Reason}",
                                    command.RequestId, filterResult.Reason);

                                choice.Message.Content = "[Content filtered]";
                                choice.FinishReason = "content_filter";
                            }
                        }
                    }
                }

                // 6. Cache the response if requested
                if (ShouldCacheResponse(command.Request))
                {
                    var cacheExpiration = TimeSpan.FromMinutes(GetCacheDurationMinutes(command.Request));
                    await _cacheService.SetWithSlidingExpirationAsync(cacheKey, response, cacheExpiration);
                    _logger.LogDebug("Cached completion response for {Duration} minutes", cacheExpiration.TotalMinutes);
                }

                // 7. Record metrics
                stopwatch.Stop();
                var provider = GetProviderFromModel(command.Request.ModelId);
                _metricsService.RecordCompletion(
                    provider,
                    command.Request.ModelId,
                    true,
                    stopwatch.Elapsed.TotalMilliseconds,
                    response.Usage?.TotalTokens ?? 0);

                _logger.LogInformation("Completion created successfully for request {RequestId} in {Duration}ms",
                    command.RequestId, stopwatch.Elapsed.TotalMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var provider = GetProviderFromModel(command.Request.ModelId);
                _metricsService.RecordCompletion(provider, command.Request.ModelId, false, stopwatch.Elapsed.TotalMilliseconds);

                _logger.LogError(ex, "Failed to create completion for request {RequestId}", command.RequestId);
                throw;
            }
        }
    }

    private async Task FilterPromptAsync(CompletionRequest request)
    {
        if (request.Messages?.Any() == true)
        {
            foreach (var message in request.Messages)
            {
                if (!string.IsNullOrEmpty(message.Content))
                {
                    var filterResult = await _contentFilteringService.FilterPromptAsync(message.Content);
                    if (!filterResult.IsAllowed)
                    {
                        throw new InvalidOperationException($"Prompt content filtered: {filterResult.Reason}");
                    }
                }
            }
        }
    }

    private string GenerateCacheKey(CompletionRequest request)
    {
        // Generate a deterministic cache key based on request parameters
        var keyData = new
        {
            request.ModelId,
            Messages = request.Messages?.Select(m => new { m.Role, m.Content }).ToArray(),
            request.Temperature,
            request.MaxTokens,
            request.TopP,
            request.FrequencyPenalty,
            request.PresencePenalty,
            request.Stop
        };

        var json = System.Text.Json.JsonSerializer.Serialize(keyData);
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));
        return $"{LLMGatewayConstants.CacheKeys.CompletionPrefix}{Convert.ToHexString(hash)[..16]}";
    }

    private bool ShouldCacheResponse(CompletionRequest request)
    {
        // Don't cache streaming responses or responses with high temperature (non-deterministic)
        return !request.Stream && (request.Temperature ?? 0.7) <= 0.3;
    }

    private int GetCacheDurationMinutes(CompletionRequest request)
    {
        // Cache duration based on temperature - lower temperature = longer cache
        var temperature = request.Temperature ?? 0.7;
        return temperature switch
        {
            <= 0.1 => 60,  // Very deterministic - cache for 1 hour
            <= 0.3 => 30,  // Somewhat deterministic - cache for 30 minutes
            _ => 5         // Default - cache for 5 minutes
        };
    }

    private string GetProviderFromModel(string modelId)
    {
        // Simple provider detection based on model ID
        var lowerModelId = modelId.ToLowerInvariant();
        return lowerModelId switch
        {
            var id when id.Contains("gpt") => LLMGatewayConstants.Providers.OpenAI,
            var id when id.Contains("claude") => LLMGatewayConstants.Providers.Anthropic,
            var id when id.Contains("command") => LLMGatewayConstants.Providers.Cohere,
            _ => "Unknown"
        };
    }

    private string HashApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return "anonymous";

        return apiKey.Length > 8 ? $"{apiKey[..4]}****{apiKey[^4..]}" : "****";
    }
}
