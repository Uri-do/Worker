using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using MonitoringWorker.Models;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace MonitoringWorker.Services;

/// <summary>
/// Implementation of database monitoring service
/// </summary>
public class DatabaseMonitoringService : IDatabaseMonitoringService
{
    private readonly ILogger<DatabaseMonitoringService> _logger;
    private readonly DatabaseMonitoringOptions _options;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly ConcurrentDictionary<string, DatabaseMonitoringStats> _statsCache = new();

    public DatabaseMonitoringService(
        ILogger<DatabaseMonitoringService> logger,
        IOptions<DatabaseMonitoringOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionSemaphore = new SemaphoreSlim(_options.MaxConcurrentConnections, _options.MaxConcurrentConnections);
    }

    public async Task<IEnumerable<DatabaseConnectionHealth>> TestConnectionsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Database monitoring is disabled");
            return Enumerable.Empty<DatabaseConnectionHealth>();
        }

        _logger.LogInformation("Testing {ConnectionCount} database connections", _options.Connections.Count);

        var tasks = _options.Connections
            .Where(c => c.Enabled)
            .Select(connection => TestConnectionInternalAsync(connection, cancellationToken));

        var results = await Task.WhenAll(tasks);

        _logger.LogInformation("Database connection tests completed. Results: {HealthyCount} healthy, {UnhealthyCount} unhealthy, {ErrorCount} errors",
            results.Count(r => r.Status == MonitoringStatus.Healthy),
            results.Count(r => r.Status == MonitoringStatus.Unhealthy),
            results.Count(r => r.Status == MonitoringStatus.Error));

        return results;
    }

    public async Task<DatabaseConnectionHealth?> TestConnectionAsync(string connectionName, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Database monitoring is disabled");
            return null;
        }

        var connection = _options.Connections.FirstOrDefault(c => 
            string.Equals(c.Name, connectionName, StringComparison.OrdinalIgnoreCase) && c.Enabled);

        if (connection == null)
        {
            _logger.LogWarning("Database connection {ConnectionName} not found or disabled", connectionName);
            return null;
        }

        return await TestConnectionInternalAsync(connection, cancellationToken);
    }

    public async Task<IEnumerable<DatabaseMonitoringResult>> ExecuteMonitoringQueriesAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Database monitoring is disabled");
            return Enumerable.Empty<DatabaseMonitoringResult>();
        }

        var results = new List<DatabaseMonitoringResult>();

        foreach (var connection in _options.Connections.Where(c => c.Enabled))
        {
            var connectionResults = await ExecuteConnectionQueriesAsync(connection.Name, cancellationToken);
            results.AddRange(connectionResults);
        }

        _logger.LogInformation("Executed {QueryCount} database monitoring queries", results.Count);
        return results;
    }

    public async Task<IEnumerable<DatabaseMonitoringResult>> ExecuteConnectionQueriesAsync(string connectionName, CancellationToken cancellationToken = default)
    {
        var connection = _options.Connections.FirstOrDefault(c => 
            string.Equals(c.Name, connectionName, StringComparison.OrdinalIgnoreCase) && c.Enabled);

        if (connection == null)
        {
            _logger.LogWarning("Database connection {ConnectionName} not found or disabled", connectionName);
            return Enumerable.Empty<DatabaseMonitoringResult>();
        }

        var results = new List<DatabaseMonitoringResult>();

        // Execute queries specified for this connection
        foreach (var queryName in connection.QueryNames)
        {
            var query = _options.Queries.FirstOrDefault(q => 
                string.Equals(q.Name, queryName, StringComparison.OrdinalIgnoreCase) && q.Enabled);

            if (query != null)
            {
                var result = await ExecuteQueryInternalAsync(connection, query, cancellationToken);
                if (result != null)
                {
                    results.Add(result);
                }
            }
        }

        // Execute global queries for all connections
        foreach (var query in _options.Queries.Where(q => q.Enabled && !connection.QueryNames.Contains(q.Name)))
        {
            var result = await ExecuteQueryInternalAsync(connection, query, cancellationToken);
            if (result != null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    public async Task<DatabaseMonitoringResult?> ExecuteQueryAsync(string connectionName, string queryName, CancellationToken cancellationToken = default)
    {
        var connection = _options.Connections.FirstOrDefault(c => 
            string.Equals(c.Name, connectionName, StringComparison.OrdinalIgnoreCase) && c.Enabled);

        if (connection == null)
        {
            _logger.LogWarning("Database connection {ConnectionName} not found or disabled", connectionName);
            return null;
        }

        var query = _options.Queries.FirstOrDefault(q => 
            string.Equals(q.Name, queryName, StringComparison.OrdinalIgnoreCase) && q.Enabled);

        if (query == null)
        {
            _logger.LogWarning("Database query {QueryName} not found or disabled", queryName);
            return null;
        }

        return await ExecuteQueryInternalAsync(connection, query, cancellationToken);
    }

    public async Task<DatabaseMonitoringResult> ExecuteCustomQueryAsync(
        string connectionName, 
        string sql, 
        Dictionary<string, object>? parameters = null,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        var connection = _options.Connections.FirstOrDefault(c => 
            string.Equals(c.Name, connectionName, StringComparison.OrdinalIgnoreCase) && c.Enabled);

        if (connection == null)
        {
            return new DatabaseMonitoringResult
            {
                ConnectionName = connectionName,
                QueryName = "CustomQuery",
                Status = MonitoringStatus.Error,
                Message = "Connection not found or disabled",
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        var queryConfig = new DatabaseQueryConfig
        {
            Name = "CustomQuery",
            Sql = sql,
            Parameters = parameters,
            TimeoutSeconds = timeoutSeconds,
            ResultType = "Scalar"
        };

        return await ExecuteQueryInternalAsync(connection, queryConfig, cancellationToken);
    }

    public Task<DatabaseMonitoringStats> GetStatisticsAsync()
    {
        // This would typically be implemented with actual statistics tracking
        // For now, return basic stats based on configuration
        var stats = new DatabaseMonitoringStats
        {
            TotalConnections = _options.Connections.Count(c => c.Enabled),
            TotalQueries = _options.Queries.Count(q => q.Enabled),
            LastRunTimestamp = DateTimeOffset.UtcNow
        };

        // Group by environment
        foreach (var connection in _options.Connections.Where(c => c.Enabled))
        {
            if (!stats.ByEnvironment.ContainsKey(connection.Environment))
            {
                stats.ByEnvironment[connection.Environment] = new EnvironmentStats
                {
                    Environment = connection.Environment
                };
            }
            stats.ByEnvironment[connection.Environment].ConnectionCount++;
        }

        return Task.FromResult(stats);
    }

    public Task<IEnumerable<string>> GetConnectionNamesAsync()
    {
        return Task.FromResult(_options.Connections
            .Where(c => c.Enabled)
            .Select(c => c.Name)
            .AsEnumerable());
    }

    public Task<IEnumerable<(string Name, string Description)>> GetQueryNamesAsync()
    {
        return Task.FromResult(_options.Queries
            .Where(q => q.Enabled)
            .Select(q => (q.Name, q.Description))
            .AsEnumerable());
    }

    private async Task<DatabaseConnectionHealth> TestConnectionInternalAsync(
        DatabaseConnectionConfig connectionConfig, 
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Testing database connection {ConnectionName}", connectionConfig.Name);

            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionConfig.ConnectionString);
            var timeout = connectionConfig.ConnectionTimeoutSeconds ?? _options.DefaultConnectionTimeoutSeconds;
            connectionStringBuilder.ConnectTimeout = timeout;

            using var connection = new SqlConnection(connectionStringBuilder.ConnectionString);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeout));

            await connection.OpenAsync(cts.Token);
            stopwatch.Stop();

            var result = new DatabaseConnectionHealth
            {
                ConnectionName = connectionConfig.Name,
                Status = MonitoringStatus.Healthy,
                Message = "Connection successful",
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ServerVersion = connection.ServerVersion,
                DatabaseName = connection.Database,
                Details = new
                {
                    Provider = connectionConfig.Provider,
                    Environment = connectionConfig.Environment,
                    Tags = connectionConfig.Tags,
                    ConnectionTimeout = timeout
                }
            };

            _logger.LogDebug("Database connection {ConnectionName} test successful in {Duration}ms", 
                connectionConfig.Name, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Database connection {ConnectionName} test was cancelled", connectionConfig.Name);

            return new DatabaseConnectionHealth
            {
                ConnectionName = connectionConfig.Name,
                Status = MonitoringStatus.Error,
                Message = "Connection test was cancelled",
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error testing database connection {ConnectionName}", connectionConfig.Name);

            return new DatabaseConnectionHealth
            {
                ConnectionName = connectionConfig.Name,
                Status = MonitoringStatus.Error,
                Message = $"Connection failed: {ex.Message}",
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new
                {
                    Error = ex.Message,
                    Type = ex.GetType().Name,
                    Provider = connectionConfig.Provider,
                    Environment = connectionConfig.Environment
                }
            };
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private async Task<DatabaseMonitoringResult> ExecuteQueryInternalAsync(
        DatabaseConnectionConfig connectionConfig,
        DatabaseQueryConfig queryConfig,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Executing query {QueryName} on connection {ConnectionName}",
                queryConfig.Name, connectionConfig.Name);

            using var connection = new SqlConnection(connectionConfig.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(queryConfig.Sql, connection);

            var timeout = queryConfig.TimeoutSeconds ??
                         connectionConfig.CommandTimeoutSeconds ??
                         _options.DefaultCommandTimeoutSeconds;
            command.CommandTimeout = timeout;

            // Add parameters if specified
            if (queryConfig.Parameters != null)
            {
                foreach (var param in queryConfig.Parameters)
                {
                    command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                }
            }

            object? result = null;
            switch (queryConfig.ResultType.ToLowerInvariant())
            {
                case "scalar":
                    result = await command.ExecuteScalarAsync(cancellationToken);
                    break;
                case "nonquery":
                    result = await command.ExecuteNonQueryAsync(cancellationToken);
                    break;
                case "table":
                    var dataTable = new DataTable();
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        dataTable.Load(reader);
                    }
                    result = dataTable;
                    break;
                default:
                    result = await command.ExecuteScalarAsync(cancellationToken);
                    break;
            }

            stopwatch.Stop();

            // Evaluate result against expected values and thresholds
            var status = EvaluateQueryResult(result, queryConfig);
            var message = GenerateResultMessage(result, queryConfig, status);

            var monitoringResult = new DatabaseMonitoringResult
            {
                ConnectionName = connectionConfig.Name,
                QueryName = queryConfig.Name,
                Status = status,
                Message = message,
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ResultValue = result,
                Provider = connectionConfig.Provider,
                Environment = connectionConfig.Environment,
                Tags = connectionConfig.Tags.ToList(),
                Details = new
                {
                    QueryType = queryConfig.Type,
                    ResultType = queryConfig.ResultType,
                    ExpectedValue = queryConfig.ExpectedValue,
                    ComparisonOperator = queryConfig.ComparisonOperator,
                    WarningThreshold = queryConfig.WarningThreshold,
                    CriticalThreshold = queryConfig.CriticalThreshold,
                    Timeout = timeout,
                    ParameterCount = queryConfig.Parameters?.Count ?? 0
                }
            };

            _logger.LogDebug("Query {QueryName} on {ConnectionName} completed: {Status} in {Duration}ms",
                queryConfig.Name, connectionConfig.Name, status, stopwatch.ElapsedMilliseconds);

            return monitoringResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Query {QueryName} on {ConnectionName} was cancelled",
                queryConfig.Name, connectionConfig.Name);

            return new DatabaseMonitoringResult
            {
                ConnectionName = connectionConfig.Name,
                QueryName = queryConfig.Name,
                Status = MonitoringStatus.Error,
                Message = "Query execution was cancelled",
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Provider = connectionConfig.Provider,
                Environment = connectionConfig.Environment,
                Tags = connectionConfig.Tags.ToList()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing query {QueryName} on connection {ConnectionName}",
                queryConfig.Name, connectionConfig.Name);

            return new DatabaseMonitoringResult
            {
                ConnectionName = connectionConfig.Name,
                QueryName = queryConfig.Name,
                Status = MonitoringStatus.Error,
                Message = $"Query execution failed: {ex.Message}",
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Provider = connectionConfig.Provider,
                Environment = connectionConfig.Environment,
                Tags = connectionConfig.Tags.ToList(),
                Details = new
                {
                    Error = ex.Message,
                    Type = ex.GetType().Name,
                    Sql = queryConfig.Sql
                }
            };
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private static MonitoringStatus EvaluateQueryResult(object? result, DatabaseQueryConfig queryConfig)
    {
        if (result == null || result == DBNull.Value)
        {
            return queryConfig.ExpectedValue == null ? MonitoringStatus.Healthy : MonitoringStatus.Unhealthy;
        }

        // Check against expected value
        if (queryConfig.ExpectedValue != null)
        {
            var comparison = CompareValues(result, queryConfig.ExpectedValue, queryConfig.ComparisonOperator);
            if (!comparison)
            {
                return MonitoringStatus.Unhealthy;
            }
        }

        // Check against thresholds for numeric values
        if (result is IConvertible convertible)
        {
            try
            {
                var numericValue = Convert.ToDouble(convertible);

                if (queryConfig.CriticalThreshold.HasValue)
                {
                    if (numericValue >= queryConfig.CriticalThreshold.Value)
                    {
                        return MonitoringStatus.Error;
                    }
                }

                if (queryConfig.WarningThreshold.HasValue)
                {
                    if (numericValue >= queryConfig.WarningThreshold.Value)
                    {
                        return MonitoringStatus.Unhealthy;
                    }
                }
            }
            catch
            {
                // If conversion fails, ignore threshold checks
            }
        }

        return MonitoringStatus.Healthy;
    }

    private static bool CompareValues(object actual, object expected, string comparisonOperator)
    {
        try
        {
            return comparisonOperator.ToLowerInvariant() switch
            {
                "equals" or "eq" => actual.Equals(expected),
                "notequals" or "ne" => !actual.Equals(expected),
                "greaterthan" or "gt" => Convert.ToDouble(actual) > Convert.ToDouble(expected),
                "greaterthanorequal" or "gte" => Convert.ToDouble(actual) >= Convert.ToDouble(expected),
                "lessthan" or "lt" => Convert.ToDouble(actual) < Convert.ToDouble(expected),
                "lessthanorequal" or "lte" => Convert.ToDouble(actual) <= Convert.ToDouble(expected),
                _ => actual.Equals(expected)
            };
        }
        catch
        {
            return actual.Equals(expected);
        }
    }

    private static string GenerateResultMessage(object? result, DatabaseQueryConfig queryConfig, MonitoringStatus status)
    {
        var resultStr = result?.ToString() ?? "null";

        return status switch
        {
            MonitoringStatus.Healthy => $"Query successful: {resultStr}",
            MonitoringStatus.Unhealthy => $"Query result outside expected range: {resultStr}",
            MonitoringStatus.Error => $"Query failed or critical threshold exceeded: {resultStr}",
            _ => $"Query result: {resultStr}"
        };
    }
}
