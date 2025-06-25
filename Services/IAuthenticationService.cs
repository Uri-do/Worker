using MonitoringWorker.Models;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for handling authentication operations
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Login response with token and user info, or null if authentication fails</returns>
    Task<LoginResponse?> AuthenticateAsync(string username, string password);

    /// <summary>
    /// Validates a JWT token and returns user information
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User information if token is valid, null otherwise</returns>
    Task<UserInfo?> ValidateTokenAsync(string token);

    /// <summary>
    /// Gets user information by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>User information if found, null otherwise</returns>
    Task<UserInfo?> GetUserAsync(string username);

    /// <summary>
    /// Checks if a user has a specific permission
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="permission">Permission to check</param>
    /// <returns>True if user has permission, false otherwise</returns>
    Task<bool> HasPermissionAsync(string username, string permission);

    /// <summary>
    /// Checks if a user has a specific role
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="role">Role to check</param>
    /// <returns>True if user has role, false otherwise</returns>
    Task<bool> HasRoleAsync(string username, string role);
}
