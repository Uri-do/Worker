using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Models;

/// <summary>
/// Database monitoring result
/// </summary>
public class DatabaseMonitoringResult
{
    /// <summary>
    /// Connection name
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Query name
    /// </summary>
    public string QueryName { get; set; } = string.Empty;

    /// <summary>
    /// Monitoring status
    /// </summary>
    public MonitoringStatus Status { get; set; }

    /// <summary>
    /// Result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Query execution time
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Query duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Query result value
    /// </summary>
    public object? ResultValue { get; set; }

    /// <summary>
    /// Additional result details
    /// </summary>
    public object? Details { get; set; }

    /// <summary>
    /// Database provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Environment
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Connection tags
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Database connection health information
/// </summary>
public class DatabaseConnectionHealth
{
    /// <summary>
    /// Connection name
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Connection status
    /// </summary>
    public MonitoringStatus Status { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Connection test timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Connection test duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Database server version
    /// </summary>
    public string? ServerVersion { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Connection details
    /// </summary>
    public object? Details { get; set; }
}

/// <summary>
/// Database query execution context
/// </summary>
public class DatabaseQueryContext
{
    /// <summary>
    /// Connection name
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Query name
    /// </summary>
    public string QueryName { get; set; } = string.Empty;

    /// <summary>
    /// SQL query
    /// </summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Query parameters
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Query timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Expected result type
    /// </summary>
    public string ResultType { get; set; } = "Scalar";

    /// <summary>
    /// Expected value for validation
    /// </summary>
    public object? ExpectedValue { get; set; }

    /// <summary>
    /// Comparison operator
    /// </summary>
    public string ComparisonOperator { get; set; } = "Equals";

    /// <summary>
    /// Warning threshold
    /// </summary>
    public double? WarningThreshold { get; set; }

    /// <summary>
    /// Critical threshold
    /// </summary>
    public double? CriticalThreshold { get; set; }
}

/// <summary>
/// Database monitoring statistics
/// </summary>
public class DatabaseMonitoringStats
{
    /// <summary>
    /// Total connections monitored
    /// </summary>
    public int TotalConnections { get; set; }

    /// <summary>
    /// Healthy connections count
    /// </summary>
    public int HealthyConnections { get; set; }

    /// <summary>
    /// Unhealthy connections count
    /// </summary>
    public int UnhealthyConnections { get; set; }

    /// <summary>
    /// Error connections count
    /// </summary>
    public int ErrorConnections { get; set; }

    /// <summary>
    /// Total queries executed
    /// </summary>
    public int TotalQueries { get; set; }

    /// <summary>
    /// Successful queries count
    /// </summary>
    public int SuccessfulQueries { get; set; }

    /// <summary>
    /// Failed queries count
    /// </summary>
    public int FailedQueries { get; set; }

    /// <summary>
    /// Average query duration in milliseconds
    /// </summary>
    public double AverageQueryDurationMs { get; set; }

    /// <summary>
    /// Last monitoring run timestamp
    /// </summary>
    public DateTimeOffset LastRunTimestamp { get; set; }

    /// <summary>
    /// Statistics by environment
    /// </summary>
    public Dictionary<string, EnvironmentStats> ByEnvironment { get; set; } = new();
}

/// <summary>
/// Environment-specific statistics
/// </summary>
public class EnvironmentStats
{
    /// <summary>
    /// Environment name
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Connection count
    /// </summary>
    public int ConnectionCount { get; set; }

    /// <summary>
    /// Healthy connections
    /// </summary>
    public int HealthyCount { get; set; }

    /// <summary>
    /// Unhealthy connections
    /// </summary>
    public int UnhealthyCount { get; set; }

    /// <summary>
    /// Error connections
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Average response time
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
}

/// <summary>
/// Database connection DTO (excludes sensitive connection string)
/// </summary>
public class DatabaseConnectionDto
{
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public int ConnectionTimeoutSeconds { get; set; }
    public int CommandTimeoutSeconds { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Create database connection request
/// </summary>
public class CreateDatabaseConnectionRequest
{
    [Required(ErrorMessage = "Connection name is required")]
    [StringLength(100, ErrorMessage = "Connection name must not exceed 100 characters")]
    public string ConnectionName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Provider is required")]
    public string Provider { get; set; } = string.Empty;

    [Required(ErrorMessage = "Connection string is required")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(ErrorMessage = "Environment is required")]
    public string Environment { get; set; } = string.Empty;

    public List<string>? Tags { get; set; }

    [Range(1, 300, ErrorMessage = "Connection timeout must be between 1 and 300 seconds")]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    [Range(1, 300, ErrorMessage = "Command timeout must be between 1 and 300 seconds")]
    public int CommandTimeoutSeconds { get; set; } = 30;

    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Update database connection request
/// </summary>
public class UpdateDatabaseConnectionRequest
{
    [Required(ErrorMessage = "Connection name is required")]
    [StringLength(100, ErrorMessage = "Connection name must not exceed 100 characters")]
    public string ConnectionName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Provider is required")]
    public string Provider { get; set; } = string.Empty;

    public string? ConnectionString { get; set; } // Optional for updates

    [Required(ErrorMessage = "Environment is required")]
    public string Environment { get; set; } = string.Empty;

    public List<string>? Tags { get; set; }

    [Range(1, 300, ErrorMessage = "Connection timeout must be between 1 and 300 seconds")]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    [Range(1, 300, ErrorMessage = "Command timeout must be between 1 and 300 seconds")]
    public int CommandTimeoutSeconds { get; set; } = 30;

    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Paginated result wrapper
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
