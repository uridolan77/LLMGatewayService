using LLMGateway.Core.Models.Auth;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for user service
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User</returns>
    Task<User?> GetByIdAsync(string id);
    
    /// <summary>
    /// Get user by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>User</returns>
    Task<User?> GetByUsernameAsync(string username);
    
    /// <summary>
    /// Get user by email
    /// </summary>
    /// <param name="email">Email</param>
    /// <returns>User</returns>
    Task<User?> GetByEmailAsync(string email);
    
    /// <summary>
    /// Create user
    /// </summary>
    /// <param name="user">User</param>
    /// <param name="password">Password</param>
    /// <returns>Created user</returns>
    Task<User> CreateAsync(User user, string password);
    
    /// <summary>
    /// Update user
    /// </summary>
    /// <param name="user">User</param>
    /// <returns>Whether the update was successful</returns>
    Task<bool> UpdateAsync(User user);
    
    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Whether the deletion was successful</returns>
    Task<bool> DeleteAsync(string id);
    
    /// <summary>
    /// Verify user password
    /// </summary>
    /// <param name="user">User</param>
    /// <param name="password">Password</param>
    /// <returns>Whether the password is valid</returns>
    Task<bool> VerifyPasswordAsync(User user, string password);
    
    /// <summary>
    /// Update user password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentPassword">Current password</param>
    /// <param name="newPassword">New password</param>
    /// <returns>Whether the password update was successful</returns>
    Task<bool> UpdatePasswordAsync(string userId, string currentPassword, string newPassword);
    
    /// <summary>
    /// Get all users
    /// </summary>
    /// <param name="skip">Number of users to skip</param>
    /// <param name="take">Number of users to take</param>
    /// <returns>List of users</returns>
    Task<List<User>> GetAllAsync(int skip = 0, int take = 100);
    
    /// <summary>
    /// Get all users with role
    /// </summary>
    /// <param name="role">Role</param>
    /// <param name="skip">Number of users to skip</param>
    /// <param name="take">Number of users to take</param>
    /// <returns>List of users</returns>
    Task<List<User>> GetUsersInRoleAsync(string role, int skip = 0, int take = 100);
    
    /// <summary>
    /// Add user to role
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">Role</param>
    /// <returns>Whether the role was added</returns>
    Task<bool> AddToRoleAsync(string userId, string role);
    
    /// <summary>
    /// Remove user from role
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">Role</param>
    /// <returns>Whether the role was removed</returns>
    Task<bool> RemoveFromRoleAsync(string userId, string role);
    
    /// <summary>
    /// Check if user is in role
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">Role</param>
    /// <returns>Whether the user is in the role</returns>
    Task<bool> IsInRoleAsync(string userId, string role);
}
