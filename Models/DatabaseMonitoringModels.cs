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
