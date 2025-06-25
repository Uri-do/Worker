using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringWorker.Configuration;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Controllers;

/// <summary>
/// Controller for configuration management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Admin)]
[Produces("application/json")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly IConfigurationService _configService;

    /// <summary>
    /// Initializes a new instance of the ConfigurationController
    /// </summary>
    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        IConfigurationService configService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <summary>
    /// Gets all configuration
    /// </summary>
    /// <returns>All configuration as dictionary</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAllConfiguration()
    {
        try
        {
            var config = await _configService.GetAllConfigurationAsync();
            
            _logger.LogInformation("All configuration retrieved by user {Username}", User.Identity?.Name);
            
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all configuration");
            return StatusCode(500, new { message = "An error occurred while retrieving configuration" });
        }
    }

    /// <summary>
    /// Gets monitoring configuration
    /// </summary>
    /// <returns>Monitoring options</returns>
    [HttpGet("monitoring")]
    [ProducesResponseType(typeof(MonitoringOptions), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult GetMonitoringConfiguration()
    {
        try
        {
            var config = _configService.GetMonitoringOptions();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monitoring configuration");
            return StatusCode(500, new { message = "An error occurred while retrieving monitoring configuration" });
        }
    }

    /// <summary>
    /// Updates monitoring configuration
    /// </summary>
    /// <param name="options">New monitoring options</param>
    /// <returns>Update result</returns>
    [HttpPut("monitoring")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateMonitoringConfiguration([FromBody] MonitoringOptions options)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var validationResult = await _configService.ValidateOptionsAsync(options);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { 
                    message = "Configuration validation failed", 
                    errors = validationResult.Errors,
                    warnings = validationResult.Warnings
                });
            }

            var success = await _configService.UpdateMonitoringOptionsAsync(options);
            if (!success)
            {
                return BadRequest(new { message = "Failed to update monitoring configuration" });
            }

            _logger.LogInformation("Monitoring configuration updated by user {Username}", User.Identity?.Name);
            
            return Ok(new { message = "Monitoring configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating monitoring configuration");
            return StatusCode(500, new { message = "An error occurred while updating monitoring configuration" });
        }
    }

    /// <summary>
    /// Gets database monitoring configuration
    /// </summary>
    /// <returns>Database monitoring options</returns>
    [HttpGet("database-monitoring")]
    [ProducesResponseType(typeof(DatabaseMonitoringOptions), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult GetDatabaseMonitoringConfiguration()
    {
        try
        {
            var config = _configService.GetDatabaseMonitoringOptions();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database monitoring configuration");
            return StatusCode(500, new { message = "An error occurred while retrieving database monitoring configuration" });
        }
    }

    /// <summary>
    /// Updates database monitoring configuration
    /// </summary>
    /// <param name="options">New database monitoring options</param>
    /// <returns>Update result</returns>
    [HttpPut("database-monitoring")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateDatabaseMonitoringConfiguration([FromBody] DatabaseMonitoringOptions options)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var validationResult = await _configService.ValidateOptionsAsync(options);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { 
                    message = "Configuration validation failed", 
                    errors = validationResult.Errors,
                    warnings = validationResult.Warnings
                });
            }

            var success = await _configService.UpdateDatabaseMonitoringOptionsAsync(options);
            if (!success)
            {
                return BadRequest(new { message = "Failed to update database monitoring configuration" });
            }

            _logger.LogInformation("Database monitoring configuration updated by user {Username}", User.Identity?.Name);
            
            return Ok(new { message = "Database monitoring configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating database monitoring configuration");
            return StatusCode(500, new { message = "An error occurred while updating database monitoring configuration" });
        }
    }

    /// <summary>
    /// Gets Quartz configuration
    /// </summary>
    /// <returns>Quartz options</returns>
    [HttpGet("quartz")]
    [ProducesResponseType(typeof(QuartzOptions), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult GetQuartzConfiguration()
    {
        try
        {
            var config = _configService.GetQuartzOptions();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Quartz configuration");
            return StatusCode(500, new { message = "An error occurred while retrieving Quartz configuration" });
        }
    }

    /// <summary>
    /// Gets configuration value by key
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <returns>Configuration value</returns>
    [HttpGet("value/{key}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetConfigurationValue([FromRoute, Required] string key)
    {
        try
        {
            var value = await _configService.GetConfigurationValueAsync(key);
            
            if (value == null)
            {
                return NotFound(new { message = $"Configuration key '{key}' not found" });
            }
            
            return Ok(new { key = key, value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration value for key {Key}", key);
            return StatusCode(500, new { message = "An error occurred while retrieving configuration value" });
        }
    }

    /// <summary>
    /// Sets configuration value by key
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="request">Configuration value request</param>
    /// <returns>Set result</returns>
    [HttpPut("value/{key}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> SetConfigurationValue(
        [FromRoute, Required] string key,
        [FromBody] ConfigurationValueRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var success = await _configService.SetConfigurationValueAsync(key, request.Value);
            if (!success)
            {
                return BadRequest(new { message = "Failed to set configuration value" });
            }

            _logger.LogInformation("Configuration value for key {Key} set by user {Username}", 
                key, User.Identity?.Name);
            
            return Ok(new { message = "Configuration value set successfully", key = key, value = request.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration value for key {Key}", key);
            return StatusCode(500, new { message = "An error occurred while setting configuration value" });
        }
    }

    /// <summary>
    /// Reloads configuration from sources
    /// </summary>
    /// <returns>Reload result</returns>
    [HttpPost("reload")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ReloadConfiguration()
    {
        try
        {
            var success = await _configService.ReloadConfigurationAsync();
            if (!success)
            {
                return BadRequest(new { message = "Configuration reload not supported or failed" });
            }

            _logger.LogInformation("Configuration reloaded by user {Username}", User.Identity?.Name);
            
            return Ok(new { message = "Configuration reloaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading configuration");
            return StatusCode(500, new { message = "An error occurred while reloading configuration" });
        }
    }

    /// <summary>
    /// Gets configuration change history
    /// </summary>
    /// <param name="limit">Maximum number of changes to return</param>
    /// <returns>Configuration change history</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<ConfigurationChange>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetChangeHistory([FromQuery] int limit = 100)
    {
        try
        {
            var history = await _configService.GetChangeHistoryAsync(limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration change history");
            return StatusCode(500, new { message = "An error occurred while retrieving change history" });
        }
    }
}

/// <summary>
/// Configuration value request model
/// </summary>
public class ConfigurationValueRequest
{
    /// <summary>
    /// Configuration value
    /// </summary>
    [Required(ErrorMessage = "Value is required")]
    public string Value { get; set; } = string.Empty;
}
