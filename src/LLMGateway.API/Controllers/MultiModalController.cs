using LLMGateway.API.Controllers.Base;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Provider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for multi-modal operations
/// </summary>
[ApiVersion("1.0")]
[Authorize]
public class MultiModalController : BaseApiController
{
    private readonly IMultiModalService _multiModalService;
    private readonly ILogger<MultiModalController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="multiModalService">Multi-modal service</param>
    /// <param name="logger">Logger</param>
    public MultiModalController(
        IMultiModalService multiModalService,
        ILogger<MultiModalController> logger)
    {
        _multiModalService = multiModalService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current user ID from the claims
    /// </summary>
    /// <returns>User ID</returns>
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
    }

    /// <summary>
    /// Create multi-modal completion
    /// </summary>
    /// <param name="request">Multi-modal completion request</param>
    /// <returns>Completion response</returns>
    [HttpPost("completions")]
    [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompletionResponse>> CreateMultiModalCompletionAsync(MultiModalCompletionRequest request)
    {
        // Set user ID if not provided
        if (string.IsNullOrEmpty(request.User))
        {
            request.User = GetUserId();
        }

        var response = await _multiModalService.CreateMultiModalCompletionAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Create streaming multi-modal completion
    /// </summary>
    /// <param name="request">Multi-modal completion request</param>
    /// <returns>Stream of completion chunks</returns>
    [HttpPost("completions/stream")]
    [ProducesResponseType(typeof(IAsyncEnumerable<CompletionChunk>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IAsyncEnumerable<CompletionChunk>> CreateStreamingMultiModalCompletionAsync(MultiModalCompletionRequest request)
    {
        // Set user ID if not provided
        if (string.IsNullOrEmpty(request.User))
        {
            request.User = GetUserId();
        }

        // Force streaming to be true
        request.Stream = true;

        var stream = _multiModalService.CreateStreamingMultiModalCompletionAsync(request);
        return Ok(stream);
    }

    /// <summary>
    /// Get multi-modal models
    /// </summary>
    /// <returns>List of multi-modal models</returns>
    [HttpGet("models")]
    [ProducesResponseType(typeof(IEnumerable<Model>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Model>>> GetMultiModalModelsAsync()
    {
        var models = await _multiModalService.GetMultiModalModelsAsync();
        return Ok(models);
    }

    /// <summary>
    /// Check if a model supports multi-modal inputs
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>True if the model supports multi-modal inputs</returns>
    [HttpGet("models/{modelId}/supports-multi-modal")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> SupportsMultiModalAsync(string modelId)
    {
        var supportsMultiModal = await _multiModalService.SupportsMultiModalAsync(modelId);
        return Ok(supportsMultiModal);
    }
}
