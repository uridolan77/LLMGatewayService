using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LLMGateway.Core.Commands;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;

namespace LLMGateway.Core.Handlers;

/// <summary>
/// Handler for creating batch completions
/// </summary>
public class CreateBatchCompletionHandler : IRequestHandler<CreateBatchCompletionCommand, BatchCompletionResponse>
{
    private readonly ILogger<CreateBatchCompletionHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IMetricsService _metricsService;

    public CreateBatchCompletionHandler(
        ILogger<CreateBatchCompletionHandler> logger,
        IMediator mediator,
        IMetricsService metricsService)
    {
        _logger = logger;
        _mediator = mediator;
        _metricsService = metricsService;
    }

    public async Task<BatchCompletionResponse> Handle(CreateBatchCompletionCommand command, CancellationToken cancellationToken)
    {
        using var activity = new Activity("CreateBatchCompletion").Start();
        activity?.SetTag("batch_size", command.Request.Requests.Count);
        activity?.SetTag("max_concurrency", command.Request.MaxConcurrency);
        activity?.SetTag("user", command.UserId);
        activity?.SetTag("request_id", command.RequestId);

        var stopwatch = Stopwatch.StartNew();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["BatchSize"] = command.Request.Requests.Count,
            ["MaxConcurrency"] = command.Request.MaxConcurrency,
            ["UserId"] = command.UserId ?? "anonymous",
            ["RequestId"] = command.RequestId
        }))
        {
            _logger.LogInformation("Creating batch completion with {BatchSize} requests", command.Request.Requests.Count);

            var response = new BatchCompletionResponse();
            var semaphore = new SemaphoreSlim(command.Request.MaxConcurrency, command.Request.MaxConcurrency);

            try
            {
                var tasks = command.Request.Requests.Select(async (request, index) =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var completionCommand = new CreateCompletionCommand(request, command.UserId, command.ApiKey);
                        var completionResponse = await _mediator.Send(completionCommand, cancellationToken);

                        _logger.LogDebug("Batch item {Index} completed successfully", index);
                        return new { Index = index, Response = completionResponse, Error = (string?)null };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Batch item {Index} failed: {Error}", index, ex.Message);

                        if (command.Request.FailFast)
                        {
                            throw;
                        }

                        return new {
                            Index = index,
                            Response = new CompletionResponse { Error = ex.Message },
                            Error = ex.Message
                        };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var results = await Task.WhenAll(tasks);

                // Sort results by original index to maintain order
                var sortedResults = results.OrderBy(r => r.Index).ToArray();

                response.Responses = sortedResults.Select(r => r.Response).ToList();
                response.SuccessCount = sortedResults.Count(r => r.Error == null);
                response.FailureCount = sortedResults.Count(r => r.Error != null);
                response.Errors = sortedResults.Where(r => r.Error != null).Select(r => r.Error!).ToList();

                stopwatch.Stop();
                response.TotalDurationMs = stopwatch.Elapsed.TotalMilliseconds;

                _logger.LogInformation("Batch completion completed: {SuccessCount} successful, {FailureCount} failed in {Duration}ms",
                    response.SuccessCount, response.FailureCount, response.TotalDurationMs);

                // Record batch metrics
                _metricsService.RecordCustomMetric("batch_completion_total", 1, new Dictionary<string, object>
                {
                    ["batch_size"] = command.Request.Requests.Count,
                    ["success_count"] = response.SuccessCount,
                    ["failure_count"] = response.FailureCount,
                    ["duration_ms"] = response.TotalDurationMs
                });

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Batch completion failed for request {RequestId}", command.RequestId);

                // Record failure metrics
                _metricsService.RecordCustomMetric("batch_completion_total", 1, new Dictionary<string, object>
                {
                    ["batch_size"] = command.Request.Requests.Count,
                    ["success_count"] = 0,
                    ["failure_count"] = command.Request.Requests.Count,
                    ["duration_ms"] = stopwatch.Elapsed.TotalMilliseconds,
                    ["error"] = "batch_failed"
                });

                throw;
            }
            finally
            {
                semaphore.Dispose();
            }
        }
    }
}
