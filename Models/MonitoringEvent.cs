namespace MonitoringWorker.Models;

/// <summary>
/// Represents a monitoring event that can be sent to clients via SignalR
/// </summary>
public class MonitoringEvent : MonitoringResult
{
    /// <summary>
    /// Unique identifier for this event
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The job ID that generated this event
    /// </summary>
    public string JobId { get; set; } = string.Empty;
    
    /// <summary>
    /// Event type (e.g., "WorkerStarted", "WorkerStopped", etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Event severity level
    /// </summary>
    public string? Severity { get; set; }

    /// <summary>
    /// Additional metadata for the event
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// Creates a MonitoringEvent from a MonitoringResult
    /// </summary>
    /// <param name="result">The monitoring result to convert</param>
    /// <param name="jobId">The job ID that generated the result</param>
    /// <returns>A new MonitoringEvent</returns>
    public static MonitoringEvent FromResult(MonitoringResult result, string jobId)
    {
        return new MonitoringEvent
        {
            Id = Guid.NewGuid().ToString(),
            JobId = jobId,
            CheckName = result.CheckName,
            Status = result.Status,
            Message = result.Message,
            Details = result.Details,
            Timestamp = result.Timestamp,
            DurationMs = result.DurationMs
        };
    }
}
