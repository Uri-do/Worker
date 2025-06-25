using MonitoringWorker.Models;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for sending real-time notifications about monitoring events
/// </summary>
public interface IEventNotificationService
{
    /// <summary>
    /// Sends a monitoring event notification to all connected clients
    /// </summary>
    /// <param name="monitoringEvent">The monitoring event to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyAsync(MonitoringEvent monitoringEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a monitoring event notification to a specific group
    /// </summary>
    /// <param name="groupName">The group to send the notification to</param>
    /// <param name="monitoringEvent">The monitoring event to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyGroupAsync(string groupName, MonitoringEvent monitoringEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a monitoring event notification to a specific client
    /// </summary>
    /// <param name="connectionId">The connection ID to send the notification to</param>
    /// <param name="monitoringEvent">The monitoring event to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyClientAsync(string connectionId, MonitoringEvent monitoringEvent, CancellationToken cancellationToken = default);
}
