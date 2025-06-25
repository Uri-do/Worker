using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Models;

/// <summary>
/// User roles for authorization
/// </summary>
public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";
}

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response model
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (Bearer)
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    public UserInfo User { get; set; } = new();
}

/// <summary>
/// User information model
/// </summary>
public class UserInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// User permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// JWT token claims
/// </summary>
public class TokenClaims
{
    /// <summary>
    /// User ID claim
    /// </summary>
    public const string UserId = "user_id";

    /// <summary>
    /// Username claim
    /// </summary>
    public const string Username = "username";

    /// <summary>
    /// Role claim
    /// </summary>
    public const string Role = "role";

    /// <summary>
    /// Permission claim
    /// </summary>
    public const string Permission = "permission";
}

/// <summary>
/// User permissions for fine-grained access control
/// </summary>
public static class Permissions
{
    public const string ViewMonitoring = "monitoring:view";
    public const string ManageMonitoring = "monitoring:manage";
    public const string ViewMetrics = "metrics:view";
    public const string ManageConfiguration = "config:manage";
    public const string ViewLogs = "logs:view";
    public const string ManageUsers = "users:manage";
}
