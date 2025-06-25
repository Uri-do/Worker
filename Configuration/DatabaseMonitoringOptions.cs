using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Configuration;

/// <summary>
/// Database monitoring configuration options
/// </summary>
public class DatabaseMonitoringOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "DatabaseMonitoring";

    /// <summary>
    /// Whether database monitoring is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default connection timeout in seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "Connection timeout must be between 1 and 300 seconds")]
    public int DefaultConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Default command timeout in seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "Command timeout must be between 1 and 300 seconds")]
    public int DefaultCommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of concurrent database connections
    /// </summary>
    [Range(1, 100, ErrorMessage = "Max connections must be between 1 and 100")]
    public int MaxConcurrentConnections { get; set; } = 10;

    /// <summary>
    /// Database connections to monitor
    /// </summary>
    [Required]
    public List<DatabaseConnectionConfig> Connections { get; set; } = new();

    /// <summary>
    /// Predefined monitoring queries
    /// </summary>
    public List<DatabaseQueryConfig> Queries { get; set; } = new();
}

/// <summary>
/// Database connection configuration
/// </summary>
public class DatabaseConnectionConfig
{
    /// <summary>
    /// Connection name/identifier
    /// </summary>
    [Required(ErrorMessage = "Connection name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Connection name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Connection string
    /// </summary>
    [Required(ErrorMessage = "Connection string is required")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database provider (SqlServer, PostgreSQL, MySQL, etc.)
    /// </summary>
    [Required(ErrorMessage = "Provider is required")]
    public string Provider { get; set; } = "SqlServer";

    /// <summary>
    /// Connection timeout in seconds (overrides default)
    /// </summary>
    [Range(1, 300, ErrorMessage = "Connection timeout must be between 1 and 300 seconds")]
    public int? ConnectionTimeoutSeconds { get; set; }

    /// <summary>
    /// Command timeout in seconds (overrides default)
    /// </summary>
    [Range(1, 300, ErrorMessage = "Command timeout must be between 1 and 300 seconds")]
    public int? CommandTimeoutSeconds { get; set; }

    /// <summary>
    /// Whether this connection is enabled for monitoring
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Tags for categorizing connections
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Environment (Development, Staging, Production)
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Queries to run for this specific connection
    /// </summary>
    public List<string> QueryNames { get; set; } = new();
}

/// <summary>
/// Database query configuration for monitoring
/// </summary>
public class DatabaseQueryConfig
{
    /// <summary>
    /// Query name/identifier
    /// </summary>
    [Required(ErrorMessage = "Query name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Query name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SQL query to execute
    /// </summary>
    [Required(ErrorMessage = "SQL query is required")]
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Query description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Query type (HealthCheck, Performance, Custom)
    /// </summary>
    public string Type { get; set; } = "HealthCheck";

    /// <summary>
    /// Expected result type (Scalar, Table, NonQuery)
    /// </summary>
    public string ResultType { get; set; } = "Scalar";

    /// <summary>
    /// Expected value for health check queries
    /// </summary>
    public object? ExpectedValue { get; set; }

    /// <summary>
    /// Comparison operator for expected value (Equals, GreaterThan, LessThan, etc.)
    /// </summary>
    public string ComparisonOperator { get; set; } = "Equals";

    /// <summary>
    /// Warning threshold for numeric results
    /// </summary>
    public double? WarningThreshold { get; set; }

    /// <summary>
    /// Critical threshold for numeric results
    /// </summary>
    public double? CriticalThreshold { get; set; }

    /// <summary>
    /// Query timeout in seconds (overrides connection default)
    /// </summary>
    [Range(1, 300, ErrorMessage = "Query timeout must be between 1 and 300 seconds")]
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Whether this query is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Query parameters
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}
