using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Controllers;

/// <summary>
/// Controller for database monitoring operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DatabaseMonitoringController : ControllerBase
{
    private readonly ILogger<DatabaseMonitoringController> _logger;
    private readonly IDatabaseMonitoringService _dbMonitoringService;

    /// <summary>
    /// Initializes a new instance of the DatabaseMonitoringController
    /// </summary>
    public DatabaseMonitoringController(
        ILogger<DatabaseMonitoringController> logger,
        IDatabaseMonitoringService dbMonitoringService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbMonitoringService = dbMonitoringService ?? throw new ArgumentNullException(nameof(dbMonitoringService));
    }

    /// <summary>
    /// Tests all configured database connections
    /// </summary>
    /// <returns>Collection of connection health results</returns>
    [HttpGet("connections/test")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(IEnumerable<DatabaseConnectionHealth>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> TestConnections()
    {
        try
        {
            var results = await _dbMonitoringService.TestConnectionsAsync();
            
            _logger.LogInformation("Database connection tests completed by user {Username}", 
                User.Identity?.Name);
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connections");
            return StatusCode(500, new { message = "An error occurred while testing database connections" });
        }
    }

    /// <summary>
    /// Tests a specific database connection
    /// </summary>
    /// <param name="connectionName">Name of the connection to test</param>
    /// <returns>Connection health result</returns>
    [HttpGet("connections/{connectionName}/test")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(DatabaseConnectionHealth), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> TestConnection([FromRoute, Required] string connectionName)
    {
        try
        {
            var result = await _dbMonitoringService.TestConnectionAsync(connectionName);
            
            if (result == null)
            {
                return NotFound(new { message = $"Connection '{connectionName}' not found or disabled" });
            }
            
            _logger.LogInformation("Database connection {ConnectionName} tested by user {Username}", 
                connectionName, User.Identity?.Name);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connection {ConnectionName}", connectionName);
            return StatusCode(500, new { message = "An error occurred while testing the database connection" });
        }
    }

    /// <summary>
    /// Executes all configured monitoring queries
    /// </summary>
    /// <returns>Collection of query results</returns>
    [HttpGet("queries/execute")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(IEnumerable<DatabaseMonitoringResult>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ExecuteMonitoringQueries()
    {
        try
        {
            var results = await _dbMonitoringService.ExecuteMonitoringQueriesAsync();
            
            _logger.LogInformation("Database monitoring queries executed by user {Username}", 
                User.Identity?.Name);
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing database monitoring queries");
            return StatusCode(500, new { message = "An error occurred while executing monitoring queries" });
        }
    }

    /// <summary>
    /// Executes monitoring queries for a specific connection
    /// </summary>
    /// <param name="connectionName">Name of the connection</param>
    /// <returns>Collection of query results for the connection</returns>
    [HttpGet("connections/{connectionName}/queries")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(IEnumerable<DatabaseMonitoringResult>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ExecuteConnectionQueries([FromRoute, Required] string connectionName)
    {
        try
        {
            var results = await _dbMonitoringService.ExecuteConnectionQueriesAsync(connectionName);
            
            if (!results.Any())
            {
                return NotFound(new { message = $"Connection '{connectionName}' not found or has no queries configured" });
            }
            
            _logger.LogInformation("Database queries for connection {ConnectionName} executed by user {Username}", 
                connectionName, User.Identity?.Name);
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing queries for connection {ConnectionName}", connectionName);
            return StatusCode(500, new { message = "An error occurred while executing connection queries" });
        }
    }

    /// <summary>
    /// Executes a specific monitoring query
    /// </summary>
    /// <param name="connectionName">Name of the connection</param>
    /// <param name="queryName">Name of the query</param>
    /// <returns>Query result</returns>
    [HttpGet("connections/{connectionName}/queries/{queryName}")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(DatabaseMonitoringResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ExecuteQuery(
        [FromRoute, Required] string connectionName, 
        [FromRoute, Required] string queryName)
    {
        try
        {
            var result = await _dbMonitoringService.ExecuteQueryAsync(connectionName, queryName);
            
            if (result == null)
            {
                return NotFound(new { message = $"Connection '{connectionName}' or query '{queryName}' not found" });
            }
            
            _logger.LogInformation("Database query {QueryName} on connection {ConnectionName} executed by user {Username}", 
                queryName, connectionName, User.Identity?.Name);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query {QueryName} on connection {ConnectionName}", 
                queryName, connectionName);
            return StatusCode(500, new { message = "An error occurred while executing the query" });
        }
    }

    /// <summary>
    /// Executes a custom SQL query for monitoring
    /// </summary>
    /// <param name="connectionName">Name of the connection</param>
    /// <param name="request">Custom query request</param>
    /// <returns>Query execution result</returns>
    [HttpPost("connections/{connectionName}/custom-query")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Operator)]
    [ProducesResponseType(typeof(DatabaseMonitoringResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ExecuteCustomQuery(
        [FromRoute, Required] string connectionName,
        [FromBody] CustomQueryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _dbMonitoringService.ExecuteCustomQueryAsync(
                connectionName, 
                request.Sql, 
                request.Parameters,
                request.TimeoutSeconds);
            
            _logger.LogInformation("Custom database query executed on connection {ConnectionName} by user {Username}", 
                connectionName, User.Identity?.Name);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing custom query on connection {ConnectionName}", connectionName);
            return StatusCode(500, new { message = "An error occurred while executing the custom query" });
        }
    }

    /// <summary>
    /// Gets database monitoring statistics
    /// </summary>
    /// <returns>Database monitoring statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Policy = "ViewMetrics")]
    [ProducesResponseType(typeof(DatabaseMonitoringStats), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _dbMonitoringService.GetStatisticsAsync();
            
            _logger.LogDebug("Database monitoring statistics retrieved by user {Username}", 
                User.Identity?.Name);
            
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database monitoring statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
        }
    }

    /// <summary>
    /// Gets list of configured connections
    /// </summary>
    /// <returns>List of connection names</returns>
    [HttpGet("connections")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetConnections()
    {
        try
        {
            var connections = await _dbMonitoringService.GetConnectionNamesAsync();
            return Ok(connections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database connections");
            return StatusCode(500, new { message = "An error occurred while retrieving connections" });
        }
    }

    /// <summary>
    /// Gets list of configured queries
    /// </summary>
    /// <returns>List of query names and descriptions</returns>
    [HttpGet("queries")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetQueries()
    {
        try
        {
            var queries = await _dbMonitoringService.GetQueryNamesAsync();
            var result = queries.Select(q => new { name = q.Name, description = q.Description });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database queries");
            return StatusCode(500, new { message = "An error occurred while retrieving queries" });
        }
    }

    /// <summary>
    /// Gets database monitoring dashboard summary
    /// </summary>
    /// <returns>Dashboard summary with real-time status</returns>
    [HttpGet("dashboard")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var connections = await _dbMonitoringService.TestConnectionsAsync();
            var stats = await _dbMonitoringService.GetStatisticsAsync();

            var dashboard = new
            {
                Summary = new
                {
                    TotalConnections = stats.TotalConnections,
                    HealthyConnections = connections.Count(c => c.Status == MonitoringStatus.Healthy),
                    WarningConnections = connections.Count(c => c.Status == MonitoringStatus.Warning),
                    CriticalConnections = connections.Count(c => c.Status == MonitoringStatus.Critical),
                    ErrorConnections = connections.Count(c => c.Status == MonitoringStatus.Error),
                    OverallHealthPercentage = stats.TotalConnections > 0
                        ? (double)connections.Count(c => c.Status == MonitoringStatus.Healthy) / stats.TotalConnections * 100
                        : 0,
                    LastUpdate = DateTime.UtcNow
                },
                Connections = connections.Select(c => new
                {
                    c.ConnectionName,
                    Status = c.Status.ToString(),
                    c.Message,
                    c.DurationMs,
                    c.Timestamp,
                    c.ServerVersion,
                    c.DatabaseName
                }),
                Statistics = stats
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database monitoring dashboard");
            return StatusCode(500, new { message = "An error occurred while retrieving dashboard data" });
        }
    }

    /// <summary>
    /// Gets monitoring results with filtering and pagination
    /// </summary>
    /// <param name="connectionName">Filter by connection name</param>
    /// <param name="status">Filter by status</param>
    /// <param name="environment">Filter by environment</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <returns>Paginated monitoring results</returns>
    [HttpGet("results")]
    [Authorize(Policy = "ViewMonitoring")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetMonitoringResults(
        [FromQuery] string? connectionName = null,
        [FromQuery] string? status = null,
        [FromQuery] string? environment = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageNumber = 1)
    {
        try
        {
            // For now, return recent results from the service
            // In a full implementation, this would query the database with filtering
            var allResults = await _dbMonitoringService.ExecuteMonitoringQueriesAsync();

            // Apply basic filtering (in a real implementation, this would be done at the database level)
            var filteredResults = allResults.AsEnumerable();

            if (!string.IsNullOrEmpty(connectionName))
                filteredResults = filteredResults.Where(r => r.ConnectionName.Contains(connectionName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(status))
                filteredResults = filteredResults.Where(r => r.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(environment))
                filteredResults = filteredResults.Where(r => r.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase));

            var totalCount = filteredResults.Count();
            var pagedResults = filteredResults
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new
            {
                Items = pagedResults,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = pageNumber * pageSize < totalCount,
                HasPreviousPage = pageNumber > 1
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitoring results");
            return StatusCode(500, new { message = "An error occurred while retrieving monitoring results" });
        }
    }

    /// <summary>
    /// Gets monitoring trends and analytics
    /// </summary>
    /// <param name="period">Time period (24h, 7d, 30d)</param>
    /// <param name="groupBy">Group by (hour, day)</param>
    /// <returns>Monitoring trends data</returns>
    [HttpGet("trends")]
    [Authorize(Policy = "ViewMetrics")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetMonitoringTrends(
        [FromQuery] string period = "24h",
        [FromQuery] string groupBy = "hour")
    {
        try
        {
            // For now, return mock trend data
            // In a full implementation, this would query historical data from the database
            var trends = new
            {
                Period = period,
                GroupBy = groupBy,
                Data = new[]
                {
                    new { Timestamp = DateTime.UtcNow.AddHours(-24), HealthyCount = 8, WarningCount = 1, ErrorCount = 0 },
                    new { Timestamp = DateTime.UtcNow.AddHours(-12), HealthyCount = 9, WarningCount = 0, ErrorCount = 0 },
                    new { Timestamp = DateTime.UtcNow.AddHours(-6), HealthyCount = 8, WarningCount = 1, ErrorCount = 1 },
                    new { Timestamp = DateTime.UtcNow, HealthyCount = 9, WarningCount = 0, ErrorCount = 0 }
                },
                Summary = new
                {
                    AverageHealthyPercentage = 88.9,
                    TotalChecks = 1250,
                    AverageResponseTime = 245.5,
                    UptimePercentage = 99.2
                }
            };

            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitoring trends");
            return StatusCode(500, new { message = "An error occurred while retrieving trends data" });
        }
    }
}

/// <summary>
/// Custom query request model
/// </summary>
public class CustomQueryRequest
{
    /// <summary>
    /// SQL query to execute
    /// </summary>
    [Required(ErrorMessage = "SQL query is required")]
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Query parameters
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Query timeout in seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;
}
