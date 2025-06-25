using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MonitoringWorker.Models;
using MonitoringWorker.Services;

namespace MonitoringWorker.Hubs;

/// <summary>
/// SignalR hub for real-time monitoring notifications
/// </summary>
[Authorize]
public class MonitoringHub : Hub
{
    private readonly ILogger<MonitoringHub> _logger;
    private readonly IMetricsService _metricsService;

    public MonitoringHub(ILogger<MonitoringHub> logger, IMetricsService metricsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        var remoteIpAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var username = Context.User?.Identity?.Name ?? "Anonymous";
        var userId = Context.User?.FindFirst("user_id")?.Value ?? "Unknown";

        _logger.LogInformation("Client connected: {ConnectionId} from {RemoteIpAddress} using {UserAgent} (User: {Username})",
            connectionId, remoteIpAddress, userAgent, username);

        // Send connection confirmation to the client
        await Clients.Caller.SendAsync("Connected", new
        {
            ConnectionId = connectionId,
            Username = username,
            UserId = userId,
            Timestamp = DateTimeOffset.UtcNow,
            Message = "Successfully connected to monitoring hub",
            Roles = Context.User?.FindAll("role").Select(c => c.Value).ToList() ?? new List<string>(),
            Permissions = Context.User?.FindAll("permission").Select(c => c.Value).ToList() ?? new List<string>()
        });

        // Send current metrics to the newly connected client
        try
        {
            var metrics = _metricsService.GetMetrics();
            await Clients.Caller.SendAsync("MetricsUpdate", new
            {
                Timestamp = DateTimeOffset.UtcNow,
                Metrics = metrics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send initial metrics to client {ConnectionId}", connectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", connectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows clients to subscribe to a specific group for targeted notifications
    /// </summary>
    /// <param name="groupName">Name of the group to subscribe to</param>
    public async Task Subscribe(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            await Clients.Caller.SendAsync("Error", new
            {
                Message = "Group name cannot be empty",
                Timestamp = DateTimeOffset.UtcNow
            });
            return;
        }

        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation("Client {ConnectionId} subscribed to group {GroupName}", 
                Context.ConnectionId, groupName);

            await Clients.Caller.SendAsync("Subscribed", new
            {
                GroupName = groupName,
                Timestamp = DateTimeOffset.UtcNow,
                Message = $"Successfully subscribed to group '{groupName}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe client {ConnectionId} to group {GroupName}", 
                Context.ConnectionId, groupName);

            await Clients.Caller.SendAsync("Error", new
            {
                Message = $"Failed to subscribe to group '{groupName}'",
                Timestamp = DateTimeOffset.UtcNow,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Allows clients to unsubscribe from a specific group
    /// </summary>
    /// <param name="groupName">Name of the group to unsubscribe from</param>
    public async Task Unsubscribe(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            await Clients.Caller.SendAsync("Error", new
            {
                Message = "Group name cannot be empty",
                Timestamp = DateTimeOffset.UtcNow
            });
            return;
        }

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation("Client {ConnectionId} unsubscribed from group {GroupName}", 
                Context.ConnectionId, groupName);

            await Clients.Caller.SendAsync("Unsubscribed", new
            {
                GroupName = groupName,
                Timestamp = DateTimeOffset.UtcNow,
                Message = $"Successfully unsubscribed from group '{groupName}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe client {ConnectionId} from group {GroupName}", 
                Context.ConnectionId, groupName);

            await Clients.Caller.SendAsync("Error", new
            {
                Message = $"Failed to unsubscribe from group '{groupName}'",
                Timestamp = DateTimeOffset.UtcNow,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Allows clients to request current metrics
    /// </summary>
    [Authorize(Policy = "ViewMetrics")]
    public async Task GetMetrics()
    {
        try
        {
            // Check if user has permission to view metrics
            if (!Context.User?.FindAll("permission").Any(c => c.Value == Permissions.ViewMetrics) == true)
            {
                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Insufficient permissions to view metrics",
                    Timestamp = DateTimeOffset.UtcNow,
                    RequiredPermission = Permissions.ViewMetrics
                });
                return;
            }

            var metrics = _metricsService.GetMetrics();
            await Clients.Caller.SendAsync("MetricsUpdate", new
            {
                Timestamp = DateTimeOffset.UtcNow,
                Metrics = metrics
            });

            _logger.LogDebug("Sent metrics to client {ConnectionId} (User: {Username})",
                Context.ConnectionId, Context.User?.Identity?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send metrics to client {ConnectionId}", Context.ConnectionId);

            await Clients.Caller.SendAsync("Error", new
            {
                Message = "Failed to retrieve metrics",
                Timestamp = DateTimeOffset.UtcNow,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Allows admin users to trigger a manual monitoring check
    /// </summary>
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Operator)]
    public async Task TriggerManualCheck()
    {
        try
        {
            // Check if user has permission to manage monitoring
            if (!Context.User?.FindAll("permission").Any(c => c.Value == Permissions.ManageMonitoring) == true)
            {
                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Insufficient permissions to trigger manual checks",
                    Timestamp = DateTimeOffset.UtcNow,
                    RequiredPermission = Permissions.ManageMonitoring
                });
                return;
            }

            var username = Context.User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("Manual monitoring check triggered by user {Username} from connection {ConnectionId}",
                username, Context.ConnectionId);

            await Clients.Caller.SendAsync("ManualCheckTriggered", new
            {
                Message = "Manual monitoring check has been triggered",
                Timestamp = DateTimeOffset.UtcNow,
                TriggeredBy = username
            });

            // In a real implementation, this would trigger the monitoring job
            // For now, we'll just send a confirmation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger manual check for client {ConnectionId}", Context.ConnectionId);

            await Clients.Caller.SendAsync("Error", new
            {
                Message = "Failed to trigger manual check",
                Timestamp = DateTimeOffset.UtcNow,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Allows clients to get their user information
    /// </summary>
    public async Task GetUserInfo()
    {
        try
        {
            var username = Context.User?.Identity?.Name ?? "Anonymous";
            var userId = Context.User?.FindFirst("user_id")?.Value ?? "Unknown";
            var roles = Context.User?.FindAll("role").Select(c => c.Value).ToList() ?? new List<string>();
            var permissions = Context.User?.FindAll("permission").Select(c => c.Value).ToList() ?? new List<string>();

            await Clients.Caller.SendAsync("UserInfo", new
            {
                Username = username,
                UserId = userId,
                Roles = roles,
                Permissions = permissions,
                Timestamp = DateTimeOffset.UtcNow
            });

            _logger.LogDebug("Sent user info to client {ConnectionId} (User: {Username})",
                Context.ConnectionId, username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send user info to client {ConnectionId}", Context.ConnectionId);

            await Clients.Caller.SendAsync("Error", new
            {
                Message = "Failed to retrieve user information",
                Timestamp = DateTimeOffset.UtcNow,
                Error = ex.Message
            });
        }
    }
}
