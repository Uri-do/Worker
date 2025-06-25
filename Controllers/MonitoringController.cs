using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringWorker.Models;
using MonitoringWorker.Services;

namespace MonitoringWorker.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;
    private readonly IMetricsService _metricsService;
    private readonly IEventNotificationService _eventNotificationService;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        IMonitoringService monitoringService,
        IMetricsService metricsService,
        IEventNotificationService eventNotificationService,
        ILogger<MonitoringController> logger)
    {
        _monitoringService = monitoringService;
        _metricsService = metricsService;
        _eventNotificationService = eventNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Trigger manual monitoring check for all endpoints
    /// </summary>
    [HttpPost("check")]
    [Authorize(Policy = "ManageMonitoring")]
    public async Task<ActionResult<List<MonitoringResult>>> TriggerManualCheck(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manual monitoring check triggered by user {UserId}", User.Identity?.Name);
            
            var results = await _monitoringService.CheckAllEndpointsAsync(cancellationToken);
            
            // Send notifications for the results
            foreach (var result in results)
            {
                var monitoringEvent = MonitoringEvent.FromResult(result, "manual-check");
                await _eventNotificationService.NotifyAsync(monitoringEvent);
            }

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual monitoring check");
            return StatusCode(500, new { message = "Error during monitoring check", error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger manual monitoring check for a specific endpoint
    /// </summary>
    [HttpPost("check/{endpointName}")]
    [Authorize(Policy = "ManageMonitoring")]
    public async Task<ActionResult<MonitoringResult>> TriggerEndpointCheck(string endpointName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manual monitoring check for endpoint {EndpointName} triggered by user {UserId}", 
                endpointName, User.Identity?.Name);
            
            var result = await _monitoringService.CheckEndpointByNameAsync(endpointName, cancellationToken);
            
            if (result == null)
            {
                return NotFound(new { message = $"Endpoint '{endpointName}' not found" });
            }

            // Send notification for the result
            var monitoringEvent = MonitoringEvent.FromResult(result, $"manual-check-{endpointName}");
            await _eventNotificationService.NotifyAsync(monitoringEvent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual monitoring check for endpoint {EndpointName}", endpointName);
            return StatusCode(500, new { message = "Error during monitoring check", error = ex.Message });
        }
    }

    /// <summary>
    /// Get current monitoring metrics
    /// </summary>
    [HttpGet("metrics")]
    [Authorize(Policy = "ViewMetrics")]
    public ActionResult<object> GetMetrics()
    {
        try
        {
            var metrics = _metricsService.GetCurrentMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitoring metrics");
            return StatusCode(500, new { message = "Error retrieving metrics", error = ex.Message });
        }
    }

    /// <summary>
    /// Get monitoring status summary
    /// </summary>
    [HttpGet("status")]
    [Authorize(Policy = "ViewMonitoring")]
    public ActionResult<object> GetMonitoringStatus()
    {
        try
        {
            var metrics = _metricsService.GetCurrentMetrics();
            
            var status = new
            {
                OverallStatus = DetermineOverallStatus(metrics),
                TotalEndpoints = GetTotalEndpoints(metrics),
                HealthyEndpoints = GetHealthyEndpoints(metrics),
                UnhealthyEndpoints = GetUnhealthyEndpoints(metrics),
                ErrorEndpoints = GetErrorEndpoints(metrics),
                LastUpdate = DateTime.UtcNow,
                Metrics = metrics
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitoring status");
            return StatusCode(500, new { message = "Error retrieving status", error = ex.Message });
        }
    }

    /// <summary>
    /// Get list of configured endpoints
    /// </summary>
    [HttpGet("endpoints")]
    [Authorize(Policy = "ViewMonitoring")]
    public ActionResult<object> GetEndpoints()
    {
        try
        {
            var endpoints = _monitoringService.GetConfiguredEndpoints();
            return Ok(endpoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configured endpoints");
            return StatusCode(500, new { message = "Error retrieving endpoints", error = ex.Message });
        }
    }

    /// <summary>
    /// Get monitoring history/statistics
    /// </summary>
    [HttpGet("history")]
    [Authorize(Policy = "ViewMetrics")]
    public ActionResult<object> GetMonitoringHistory([FromQuery] int hours = 24)
    {
        try
        {
            if (hours < 1 || hours > 168) // Max 1 week
            {
                return BadRequest(new { message = "Hours must be between 1 and 168" });
            }

            var history = _metricsService.GetMonitoringHistory(TimeSpan.FromHours(hours));
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitoring history");
            return StatusCode(500, new { message = "Error retrieving history", error = ex.Message });
        }
    }

    private static string DetermineOverallStatus(object metrics)
    {
        // This would need to be implemented based on your metrics structure
        // For now, return a simple status
        return "Healthy";
    }

    private static int GetTotalEndpoints(object metrics)
    {
        // This would need to be implemented based on your metrics structure
        return 0;
    }

    private static int GetHealthyEndpoints(object metrics)
    {
        // This would need to be implemented based on your metrics structure
        return 0;
    }

    private static int GetUnhealthyEndpoints(object metrics)
    {
        // This would need to be implemented based on your metrics structure
        return 0;
    }

    private static int GetErrorEndpoints(object metrics)
    {
        // This would need to be implemented based on your metrics structure
        return 0;
    }
}
