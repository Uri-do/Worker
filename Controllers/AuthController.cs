using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Controllers;

/// <summary>
/// Authentication controller for login and user management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IAuthenticationService _authService;

    /// <summary>
    /// Initializes a new instance of the AuthController
    /// </summary>
    public AuthController(
        ILogger<AuthController> logger,
        IAuthenticationService authService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.AuthenticateAsync(request.Username, request.Password);
            
            if (result == null)
            {
                _logger.LogWarning("Failed login attempt for username: {Username} from IP: {RemoteIpAddress}", 
                    request.Username, HttpContext.Connection.RemoteIpAddress);
                
                return Unauthorized(new { message = "Invalid username or password" });
            }

            _logger.LogInformation("Successful login for user: {Username} from IP: {RemoteIpAddress}", 
                request.Username, HttpContext.Connection.RemoteIpAddress);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return StatusCode(500, new { message = "An error occurred during authentication" });
        }
    }

    /// <summary>
    /// Gets current user information
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var username = User.FindFirst("username")?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var userInfo = await _authService.GetUserAsync(username);
            if (userInfo == null)
            {
                return Unauthorized();
            }

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user information");
            return StatusCode(500, new { message = "An error occurred while retrieving user information" });
        }
    }

    /// <summary>
    /// Validates the current JWT token
    /// </summary>
    /// <returns>Token validation result</returns>
    [HttpPost("validate")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    public IActionResult ValidateToken()
    {
        try
        {
            var username = User.FindFirst("username")?.Value;
            var userId = User.FindFirst("user_id")?.Value;
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            return Ok(new
            {
                valid = true,
                username = username,
                userId = userId,
                roles = User.FindAll("role").Select(c => c.Value).ToList(),
                permissions = User.FindAll("permission").Select(c => c.Value).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, new { message = "An error occurred while validating the token" });
        }
    }

    /// <summary>
    /// Checks if the current user has a specific permission
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <returns>Permission check result</returns>
    [HttpGet("check-permission")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CheckPermission([FromQuery, Required] string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return BadRequest(new { message = "Permission parameter is required" });
        }

        try
        {
            var username = User.FindFirst("username")?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var hasPermission = await _authService.HasPermissionAsync(username, permission);
            
            return Ok(new
            {
                permission = permission,
                hasPermission = hasPermission
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission}", permission);
            return StatusCode(500, new { message = "An error occurred while checking permission" });
        }
    }

    /// <summary>
    /// Gets available permissions for documentation
    /// </summary>
    /// <returns>List of available permissions</returns>
    [HttpGet("permissions")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult GetPermissions()
    {
        var permissions = new
        {
            monitoring = new[] { Permissions.ViewMonitoring, Permissions.ManageMonitoring },
            metrics = new[] { Permissions.ViewMetrics },
            configuration = new[] { Permissions.ManageConfiguration },
            logs = new[] { Permissions.ViewLogs },
            users = new[] { Permissions.ManageUsers }
        };

        return Ok(permissions);
    }
}
