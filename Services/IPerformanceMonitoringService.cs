using System.Diagnostics;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for monitoring application performance
/// </summary>
public interface IPerformanceMonitoringService
{
    /// <summary>
    /// Starts a performance measurement
    /// </summary>
    /// <param name="operationName">Name of the operation being measured</param>
    /// <returns>Performance measurement context</returns>
    IPerformanceMeasurement StartMeasurement(string operationName);

    /// <summary>
    /// Records a performance metric
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="durationMs">Duration in milliseconds</param>
    /// <param name="additionalData">Additional performance data</param>
    void RecordMetric(string operationName, long durationMs, Dictionary<string, object>? additionalData = null);

    /// <summary>
    /// Gets performance statistics for an operation
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="timeRange">Time range for statistics</param>
    /// <returns>Performance statistics</returns>
    Task<PerformanceStatistics> GetStatisticsAsync(string operationName, TimeSpan? timeRange = null);

    /// <summary>
    /// Gets overall performance summary
    /// </summary>
    /// <param name="timeRange">Time range for summary</param>
    /// <returns>Performance summary</returns>
    Task<PerformanceSummary> GetSummaryAsync(TimeSpan? timeRange = null);

    /// <summary>
    /// Gets slow operations report
    /// </summary>
    /// <param name="thresholdMs">Threshold in milliseconds for slow operations</param>
    /// <param name="timeRange">Time range for report</param>
    /// <returns>Slow operations report</returns>
    Task<List<SlowOperation>> GetSlowOperationsAsync(long thresholdMs = 1000, TimeSpan? timeRange = null);
}

/// <summary>
/// Performance measurement context
/// </summary>
public interface IPerformanceMeasurement : IDisposable
{
    /// <summary>
    /// Operation name
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Start time
    /// </summary>
    DateTime StartTime { get; }

    /// <summary>
    /// Elapsed time
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Adds additional data to the measurement
    /// </summary>
    /// <param name="key">Data key</param>
    /// <param name="value">Data value</param>
    void AddData(string key, object value);

    /// <summary>
    /// Marks the operation as successful
    /// </summary>
    void MarkSuccess();

    /// <summary>
    /// Marks the operation as failed
    /// </summary>
    /// <param name="error">Error information</param>
    void MarkFailure(string error);
}

/// <summary>
/// Performance statistics for an operation
/// </summary>
public class PerformanceStatistics
{
    /// <summary>
    /// Operation name
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of executions
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Number of successful executions
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;

    /// <summary>
    /// Average duration in milliseconds
    /// </summary>
    public double AverageDurationMs { get; set; }

    /// <summary>
    /// Minimum duration in milliseconds
    /// </summary>
    public long MinDurationMs { get; set; }

    /// <summary>
    /// Maximum duration in milliseconds
    /// </summary>
    public long MaxDurationMs { get; set; }

    /// <summary>
    /// 50th percentile duration
    /// </summary>
    public long P50DurationMs { get; set; }

    /// <summary>
    /// 95th percentile duration
    /// </summary>
    public long P95DurationMs { get; set; }

    /// <summary>
    /// 99th percentile duration
    /// </summary>
    public long P99DurationMs { get; set; }

    /// <summary>
    /// Time range for these statistics
    /// </summary>
    public TimeSpan TimeRange { get; set; }

    /// <summary>
    /// Statistics generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Overall performance summary
/// </summary>
public class PerformanceSummary
{
    /// <summary>
    /// Total operations tracked
    /// </summary>
    public int TotalOperations { get; set; }

    /// <summary>
    /// Total executions across all operations
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Overall success rate
    /// </summary>
    public double OverallSuccessRate { get; set; }

    /// <summary>
    /// Average response time across all operations
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Top performing operations
    /// </summary>
    public List<PerformanceStatistics> TopPerformingOperations { get; set; } = new();

    /// <summary>
    /// Worst performing operations
    /// </summary>
    public List<PerformanceStatistics> WorstPerformingOperations { get; set; } = new();

    /// <summary>
    /// Most frequently executed operations
    /// </summary>
    public List<PerformanceStatistics> MostFrequentOperations { get; set; } = new();

    /// <summary>
    /// Time range for this summary
    /// </summary>
    public TimeSpan TimeRange { get; set; }

    /// <summary>
    /// Summary generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Slow operation information
/// </summary>
public class SlowOperation
{
    /// <summary>
    /// Operation name
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Execution timestamp
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional operation data
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
