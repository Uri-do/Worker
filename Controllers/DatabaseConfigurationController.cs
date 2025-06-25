using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringWorker.Models;

namespace MonitoringWorker.Controllers;

/// <summary>
/// Controller for managing database monitoring configuration
/// </summary>
[ApiController]
[Route("api/database-monitoring/config")]
[Authorize]
public class DatabaseConfigurationController : ControllerBase
{
    private readonly ILogger<DatabaseConfigurationController> _logger;

    public DatabaseConfigurationController(ILogger<DatabaseConfigurationController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all database connections
    /// </summary>
    /// <returns>List of database connections</returns>
    [HttpGet("connections")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(IEnumerable<DatabaseConnectionDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult GetDatabaseConnections()
    {
        try
        {
            // In a full implementation, this would query the database
            var connections = new[]
            {
                new DatabaseConnectionDto
                {
                    ConnectionId = Guid.NewGuid(),
                    ConnectionName = "Production-DB",
                    Provider = "SqlServer",
                    Environment = "Production",
                    Tags = new List<string> { "critical", "primary" },
                    ConnectionTimeoutSeconds = 30,
                    CommandTimeoutSeconds = 30,
                    IsEnabled = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    ModifiedDate = DateTime.UtcNow.AddDays(-1)
                },
                new DatabaseConnectionDto
                {
                    ConnectionId = Guid.NewGuid(),
                    ConnectionName = "Development-DB",
                    Provider = "SqlServer",
                    Environment = "Development",
                    Tags = new List<string> { "dev", "testing" },
                    ConnectionTimeoutSeconds = 30,
                    CommandTimeoutSeconds = 30,
                    IsEnabled = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-15),
                    ModifiedDate = DateTime.UtcNow.AddDays(-2)
                }
            };

            return Ok(connections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database connections");
            return StatusCode(500, new { message = "An error occurred while retrieving database connections" });
        }
    }

    /// <summary>
    /// Get specific database connection
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <returns>Database connection details</returns>
    [HttpGet("connections/{connectionId}")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(DatabaseConnectionDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult GetDatabaseConnection([FromRoute] Guid connectionId)
    {
        try
        {
            // In a full implementation, this would query the database
            var connection = new DatabaseConnectionDto
            {
                ConnectionId = connectionId,
                ConnectionName = "Production-DB",
                Provider = "SqlServer",
                Environment = "Production",
                Tags = new List<string> { "critical", "primary" },
                ConnectionTimeoutSeconds = 30,
                CommandTimeoutSeconds = 30,
                IsEnabled = true,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                ModifiedDate = DateTime.UtcNow.AddDays(-1)
            };

            return Ok(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database connection {ConnectionId}", connectionId);
            return StatusCode(500, new { message = "An error occurred while retrieving the database connection" });
        }
    }

    /// <summary>
    /// Create new database connection
    /// </summary>
    /// <param name="request">Connection creation request</param>
    /// <returns>Created database connection</returns>
    [HttpPost("connections")]
    [Authorize(Policy = "ManageMonitoring")]
    [ProducesResponseType(typeof(DatabaseConnectionDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult CreateDatabaseConnection([FromBody] CreateDatabaseConnectionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // In a full implementation, this would save to the database
            var connection = new DatabaseConnectionDto
            {
                ConnectionId = Guid.NewGuid(),
                ConnectionName = request.ConnectionName,
                Provider = request.Provider,
                Environment = request.Environment,
                Tags = request.Tags ?? new List<string>(),
                ConnectionTimeoutSeconds = request.ConnectionTimeoutSeconds,
                CommandTimeoutSeconds = request.CommandTimeoutSeconds,
                IsEnabled = request.IsEnabled,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _logger.LogInformation("Database connection {ConnectionName} created by user {User}", 
                request.ConnectionName, User.Identity?.Name);

            return CreatedAtAction(nameof(GetDatabaseConnection), 
                new { connectionId = connection.ConnectionId }, connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database connection {ConnectionName}", request.ConnectionName);
            return StatusCode(500, new { message = "An error occurred while creating the database connection" });
        }
    }

    /// <summary>
    /// Update database connection
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <param name="request">Connection update request</param>
    /// <returns>Updated database connection</returns>
    [HttpPut("connections/{connectionId}")]
    [Authorize(Policy = "ManageMonitoring")]
    [ProducesResponseType(typeof(DatabaseConnectionDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult UpdateDatabaseConnection(
        [FromRoute] Guid connectionId,
        [FromBody] UpdateDatabaseConnectionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // In a full implementation, this would update the database
            var connection = new DatabaseConnectionDto
            {
                ConnectionId = connectionId,
                ConnectionName = request.ConnectionName,
                Provider = request.Provider,
                Environment = request.Environment,
                Tags = request.Tags ?? new List<string>(),
                ConnectionTimeoutSeconds = request.ConnectionTimeoutSeconds,
                CommandTimeoutSeconds = request.CommandTimeoutSeconds,
                IsEnabled = request.IsEnabled,
                CreatedDate = DateTime.UtcNow.AddDays(-30), // Would come from database
                ModifiedDate = DateTime.UtcNow
            };

            _logger.LogInformation("Database connection {ConnectionId} updated by user {User}", 
                connectionId, User.Identity?.Name);

            return Ok(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating database connection {ConnectionId}", connectionId);
            return StatusCode(500, new { message = "An error occurred while updating the database connection" });
        }
    }

    /// <summary>
    /// Delete database connection
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <returns>Operation result</returns>
    [HttpDelete("connections/{connectionId}")]
    [Authorize(Policy = "ManageMonitoring")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult DeleteDatabaseConnection([FromRoute] Guid connectionId)
    {
        try
        {
            // In a full implementation, this would delete from the database
            _logger.LogInformation("Database connection {ConnectionId} deleted by user {User}", 
                connectionId, User.Identity?.Name);

            return Ok(new { success = true, message = "Database connection deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting database connection {ConnectionId}", connectionId);
            return StatusCode(500, new { message = "An error occurred while deleting the database connection" });
        }
    }

    /// <summary>
    /// Test database connection
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <returns>Connection test result</returns>
    [HttpPost("connections/{connectionId}/test")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult TestDatabaseConnection([FromRoute] Guid connectionId)
    {
        try
        {
            // In a full implementation, this would test the actual connection
            var result = new
            {
                ConnectionId = connectionId,
                Status = "Healthy",
                Message = "Connection successful",
                DurationMs = 245,
                ServerVersion = "Microsoft SQL Server 2022",
                DatabaseName = "ProductionDB",
                Timestamp = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connection {ConnectionId}", connectionId);
            return StatusCode(500, new { message = "An error occurred while testing the database connection" });
        }
    }
}
