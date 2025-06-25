using MonitoringWorker.Models;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for collecting and recording metrics
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Records a heartbeat from the worker service
    /// </summary>
    void RecordHeartbeat();
    
    /// <summary>
    /// Records the start of a monitoring job
    /// </summary>
    void RecordJobStart();
    
    /// <summary>
    /// Records successful completion of a monitoring job
    /// </summary>
    void RecordJobSuccess();
    
    /// <summary>
    /// Records failure of a monitoring job
    /// </summary>
    void RecordJobFailure();
    
    /// <summary>
    /// Records cancellation of a monitoring job
    /// </summary>
    void RecordJobCancellation();
    
    /// <summary>
    /// Records the result of a specific monitoring check
    /// </summary>
    /// <param name="checkName">Name of the check</param>
    /// <param name="status">Status of the check</param>
    /// <param name="durationMs">Duration of the check in milliseconds</param>
    void RecordCheckResult(string checkName, MonitoringStatus status, long durationMs);
    
    /// <summary>
    /// Gets current metric values
    /// </summary>
    /// <returns>Dictionary of metric names and values</returns>
    Dictionary<string, long> GetMetrics();

    /// <summary>
    /// Gets current monitoring metrics
    /// </summary>
    /// <returns>Current monitoring metrics object</returns>
    MonitoringMetrics GetMonitoringMetrics();

    /// <summary>
    /// Gets current metrics (alias for GetMetrics)
    /// </summary>
    /// <returns>Current metrics object</returns>
    object GetCurrentMetrics();

    /// <summary>
    /// Gets monitoring history for the specified time period
    /// </summary>
    /// <param name="timeSpan">Time period to retrieve history for</param>
    /// <returns>Historical monitoring data</returns>
    object GetMonitoringHistory(TimeSpan timeSpan);

    /// <summary>
    /// Resets all metrics
    /// </summary>
    void Reset();
}
