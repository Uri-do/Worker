using Microsoft.AspNetCore.SignalR;
using MonitoringWorker.Hubs;
using MonitoringWorker.Models;

namespace MonitoringWorker.Services;

/// <summary>
/// Implementation of the event notification service using SignalR
/// </summary>
public class EventNotificationService : IEventNotificationService
{
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly ILogger<EventNotificationService> _logger;

    public EventNotificationService(
        IHubContext<MonitoringHub> hubContext,
        ILogger<EventNotificationService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task NotifyAsync(MonitoringEvent monitoringEvent, CancellationToken cancellationToken = default)
    {
        if (monitoringEvent == null)
            throw new ArgumentNullException(nameof(monitoringEvent));

        try
        {
            await _hubContext.Clients.All.SendAsync("MonitoringUpdate", monitoringEvent, cancellationToken);
            
            _logger.LogDebug("Sent monitoring event {EventId} for check {CheckName} with status {Status} to all clients",
                monitoringEvent.Id, monitoringEvent.CheckName, monitoringEvent.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send monitoring event {EventId} to all clients", monitoringEvent.Id);
            throw;
        }
    }

    public async Task NotifyGroupAsync(string groupName, MonitoringEvent monitoringEvent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Group name cannot be null or empty", nameof(groupName));
        
        if (monitoringEvent == null)
            throw new ArgumentNullException(nameof(monitoringEvent));

        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync("MonitoringUpdate", monitoringEvent, cancellationToken);
            
            _logger.LogDebug("Sent monitoring event {EventId} for check {CheckName} with status {Status} to group {GroupName}",
                monitoringEvent.Id, monitoringEvent.CheckName, monitoringEvent.Status, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send monitoring event {EventId} to group {GroupName}", 
                monitoringEvent.Id, groupName);
            throw;
        }
    }

    public async Task NotifyClientAsync(string connectionId, MonitoringEvent monitoringEvent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("Connection ID cannot be null or empty", nameof(connectionId));
        
        if (monitoringEvent == null)
            throw new ArgumentNullException(nameof(monitoringEvent));

        try
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("MonitoringUpdate", monitoringEvent, cancellationToken);
            
            _logger.LogDebug("Sent monitoring event {EventId} for check {CheckName} with status {Status} to client {ConnectionId}",
                monitoringEvent.Id, monitoringEvent.CheckName, monitoringEvent.Status, connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send monitoring event {EventId} to client {ConnectionId}", 
                monitoringEvent.Id, connectionId);
            throw;
        }
    }
}
