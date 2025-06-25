using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MonitoringWorker.HealthChecks;

/// <summary>
/// Liveness health check that verifies the service is alive and responding
/// </summary>
public class LivenessHealthCheck : IHealthCheck
{
    private readonly ILogger<LivenessHealthCheck> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public LivenessHealthCheck(ILogger<LivenessHealthCheck> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uptime = DateTime.UtcNow - _startTime;
            
            _logger.LogTrace("Liveness check executed. Uptime: {Uptime}", uptime);

            var data = new Dictionary<string, object>
            {
                ["uptime"] = uptime.ToString(@"dd\.hh\:mm\:ss"),
                ["uptimeSeconds"] = (int)uptime.TotalSeconds,
                ["startTime"] = _startTime,
                ["currentTime"] = DateTime.UtcNow,
                ["machineName"] = Environment.MachineName,
                ["processId"] = Environment.ProcessId
            };

            return Task.FromResult(HealthCheckResult.Healthy(
                "Service is alive and responding", 
                data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Liveness health check failed");
            
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Liveness check failed", 
                ex));
        }
    }
}
