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
    /// An error occurred while checking the endpoint
    /// </summary>
    Error = 3
}
