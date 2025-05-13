using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LLMGateway.API.Controllers.Base;

/// <summary>
/// Base API controller
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Get the current user ID from the claims
    /// </summary>
    /// <returns>User ID</returns>
    protected string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
    }
}
