using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Embedding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for embeddings
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = "EmbeddingAccess")]
public class EmbeddingsController : BaseApiController
{
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<EmbeddingsController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="embeddingService">Embedding service</param>
    /// <param name="logger">Logger</param>
    public EmbeddingsController(
        IEmbeddingService embeddingService,
        ILogger<EmbeddingsController> logger)
    {
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <summary>
    /// Create an embedding
    /// </summary>
    /// <param name="request">Embedding request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding response</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EmbeddingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<EmbeddingResponse>> CreateEmbeddingAsync(
        [FromBody] EmbeddingRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating embedding for model {ModelId}", request.ModelId);

        // Set the user ID from the claims if not provided
        if (string.IsNullOrEmpty(request.User) && User.Identity?.Name != null)
        {
            request.User = User.Identity.Name;
        }

        try
        {
            var response = await _embeddingService.CreateEmbeddingAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create embedding for model {ModelId}", request.ModelId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
