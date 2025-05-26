using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Auth;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace LLMGateway.Core.Services;

/// <summary>
/// In-memory implementation of IUserService for when database is not available
/// </summary>
public class InMemoryUserService : IUserService
{
    private readonly ILogger<InMemoryUserService> _logger;
    private readonly ConcurrentDictionary<string, User> _users = new();
    private readonly ConcurrentDictionary<string, User> _usersByEmail = new();
    private readonly ConcurrentDictionary<string, User> _usersByUsername = new();

    public InMemoryUserService(ILogger<InMemoryUserService> logger)
    {
        _logger = logger;

        // Create a default admin user for testing
        CreateDefaultUser();
    }

    private void CreateDefaultUser()
    {
        var defaultUser = new User
        {
            Id = "default-user-id",
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = HashPassword("admin123"), // Default password
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _users[defaultUser.Id] = defaultUser;
        _usersByEmail[defaultUser.Email] = defaultUser;
        _usersByUsername[defaultUser.Username] = defaultUser;

        _logger.LogInformation("Created default admin user with username 'admin' and password 'admin123'");
    }

    public Task<User?> GetByIdAsync(string userId)
    {
        _logger.LogDebug("Getting user by ID {UserId}", userId);

        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        _logger.LogDebug("Getting user by username {Username}", username);

        _usersByUsername.TryGetValue(username, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        _logger.LogDebug("Getting user by email {Email}", email);

        _usersByEmail.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public Task<User> CreateAsync(User user, string password)
    {
        _logger.LogDebug("Creating user {Username}", user.Username);

        // Check if user already exists
        if (_usersByUsername.ContainsKey(user.Username))
        {
            throw new InvalidOperationException($"User with username '{user.Username}' already exists");
        }

        if (_usersByEmail.ContainsKey(user.Email))
        {
            throw new InvalidOperationException($"User with email '{user.Email}' already exists");
        }

        // Set user properties
        user.Id = user.Id ?? Guid.NewGuid().ToString();
        user.PasswordHash = HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        user.IsActive = true;

        // Store user
        _users[user.Id] = user;
        _usersByEmail[user.Email] = user;
        _usersByUsername[user.Username] = user;

        return Task.FromResult(user);
    }

    public Task<bool> UpdateAsync(User user)
    {
        _logger.LogDebug("Updating user {UserId}", user.Id);

        if (!_users.ContainsKey(user.Id))
        {
            throw new KeyNotFoundException($"User with ID '{user.Id}' not found");
        }

        var existingUser = _users[user.Id];

        // Update email index if email changed
        if (existingUser.Email != user.Email)
        {
            _usersByEmail.TryRemove(existingUser.Email, out _);
            _usersByEmail[user.Email] = user;
        }

        // Update username index if username changed
        if (existingUser.Username != user.Username)
        {
            _usersByUsername.TryRemove(existingUser.Username, out _);
            _usersByUsername[user.Username] = user;
        }

        user.UpdatedAt = DateTime.UtcNow;
        _users[user.Id] = user;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string userId)
    {
        _logger.LogDebug("Deleting user {UserId}", userId);

        if (_users.TryRemove(userId, out var user))
        {
            _usersByEmail.TryRemove(user.Email, out _);
            _usersByUsername.TryRemove(user.Username, out _);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> VerifyPasswordAsync(User user, string password)
    {
        _logger.LogDebug("Validating password for user {UserId}", user.Id);

        var hashedPassword = HashPassword(password);
        var isValid = user.PasswordHash == hashedPassword;

        return Task.FromResult(isValid);
    }

    public Task<bool> ValidatePasswordAsync(User user, string password)
    {
        return VerifyPasswordAsync(user, password);
    }

    public Task<bool> UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        return ChangePasswordAsync(userId, currentPassword, newPassword);
    }

    public Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        _logger.LogDebug("Changing password for user {UserId}", userId);

        if (!_users.TryGetValue(userId, out var user))
        {
            return Task.FromResult(false);
        }

        // Validate current password
        if (!ValidatePasswordAsync(user, currentPassword).Result)
        {
            return Task.FromResult(false);
        }

        // Update password
        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _users[userId] = user;

        return Task.FromResult(true);
    }

    public Task<bool> SetPasswordAsync(string userId, string newPassword)
    {
        _logger.LogDebug("Setting password for user {UserId}", userId);

        if (!_users.TryGetValue(userId, out var user))
        {
            return Task.FromResult(false);
        }

        // Update password
        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _users[userId] = user;

        return Task.FromResult(true);
    }

    public Task<bool> IsActiveAsync(string userId)
    {
        _logger.LogDebug("Checking if user {UserId} is active", userId);

        if (_users.TryGetValue(userId, out var user))
        {
            return Task.FromResult(user.IsActive);
        }

        return Task.FromResult(false);
    }

    public Task<bool> ActivateAsync(string userId)
    {
        _logger.LogDebug("Activating user {UserId}", userId);

        if (_users.TryGetValue(userId, out var user))
        {
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            _users[userId] = user;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> DeactivateAsync(string userId)
    {
        _logger.LogDebug("Deactivating user {UserId}", userId);

        if (_users.TryGetValue(userId, out var user))
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            _users[userId] = user;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<List<User>> GetAllAsync(int skip = 0, int take = 100)
    {
        _logger.LogDebug("Getting all users");

        var users = _users.Values
            .OrderBy(u => u.Username)
            .Skip(skip)
            .Take(take)
            .ToList();
        return Task.FromResult(users);
    }

    public Task<IEnumerable<User>> GetAllAsync()
    {
        _logger.LogDebug("Getting all users");

        var users = _users.Values.OrderBy(u => u.Username);
        return Task.FromResult(users.AsEnumerable());
    }

    public Task<(IEnumerable<User> Users, int TotalCount)> SearchAsync(
        string? query,
        bool? isActive,
        int page,
        int pageSize)
    {
        _logger.LogDebug("Searching users with query: {Query}", query);

        var queryable = _users.Values.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(u =>
                u.Username.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            queryable = queryable.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = queryable.Count();

        var users = queryable
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((users.AsEnumerable(), totalCount));
    }

    public Task<List<User>> GetUsersInRoleAsync(string role, int skip = 0, int take = 100)
    {
        _logger.LogDebug("Getting users in role {Role}", role);

        var users = _users.Values
            .Where(u => u.Roles != null && u.Roles.Contains(role))
            .OrderBy(u => u.Username)
            .Skip(skip)
            .Take(take)
            .ToList();
        return Task.FromResult(users);
    }

    public Task<bool> AddToRoleAsync(string userId, string role)
    {
        _logger.LogDebug("Adding user {UserId} to role {Role}", userId, role);

        if (_users.TryGetValue(userId, out var user))
        {
            user.Roles ??= new List<string>();
            if (!user.Roles.Contains(role))
            {
                user.Roles.Add(role);
                user.UpdatedAt = DateTime.UtcNow;
                _users[userId] = user;
            }
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> RemoveFromRoleAsync(string userId, string role)
    {
        _logger.LogDebug("Removing user {UserId} from role {Role}", userId, role);

        if (_users.TryGetValue(userId, out var user))
        {
            if (user.Roles != null && user.Roles.Contains(role))
            {
                user.Roles.Remove(role);
                user.UpdatedAt = DateTime.UtcNow;
                _users[userId] = user;
            }
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> IsInRoleAsync(string userId, string role)
    {
        _logger.LogDebug("Checking if user {UserId} is in role {Role}", userId, role);

        if (_users.TryGetValue(userId, out var user))
        {
            var isInRole = user.Roles != null && user.Roles.Contains(role);
            return Task.FromResult(isInRole);
        }

        return Task.FromResult(false);
    }

    private static string HashPassword(string password)
    {
        // Simple hash for in-memory implementation (not secure for production)
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
        return Convert.ToBase64String(hashedBytes);
    }
}
