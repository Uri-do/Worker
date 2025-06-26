using MonitoringWorker.Models;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace MonitoringWorker.Services;

/// <summary>
/// Implementation of the metrics service with OpenTelemetry integration and configurable features
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly IMonitoringConfigurationService _config;
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, (long Sum, long Count)> _durations = new();
    private readonly object _lockObject = new();
    private readonly DateTime _startTime = DateTime.UtcNow;
    private DateTime _lastHeartbeat = DateTime.UtcNow;

    // OpenTelemetry metrics (conditionally created based on configuration)
    private static readonly Meter Meter = new("MonitoringWorker.Metrics", "1.0.0");
    private readonly Counter<long>? _heartbeatCounter;
    private readonly Counter<long>? _jobStartedCounter;
    private readonly Counter<long>? _jobCompletedCounter;
    private readonly Counter<long>? _jobFailedCounter;
    private readonly Counter<long>? _jobCancelledCounter;
    private readonly Counter<long>? _checkCounter;
    private readonly Histogram<long>? _checkDurationHistogram;
    private readonly ObservableGauge<double>? _uptimeGauge;
    private readonly ObservableGauge<long>? _totalChecksGauge;

    public MetricsService(
        ILogger<MetricsService> logger,
        IMonitoringConfigurationService config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Only initialize metrics if monitoring is enabled
        if (!_config.IsMonitoringEnabled)
        {
            _logger.LogInformation("Monitoring is disabled - metrics collection will be minimal");
            InitializeMetrics();
            return;
        }

        _logger.LogInformation("Initializing metrics with configuration: Basic={Basic}, Detailed={Detailed}, System={System}",
            _config.IsBasicMetricsEnabled, _config.IsDetailedMetricsEnabled, _config.IsSystemMetricsEnabled);

        // Initialize basic metrics if enabled
        if (_config.IsBasicMetricsEnabled)
        {
            _heartbeatCounter = Meter.CreateCounter<long>(
                "monitoring_worker_heartbeat_total",
                description: "Total number of worker heartbeats");

            _jobStartedCounter = Meter.CreateCounter<long>(
                "monitoring_worker_jobs_started_total",
                description: "Total number of monitoring jobs started");

            _jobCompletedCounter = Meter.CreateCounter<long>(
                "monitoring_worker_jobs_completed_total",
                description: "Total number of monitoring jobs completed successfully");

            _jobFailedCounter = Meter.CreateCounter<long>(
                "monitoring_worker_jobs_failed_total",
                description: "Total number of monitoring jobs that failed");

            _jobCancelledCounter = Meter.CreateCounter<long>(
                "monitoring_worker_jobs_cancelled_total",
                description: "Total number of monitoring jobs that were cancelled");

            _checkCounter = Meter.CreateCounter<long>(
                "monitoring_worker_checks_total",
                description: "Total number of monitoring checks performed");

            _uptimeGauge = Meter.CreateObservableGauge<double>(
                "monitoring_worker_uptime_seconds",
                description: "Worker uptime in seconds",
                observeValue: () => (DateTime.UtcNow - _startTime).TotalSeconds);
        }

        // Initialize detailed metrics if enabled
        if (_config.IsDetailedMetricsEnabled)
        {
            _checkDurationHistogram = Meter.CreateHistogram<long>(
                "monitoring_worker_check_duration_milliseconds",
                unit: "ms",
                description: "Duration of monitoring checks in milliseconds");

            _totalChecksGauge = Meter.CreateObservableGauge<long>(
                "monitoring_worker_total_checks",
                description: "Total number of checks performed",
                observeValue: () => GetTotalChecks());
        }

        InitializeMetrics();
    }

    public void RecordHeartbeat()
    {
        if (!_config.IsMonitoringEnabled) return;

        if (_config.IsBasicMetricsEnabled)
        {
            IncrementCounter("worker.heartbeat");
            _heartbeatCounter?.Add(1);
        }

        _lastHeartbeat = DateTime.UtcNow;
        _logger.LogTrace("Recorded heartbeat");
    }

    public void RecordJobStart()
    {
        if (!_config.IsMonitoringEnabled) return;

        if (_config.IsBasicMetricsEnabled)
        {
            IncrementCounter("job.started");
            _jobStartedCounter?.Add(1);
        }

        _logger.LogDebug("Recorded job start");
    }

    public void RecordJobSuccess()
    {
        if (!_config.IsMonitoringEnabled) return;

        if (_config.IsBasicMetricsEnabled)
        {
            IncrementCounter("job.completed");
            _jobCompletedCounter?.Add(1);
        }

        _logger.LogDebug("Recorded job success");
    }

    public void RecordJobFailure()
    {
        if (!_config.IsMonitoringEnabled) return;

        if (_config.IsBasicMetricsEnabled)
        {
            IncrementCounter("job.failed");
            _jobFailedCounter?.Add(1);
        }

        _logger.LogDebug("Recorded job failure");
    }

    public void RecordJobCancellation()
    {
        if (!_config.IsMonitoringEnabled) return;

        if (_config.IsBasicMetricsEnabled)
        {
            IncrementCounter("job.cancelled");
            _jobCancelledCounter?.Add(1);
        }

        _logger.LogDebug("Recorded job cancellation");
    }

    public void RecordCheckResult(string checkName, MonitoringStatus status, long durationMs)
    {
        if (string.IsNullOrWhiteSpace(checkName))
            throw new ArgumentException("Check name cannot be null or empty", nameof(checkName));

        if (!_config.IsMonitoringEnabled) return;

        var sanitizedName = SanitizeMetricName(checkName);

        // Record basic metrics if enabled
        if (_config.IsBasicMetricsEnabled)
        {
            // Record status-specific counter
            IncrementCounter($"check.{sanitizedName}.{status.ToString().ToLowerInvariant()}");

            // Record overall check counter
            IncrementCounter($"check.{sanitizedName}.total");

            // Record OpenTelemetry check counter
            _checkCounter?.Add(1,
                new KeyValuePair<string, object?>("check_name", sanitizedName),
                new KeyValuePair<string, object?>("status", status.ToString().ToLowerInvariant()));
        }

        // Record detailed metrics if enabled
        if (_config.IsDetailedMetricsEnabled)
        {
            // Record duration
            RecordDuration($"check.{sanitizedName}.duration", durationMs);

            // Record OpenTelemetry duration histogram
            _checkDurationHistogram?.Record(durationMs,
                new KeyValuePair<string, object?>("check_name", sanitizedName),
                new KeyValuePair<string, object?>("status", status.ToString().ToLowerInvariant()));
        }

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
                TotalJobs = _counters.GetValueOrDefault("job.started", 0),
                SuccessfulJobs = _counters.GetValueOrDefault("job.completed", 0),
                FailedJobs = _counters.GetValueOrDefault("job.failed", 0),
                CancelledJobs = _counters.GetValueOrDefault("job.cancelled", 0),
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

    private long GetTotalChecks()
    {
        return _counters.Values.Where((_, index) => _counters.Keys.ElementAtOrDefault(index)?.StartsWith("check.") == true)
                              .Sum();
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
