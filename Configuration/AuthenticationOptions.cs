using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Configuration;

/// <summary>
/// Authentication configuration options
/// </summary>
public class AuthenticationOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Authentication";

    /// <summary>
    /// JWT configuration
    /// </summary>
    [Required]
    public JwtOptions Jwt { get; set; } = new();

    /// <summary>
    /// User configuration
    /// </summary>
    [Required]
    public UsersOptions Users { get; set; } = new();

    /// <summary>
    /// Whether authentication is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to require authentication for health checks
    /// </summary>
    public bool RequireAuthForHealthChecks { get; set; } = false;
}

/// <summary>
/// JWT configuration options
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// JWT secret key for signing tokens
    /// </summary>
    [Required(ErrorMessage = "JWT secret key is required")]
    [MinLength(32, ErrorMessage = "JWT secret key must be at least 32 characters")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT issuer
    /// </summary>
    [Required(ErrorMessage = "JWT issuer is required")]
    public string Issuer { get; set; } = "MonitoringWorker";

    /// <summary>
    /// JWT audience
    /// </summary>
    [Required(ErrorMessage = "JWT audience is required")]
    public string Audience { get; set; } = "MonitoringWorker";

    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    [Range(5, 1440, ErrorMessage = "Token expiration must be between 5 minutes and 24 hours")]
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Clock skew tolerance in minutes
    /// </summary>
    [Range(0, 10, ErrorMessage = "Clock skew must be between 0 and 10 minutes")]
    public int ClockSkewMinutes { get; set; } = 5;
}

/// <summary>
/// Users configuration options
/// </summary>
public class UsersOptions
{
    /// <summary>
    /// Default users for development/testing
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one user must be configured")]
    public List<UserConfig> DefaultUsers { get; set; } = new();
}

/// <summary>
/// User configuration
/// </summary>
public class UserConfig
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required(ErrorMessage = "User ID is required")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password hash (for development - use proper identity provider in production)
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User roles
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one role must be assigned")]
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// User permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Whether the user is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}
