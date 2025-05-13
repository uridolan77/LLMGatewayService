using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ICompletionService _completionService;
    private readonly ILogger<CompletionsController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="completionService">Completion service</param>
    /// <param name="logger">Logger</param>
    public CompletionsController(
        ICompletionService completionService,
        ILogger<CompletionsController> logger)
    {
        _completionService = completionService;
        _logger = logger;
    }

    /// <summary>
    /// Create a completion
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion response</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CompletionResponse>> CreateCompletionAsync(
        [FromBody] CompletionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating completion for model {ModelId}", request.ModelId);

        // Set the user ID from the claims if not provided
        if (string.IsNullOrEmpty(request.User) && User.Identity?.Name != null)
        {
            request.User = User.Identity.Name;
        }

        // Check if streaming is requested
        if (request.Stream)
        {
            return BadRequest(new { error = "Streaming is not supported for this endpoint. Use the streaming endpoint instead." });
        }

        try
        {
            var response = await _completionService.CreateCompletionAsync(request, cancellationToken);
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task CreateCompletionStreamAsync(
        [FromBody] CompletionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating streaming completion for model {ModelId}", request.ModelId);

        // Set the user ID from the claims if not provided
        if (string.IsNullOrEmpty(request.User) && User.Identity?.Name != null)
        {
            request.User = User.Identity.Name;
        }

        // Ensure streaming is enabled
        request.Stream = true;

        // Set the response content type
        Response.ContentType = "text/event-stream";
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        try
        {
            // Get the streaming response
            var responseStream = _completionService.CreateCompletionStreamAsync(request, cancellationToken);

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
}
