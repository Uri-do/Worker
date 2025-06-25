namespace MonitoringWorker.Models;

/// <summary>
/// Worker status information
/// </summary>
public class WorkerStatus
{
    public bool IsRunning { get; set; }
    public bool IsStarted { get; set; }
    public bool IsShutdown { get; set; }
    public DateTime? StartTime { get; set; }
    public List<JobInfo> Jobs { get; set; } = new();
    public MonitoringMetrics? Metrics { get; set; }
}

/// <summary>
/// Job information
/// </summary>
public class JobInfo
{
    public string JobName { get; set; } = string.Empty;
    public string JobGroup { get; set; } = string.Empty;
    public string TriggerName { get; set; } = string.Empty;
    public string TriggerGroup { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime? NextFireTime { get; set; }
    public DateTime? PreviousFireTime { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Operation result
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Monitoring metrics
/// </summary>
public class MonitoringMetrics
{
    public long TotalChecks { get; set; }
    public long SuccessfulChecks { get; set; }
    public long FailedChecks { get; set; }
    public long ErrorChecks { get; set; }
    public double SuccessRate { get; set; }
    public long TotalJobs { get; set; }
    public long SuccessfulJobs { get; set; }
    public long FailedJobs { get; set; }
    public long CancelledJobs { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public TimeSpan Uptime { get; set; }
}
