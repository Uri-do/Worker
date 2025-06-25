namespace MonitoringWorker.Models;

/// <summary>
/// Represents the result of a monitoring check
/// </summary>
public class MonitoringResult
{
    /// <summary>
    /// The name of the check that was performed
    /// </summary>
    public string CheckName { get; set; } = string.Empty;
    
    /// <summary>
    /// The status result of the check
    /// </summary>
    public MonitoringStatus Status { get; set; }
    
    /// <summary>
    /// A human-readable message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional details about the check result
    /// </summary>
    public object? Details { get; set; }
    
    /// <summary>
    /// The timestamp when the check was performed
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// The duration of the check in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
}
