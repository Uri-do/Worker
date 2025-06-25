using MonitoringWorker.Models;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for monitoring database connections and executing health check queries
/// </summary>
public interface IDatabaseMonitoringService
{
    /// <summary>
    /// Tests all configured database connections
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of connection health results</returns>
    Task<IEnumerable<DatabaseConnectionHealth>> TestConnectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a specific database connection
    /// </summary>
    /// <param name="connectionName">Name of the connection to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connection health result</returns>
    Task<DatabaseConnectionHealth?> TestConnectionAsync(string connectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all configured monitoring queries
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of query results</returns>
    Task<IEnumerable<DatabaseMonitoringResult>> ExecuteMonitoringQueriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes monitoring queries for a specific connection
    /// </summary>
    /// <param name="connectionName">Name of the connection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of query results for the connection</returns>
    Task<IEnumerable<DatabaseMonitoringResult>> ExecuteConnectionQueriesAsync(string connectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a specific monitoring query
    /// </summary>
    /// <param name="connectionName">Name of the connection</param>
    /// <param name="queryName">Name of the query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Query result</returns>
    Task<DatabaseMonitoringResult?> ExecuteQueryAsync(string connectionName, string queryName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets monitoring statistics
    /// </summary>
    /// <returns>Database monitoring statistics</returns>
    Task<DatabaseMonitoringStats> GetStatisticsAsync();

    /// <summary>
    /// Gets list of configured connections
    /// </summary>
    /// <returns>List of connection names and their status</returns>
    Task<IEnumerable<string>> GetConnectionNamesAsync();

    /// <summary>
    /// Gets list of configured queries
    /// </summary>
    /// <returns>List of query names and their descriptions</returns>
    Task<IEnumerable<(string Name, string Description)>> GetQueryNamesAsync();

    /// <summary>
    /// Executes a custom SQL query for monitoring
    /// </summary>
    /// <param name="connectionName">Name of the connection</param>
    /// <param name="sql">SQL query to execute</param>
    /// <param name="parameters">Query parameters</param>
    /// <param name="timeoutSeconds">Query timeout in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Query execution result</returns>
    Task<DatabaseMonitoringResult> ExecuteCustomQueryAsync(
        string connectionName,
        string sql,
        Dictionary<string, object>? parameters = null,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);
}
