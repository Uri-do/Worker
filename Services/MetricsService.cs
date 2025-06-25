using MonitoringWorker.Models;
using System.Collections.Concurrent;

namespace MonitoringWorker.Services;

/// <summary>
/// Implementation of the metrics service
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, (long Sum, long Count)> _durations = new();
    private readonly object _lockObject = new();
    private readonly DateTime _startTime = DateTime.UtcNow;
    private DateTime _lastHeartbeat = DateTime.UtcNow;

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeMetrics();
    }

    public void RecordHeartbeat()
    {
        IncrementCounter("worker.heartbeat");
        _lastHeartbeat = DateTime.UtcNow;
        _logger.LogTrace("Recorded heartbeat");
    }

    public void RecordJobStart()
    {
        IncrementCounter("job.started");
        _logger.LogDebug("Recorded job start");
    }

    public void RecordJobSuccess()
    {
        IncrementCounter("job.completed");
        _logger.LogDebug("Recorded job success");
    }

    public void RecordJobFailure()
    {
        IncrementCounter("job.failed");
        _logger.LogDebug("Recorded job failure");
    }

    public void RecordJobCancellation()
    {
        IncrementCounter("job.cancelled");
        _logger.LogDebug("Recorded job cancellation");
    }

    public void RecordCheckResult(string checkName, MonitoringStatus status, long durationMs)
    {
        if (string.IsNullOrWhiteSpace(checkName))
            throw new ArgumentException("Check name cannot be null or empty", nameof(checkName));

        var sanitizedName = SanitizeMetricName(checkName);
        
        // Record status-specific counter
        IncrementCounter($"check.{sanitizedName}.{status.ToString().ToLowerInvariant()}");
        
        // Record overall check counter
        IncrementCounter($"check.{sanitizedName}.total");
        
        // Record duration
        RecordDuration($"check.{sanitizedName}.duration", durationMs);
        
        _logger.LogTrace("Recorded check result for {CheckName}: {Status} in {Duration}ms", 
            checkName, status, durationMs);
    }

    public Dictionary<string, long> GetMetrics()
    {
        var metrics = new Dictionary<string, long>();

        // Add counters
        foreach (var counter in _counters)
        {
            metrics[counter.Key] = counter.Value;
        }

        // Add duration averages
        foreach (var duration in _durations)
        {
            var average = duration.Value.Count > 0 ? duration.Value.Sum / duration.Value.Count : 0;
            metrics[$"{duration.Key}.avg"] = average;
            metrics[$"{duration.Key}.count"] = duration.Value.Count;
        }

        return metrics;
    }

    public MonitoringMetrics GetMonitoringMetrics()
    {
        lock (_lockObject)
        {
            var totalChecks = _counters.GetValueOrDefault("checks.healthy", 0) +
                             _counters.GetValueOrDefault("checks.unhealthy", 0) +
                             _counters.GetValueOrDefault("checks.error", 0);

            var successfulChecks = _counters.GetValueOrDefault("checks.healthy", 0);
            var failedChecks = _counters.GetValueOrDefault("checks.unhealthy", 0);
            var errorChecks = _counters.GetValueOrDefault("checks.error", 0);

            var successRate = totalChecks > 0 ? (double)successfulChecks / totalChecks * 100 : 0;

            return new MonitoringMetrics
            {
                TotalChecks = totalChecks,
                SuccessfulChecks = successfulChecks,
                FailedChecks = failedChecks,
                ErrorChecks = errorChecks,
                SuccessRate = Math.Round(successRate, 2),
                TotalJobs = _counters.GetValueOrDefault("jobs.started", 0),
                SuccessfulJobs = _counters.GetValueOrDefault("jobs.success", 0),
                FailedJobs = _counters.GetValueOrDefault("jobs.failure", 0),
                CancelledJobs = _counters.GetValueOrDefault("jobs.cancelled", 0),
                LastHeartbeat = _lastHeartbeat,
                Uptime = DateTime.UtcNow - _startTime
            };
        }
    }

    public void Reset()
    {
        lock (_lockObject)
        {
            _counters.Clear();
            _durations.Clear();
            InitializeMetrics();
            _logger.LogInformation("Metrics have been reset");
        }
    }

    private void IncrementCounter(string name)
    {
        var count = _counters.AddOrUpdate(name, 1, (_, value) => value + 1);
        _logger.LogTrace("Metric {MetricName}: {Count}", name, count);
    }

    private void RecordDuration(string name, long durationMs)
    {
        _durations.AddOrUpdate(name, 
            (durationMs, 1), 
            (_, existing) => (existing.Sum + durationMs, existing.Count + 1));
    }

    private void InitializeMetrics()
    {
        // Initialize common metrics to ensure they exist
        _counters.TryAdd("worker.heartbeat", 0);
        _counters.TryAdd("job.started", 0);
        _counters.TryAdd("job.completed", 0);
        _counters.TryAdd("job.failed", 0);
        _counters.TryAdd("job.cancelled", 0);
    }

    private static string SanitizeMetricName(string name)
    {
        // Replace invalid characters with underscores
        return string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            .ToLowerInvariant();
    }

    /// <summary>
    /// Gets current metrics (alias for GetMetrics)
    /// </summary>
    public object GetCurrentMetrics()
    {
        var metrics = GetMetrics();
        var durations = _durations.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Count > 0 ? kvp.Value.Sum / kvp.Value.Count : 0);

        return new
        {
            Counters = metrics,
            AverageDurations = durations,
            Timestamp = DateTime.UtcNow,
            Summary = new
            {
                TotalJobs = metrics.GetValueOrDefault("job.started", 0),
                CompletedJobs = metrics.GetValueOrDefault("job.completed", 0),
                FailedJobs = metrics.GetValueOrDefault("job.failed", 0),
                CancelledJobs = metrics.GetValueOrDefault("job.cancelled", 0),
                Heartbeats = metrics.GetValueOrDefault("worker.heartbeat", 0)
            }
        };
    }

    /// <summary>
    /// Gets monitoring history for the specified time period
    /// </summary>
    public object GetMonitoringHistory(TimeSpan timeSpan)
    {
        // For now, return current metrics as history
        // In a real implementation, you would store historical data
        var currentMetrics = GetCurrentMetrics();

        return new
        {
            TimeSpan = timeSpan,
            StartTime = DateTime.UtcNow.Subtract(timeSpan),
            EndTime = DateTime.UtcNow,
            CurrentMetrics = currentMetrics,
            Note = "Historical data storage not implemented - showing current metrics"
        };
    }
}
