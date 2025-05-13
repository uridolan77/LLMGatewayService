using LLMGateway.API.Controllers;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Users controller
/// </summary>
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userService">User service</param>
    /// <param name="logger">Logger</param>
    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all users
    /// </summary>
    /// <param name="skip">Number of users to skip</param>
    /// <param name="take">Number of users to take</param>
    /// <returns>List of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<User>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var users = await _userService.GetAllAsync(skip, take);
        return Ok(users);
    }
    
    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(User), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        
        if (user == null)
        {
            return NotFound();
        }
        
        return Ok(user);
    }
    
    /// <summary>
    /// Create user
    /// </summary>
    /// <param name="request">Create user request</param>
    /// <returns>Created user</returns>
    [HttpPost]
    [ProducesResponseType(typeof(User), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        try
        {            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Roles = new List<string> { request.Role },
                IsActive = true
            };
            
            var createdUser = await _userService.CreateAsync(user, request.Password);
            
            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Update user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Update user request</param>
    /// <returns>No content</returns>
    [HttpPut("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
              // Update user properties
            user.Username = request.Username;
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.IsActive = request.IsActive;
            user.Roles = new List<string> { request.Role };
            
            var result = await _userService.UpdateAsync(user);
            
            if (!result)
            {
                return BadRequest();
            }
            
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _userService.DeleteAsync(id);
        
        if (!result)
        {
            return NotFound();
        }
        
        return NoContent();
    }
    
    /// <summary>
    /// Update user password
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Update password request</param>
    /// <returns>No content</returns>
    [HttpPut("{id}/password")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> UpdatePassword(string id, [FromBody] UpdatePasswordRequest request)
    {
        var user = await _userService.GetByIdAsync(id);
        
        if (user == null)
        {
            return NotFound();
        }
        
        var result = await _userService.UpdatePasswordAsync(id, request.CurrentPassword, request.NewPassword);
        
        if (!result)
        {
            return BadRequest(new { message = "Invalid current password" });
        }
        
        return NoContent();
    }
}

/// <summary>
/// Create user request
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Role
    /// </summary>
    public string Role { get; set; } = "User";
}

/// <summary>
/// Update user request
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the user is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Role
    /// </summary>
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Update password request
/// </summary>
public class UpdatePasswordRequest
{
    /// <summary>
    /// Current password
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// New password
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}
