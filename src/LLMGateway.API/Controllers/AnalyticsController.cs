using LLMGateway.Core.Features.TokenUsage.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for analytics
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = "AdminAccess")]
public class AnalyticsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ILogger<AnalyticsController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator">Mediator</param>
    /// <param name="logger">Logger</param>
    public AnalyticsController(
        IMediator mediator,
        ILogger<AnalyticsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get token usage statistics
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="groupBy">Group by (day, month, model, user)</param>
    /// <returns>Token usage statistics</returns>
    [HttpGet("token-usage")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<object>>> GetTokenUsageStatisticsAsync(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? groupBy)
    {
        _logger.LogInformation("Getting token usage statistics");

        try
        {
            // Convert to DateTimeOffset with UTC kind
            DateTimeOffset? startDateOffset = startDate.HasValue 
                ? new DateTimeOffset(startDate.Value, TimeSpan.Zero) 
                : null;
            
            DateTimeOffset? endDateOffset = endDate.HasValue 
                ? new DateTimeOffset(endDate.Value, TimeSpan.Zero) 
                : null;

            var query = new GetTokenUsageStatisticsQuery
            {
                StartDate = startDateOffset,
                EndDate = endDateOffset,
                GroupBy = groupBy
            };

            var statistics = await _mediator.Send(query);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token usage statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}