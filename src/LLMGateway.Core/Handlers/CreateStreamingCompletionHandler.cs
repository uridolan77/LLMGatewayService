using System.Diagnostics;
using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.Logging;
using LLMGateway.Core.Commands;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;

namespace LLMGateway.Core.Handlers;

/// <summary>
/// Handler for creating streaming completions
/// </summary>
public class CreateStreamingCompletionHandler : IRequestHandler<CreateStreamingCompletionCommand, IAsyncEnumerable<CompletionChunk>>
{
    private readonly ILogger<CreateStreamingCompletionHandler> _logger;
    private readonly ICompletionService _completionService;
    private readonly IContentFilteringService _contentFilteringService;
    private readonly ITokenCountingService _tokenCountingService;
    private readonly IMetricsService _metricsService;

    public CreateStreamingCompletionHandler(
        ILogger<CreateStreamingCompletionHandler> logger,
        ICompletionService completionService,
        IContentFilteringService contentFilteringService,
        ITokenCountingService tokenCountingService,
        IMetricsService metricsService)
    {
        _logger = logger;
        _completionService = completionService;
        _contentFilteringService = contentFilteringService;
        _tokenCountingService = tokenCountingService;
        _metricsService = metricsService;
    }

    public async Task<IAsyncEnumerable<CompletionChunk>> Handle(CreateStreamingCompletionCommand command, CancellationToken cancellationToken)
    {
        using var activity = new Activity("CreateStreamingCompletion").Start();
        activity?.SetTag("model", command.Request.ModelId);
        activity?.SetTag("user", command.UserId);
        activity?.SetTag("request_id", command.RequestId);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["ModelId"] = command.Request.ModelId,
            ["UserId"] = command.UserId ?? "anonymous",
            ["RequestId"] = command.RequestId,
            ["Streaming"] = true
        }))
        {
            _logger.LogInformation("Creating streaming completion for model {ModelId}", command.Request.ModelId);

            // Content filtering for prompt
            await FilterPromptAsync(command.Request);

            // Estimate token usage
            var tokenEstimate = await _tokenCountingService.EstimateTokensAsync(command.Request);
            _logger.LogDebug("Estimated tokens: {PromptTokens} prompt, {CompletionTokens} completion",
                tokenEstimate.PromptTokens, tokenEstimate.EstimatedCompletionTokens);

            // Return the streaming enumerable
            return CreateStreamingCompletionAsync(command, cancellationToken);
        }
    }

    private async IAsyncEnumerable<CompletionChunk> CreateStreamingCompletionAsync(
        CreateStreamingCompletionCommand command,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var chunkCount = 0;
        var totalTokens = 0;
        var success = false;

        // Set streaming flag
        command.Request.Stream = true;

        IAsyncEnumerable<CompletionResponse>? responseStream = null;

        // Get the stream without yielding in try-catch
        try
        {
            responseStream = _completionService.CreateCompletionStreamAsync(command.Request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create streaming completion for request {RequestId}", command.RequestId);

            // Record failure metrics
            var provider = GetProviderFromModel(command.Request.ModelId);
            _metricsService.RecordCompletion(provider, command.Request.ModelId, false, stopwatch.Elapsed.TotalMilliseconds, 0);

            throw;
        }

        // Now yield from the stream
        await foreach (var response in responseStream.WithCancellation(cancellationToken))
        {
            CompletionChunk? chunk = null;

            try
            {
                chunkCount++;

                // Convert CompletionResponse to CompletionChunk
                chunk = new CompletionChunk
                {
                    Id = response.Id,
                    Object = "chat.completion.chunk",
                    Created = response.Created,
                    Model = response.Model,
                    Provider = response.Provider,
                    Choices = response.Choices?.Select(choice => new ChunkChoice
                    {
                        Index = choice.Index,
                        Delta = new DeltaMessage
                        {
                            Role = choice.Message?.Role,
                            Content = choice.Message?.Content
                        },
                        FinishReason = choice.FinishReason
                    }).ToList() ?? new List<ChunkChoice>()
                };

                // Content filtering for each chunk
                if (chunk.Choices?.Any() == true)
                {
                    foreach (var choice in chunk.Choices)
                    {
                        if (!string.IsNullOrEmpty(choice.Delta?.Content))
                        {
                            var filterResult = await _contentFilteringService.FilterCompletionAsync(choice.Delta.Content);
                            if (!filterResult.IsAllowed)
                            {
                                _logger.LogWarning("Streaming completion chunk filtered for request {RequestId}: {Reason}",
                                    command.RequestId, filterResult.Reason);

                                choice.Delta.Content = "[Content filtered]";
                                choice.FinishReason = "content_filter";
                            }
                        }
                    }
                }

                // Track token usage
                if (response.Usage != null)
                {
                    totalTokens = response.Usage.TotalTokens;
                }

                // Check for completion
                if (chunk.Choices?.Any(c => !string.IsNullOrEmpty(c.FinishReason)) == true)
                {
                    success = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing streaming chunk for request {RequestId}", command.RequestId);
                // Continue with next chunk instead of breaking the stream
                continue;
            }

            if (chunk != null)
            {
                yield return chunk;
            }

            if (success)
            {
                break;
            }
        }

        // Record final metrics
        stopwatch.Stop();
        var finalProvider = GetProviderFromModel(command.Request.ModelId);
        _metricsService.RecordCompletion(finalProvider, command.Request.ModelId, success, stopwatch.Elapsed.TotalMilliseconds, totalTokens);

        _logger.LogInformation("Streaming completion completed for request {RequestId} with {ChunkCount} chunks in {Duration}ms",
            command.RequestId, chunkCount, stopwatch.Elapsed.TotalMilliseconds);
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

    private string GetProviderFromModel(string modelId)
    {
        // Simple provider detection based on model ID
        var lowerModelId = modelId.ToLowerInvariant();
        return lowerModelId switch
        {
            var id when id.Contains("gpt") => "OpenAI",
            var id when id.Contains("claude") => "Anthropic",
            var id when id.Contains("command") => "Cohere",
            _ => "Unknown"
        };
    }
}
