namespace MonitoringWorker.Models;

/// <summary>
/// Represents the status of a monitoring check
/// </summary>
public enum MonitoringStatus
{
    /// <summary>
    /// Status is unknown or not yet determined
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// The monitored endpoint is healthy and responding correctly
    /// </summary>
    Healthy = 1,
    
    /// <summary>
    /// The monitored endpoint is unhealthy but reachable
    /// </summary>
    Unhealthy = 2,

    /// <summary>
    /// The monitored endpoint has warning conditions
    /// </summary>
    Warning = 3,

    /// <summary>
    /// The monitored endpoint is in a critical state
    /// </summary>
    Critical = 4,

    /// <summary>
    /// An error occurred while checking the endpoint
    /// </summary>
    Error = 5
}
