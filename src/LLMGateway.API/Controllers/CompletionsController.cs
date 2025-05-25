using LLMGateway.Core.Commands;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Runtime.CompilerServices;
using System.Text;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for completions
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = "CompletionAccess")]
public class CompletionsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ILogger<CompletionsController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator">MediatR mediator</param>
    /// <param name="logger">Logger</param>
    public CompletionsController(
        IMediator mediator,
        ILogger<CompletionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a completion
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion response</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a completion",
        Description = "Generates text completion using the specified model")]
    [SwaggerResponse(200, "Completion generated successfully", typeof(CompletionResponse))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(401, "Authentication required")]
    [SwaggerResponse(403, "Access forbidden")]
    [SwaggerResponse(404, "Model not found")]
    [SwaggerResponse(429, "Rate limit exceeded")]
    [SwaggerResponse(500, "Internal server error")]
    [SwaggerResponse(502, "Provider unavailable")]
    [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CompletionResponse>> CreateCompletionAsync(
        [FromBody] CompletionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating completion for model {ModelId}", request.ModelId);

        // Check if streaming is requested
        if (request.Stream)
        {
            return BadRequest(new { error = "Streaming is not supported for this endpoint. Use the streaming endpoint instead." });
        }

        try
        {
            var apiKey = GetApiKeyFromHeaders();
            var userId = User.Identity?.Name;

            var command = new CreateCompletionCommand(request, userId, apiKey);
            var response = await _mediator.Send(command, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create completion for model {ModelId}", request.ModelId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a streaming completion
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of completion responses</returns>
    [HttpPost("stream")]
    [SwaggerOperation(
        Summary = "Create a streaming completion",
        Description = "Generates text completion using the specified model with real-time streaming")]
    [SwaggerResponse(200, "Streaming completion started", typeof(string))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(401, "Authentication required")]
    [SwaggerResponse(403, "Access forbidden")]
    [SwaggerResponse(404, "Model not found")]
    [SwaggerResponse(429, "Rate limit exceeded")]
    [SwaggerResponse(500, "Internal server error")]
    [SwaggerResponse(502, "Provider unavailable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task CreateCompletionStreamAsync(
        [FromBody] CompletionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating streaming completion for model {ModelId}", request.ModelId);

        // Set the response content type
        Response.ContentType = "text/event-stream";
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        try
        {
            var apiKey = GetApiKeyFromHeaders();
            var userId = User.Identity?.Name;

            var command = new CreateStreamingCompletionCommand(request, userId, apiKey, cancellationToken);
            var responseStream = await _mediator.Send(command, cancellationToken);

            // Write each chunk to the response
            await foreach (var chunk in responseStream.WithCancellation(cancellationToken))
            {
                // Serialize the chunk to JSON
                var json = System.Text.Json.JsonSerializer.Serialize(chunk);

                // Write the chunk as an SSE event
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            // Write the [DONE] event
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create streaming completion for model {ModelId}", request.ModelId);

            // Write the error as an SSE event
            var errorJson = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Create a batch of completions
    /// </summary>
    /// <param name="request">Batch completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch completion response</returns>
    [HttpPost("batch")]
    [SwaggerOperation(
        Summary = "Create batch completions",
        Description = "Generates multiple text completions in parallel")]
    [SwaggerResponse(200, "Batch completions generated successfully", typeof(BatchCompletionResponse))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(401, "Authentication required")]
    [SwaggerResponse(403, "Access forbidden")]
    [SwaggerResponse(429, "Rate limit exceeded")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(BatchCompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BatchCompletionResponse>> CreateBatchCompletionAsync(
        [FromBody] BatchCompletionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating batch completion with {BatchSize} requests", request.Requests.Count);

        if (request.Requests.Count == 0)
        {
            return BadRequest(new { error = "Batch request must contain at least one completion request." });
        }

        if (request.Requests.Count > 100) // Configurable limit
        {
            return BadRequest(new { error = "Batch size cannot exceed 100 requests." });
        }

        try
        {
            var apiKey = GetApiKeyFromHeaders();
            var userId = User.Identity?.Name;

            var command = new CreateBatchCompletionCommand(request, userId, apiKey);
            var response = await _mediator.Send(command, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create batch completion");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    private string? GetApiKeyFromHeaders()
    {
        return Request.Headers.TryGetValue("X-API-Key", out var apiKey) ? apiKey.FirstOrDefault() : null;
    }
}
