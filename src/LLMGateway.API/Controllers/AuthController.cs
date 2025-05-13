using LLMGateway.API.Controllers;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Authentication controller
/// </summary>
[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="authService">Auth service</param>
    /// <param name="userService">User service</param>
    /// <param name="logger">Logger</param>
    public AuthController(
        IAuthService authService,
        IUserService userService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _logger = logger;
    }
    
    /// <summary>
    /// Login endpoint
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>Login response</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Get client IP address
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // Attempt login
        var response = await _authService.LoginAsync(request, ipAddress);
        
        if (response == null)
        {
            return Unauthorized();
        }
        
        // Set refresh token cookie
        SetRefreshTokenCookie(response.RefreshToken);
        
        return Ok(response);
    }
    
    /// <summary>
    /// Register endpoint
    /// </summary>
    /// <param name="request">Register request</param>
    /// <returns>User</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(User), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Register user
            var user = await _authService.RegisterAsync(request);
            
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Refresh token endpoint
    /// </summary>
    /// <returns>Login response</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> RefreshToken()
    {
        // Get refresh token from cookie
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { message = "Invalid token" });
        }
        
        // Get client IP address
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // Refresh token
        var response = await _authService.RefreshTokenAsync(refreshToken, ipAddress);
        
        if (response == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }
        
        // Set refresh token cookie
        SetRefreshTokenCookie(response.RefreshToken);
        
        return Ok(response);
    }
    
    /// <summary>
    /// Logout endpoint
    /// </summary>
    /// <returns>No content</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Logout()
    {
        // Get user ID from claims
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Ok();
        }
        
        // Get client IP address
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // Logout user
        await _authService.LogoutAsync(userId, ipAddress);
        
        // Remove refresh token cookie
        Response.Cookies.Delete("refreshToken");
        
        return Ok();
    }
    
    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User</returns>
    [HttpGet("users/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(User), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        
        if (user == null)
        {
            return NotFound();
        }
        
        return Ok(user);
    }
    
    #region Private methods
    
    /// <summary>
    /// Set refresh token cookie
    /// </summary>
    /// <param name="token">Refresh token</param>
    private void SetRefreshTokenCookie(string token)
    {
        // Create cookie options
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(7),
            SameSite = SameSiteMode.Strict,
            Secure = true // Requires HTTPS
        };
        
        // Set cookie
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
    
    #endregion
}
