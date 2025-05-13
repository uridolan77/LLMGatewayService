using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Provider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for models
/// </summary>
[ApiVersion("1.0")]
[Authorize]
public class ModelsController : BaseApiController
{
    private readonly IModelService _modelService;
    private readonly ILogger<ModelsController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="modelService">Model service</param>
    /// <param name="logger">Logger</param>
    public ModelsController(
        IModelService modelService,
        ILogger<ModelsController> logger)
    {
        _modelService = modelService;
        _logger = logger;
    }

    /// <summary>
    /// Get all models
    /// </summary>
    /// <returns>List of models</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ModelInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ModelInfo>>> GetModelsAsync()
    {
        _logger.LogInformation("Getting all models");

        try
        {
            var models = await _modelService.GetModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get models");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a model by ID
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <returns>Model</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ModelInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ModelInfo>> GetModelAsync(string id)
    {
        _logger.LogInformation("Getting model {ModelId}", id);

        try
        {
            var model = await _modelService.GetModelAsync(id);
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model {ModelId}", id);
            
            if (ex is LLMGateway.Core.Exceptions.ModelNotFoundException)
            {
                return NotFound(new { error = ex.Message });
            }
            
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
