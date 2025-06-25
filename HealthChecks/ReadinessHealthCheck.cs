using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using MonitoringWorker.Services;

namespace MonitoringWorker.HealthChecks;

/// <summary>
/// Readiness health check that verifies the service is ready to handle requests
/// </summary>
public class ReadinessHealthCheck : IHealthCheck
{
    private readonly ILogger<ReadinessHealthCheck> _logger;
    private readonly IMonitoringService _monitoringService;
    private readonly MonitoringOptions _options;
    private static bool _hasBeenReady = false;
    private static DateTime? _firstReadyTime = null;

    public ReadinessHealthCheck(
        ILogger<ReadinessHealthCheck> logger,
        IMonitoringService monitoringService,
        IOptions<MonitoringOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogTrace("Readiness check starting");

            var data = new Dictionary<string, object>
            {
                ["configuredEndpoints"] = _options.Endpoints.Count,
                ["defaultTimeout"] = _options.DefaultTimeoutSeconds,
                ["hasBeenReady"] = _hasBeenReady
            };

            if (_firstReadyTime.HasValue)
            {
                data["firstReadyTime"] = _firstReadyTime.Value;
                data["readyDuration"] = (DateTime.UtcNow - _firstReadyTime.Value).ToString(@"dd\.hh\:mm\:ss");
            }

            // Check if we have any endpoints configured
            if (_options.Endpoints.Count == 0)
            {
                _logger.LogWarning("No monitoring endpoints configured");
                return HealthCheckResult.Unhealthy(
                    "No monitoring endpoints configured", 
                    data: data);
            }

            // Try to perform a quick check to verify the monitoring service is functional
            try
            {
                // Use a short timeout for readiness check to avoid blocking
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                // Try to check the first endpoint as a readiness test
                var firstEndpoint = _options.Endpoints.First();
                var result = await _monitoringService.PerformCheckAsync(firstEndpoint.Name, cts.Token);
                
                if (result != null)
                {
                    data["lastCheckResult"] = new
                    {
                        checkName = result.CheckName,
                        status = result.Status.ToString(),
                        timestamp = result.Timestamp,
                        durationMs = result.DurationMs
                    };

                    // Mark as ready if we successfully performed a check
                    if (!_hasBeenReady)
                    {
                        _hasBeenReady = true;
                        _firstReadyTime = DateTime.UtcNow;
                        _logger.LogInformation("Service is now ready to handle requests");
                    }

                    _logger.LogTrace("Readiness check completed successfully");
                    return HealthCheckResult.Healthy(
                        "Service is ready to handle requests", 
                        data);
                }
                else
                {
                    _logger.LogWarning("Monitoring service returned null result for readiness check");
                    return HealthCheckResult.Degraded(
                        "Monitoring service returned null result", 
                        data: data);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Readiness check was cancelled");
                return HealthCheckResult.Unhealthy(
                    "Readiness check was cancelled", 
                    data: data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Readiness check failed during monitoring service test");
                
                data["error"] = ex.Message;
                data["errorType"] = ex.GetType().Name;

                // If we've been ready before, this might be a temporary issue
                if (_hasBeenReady)
                {
                    return HealthCheckResult.Degraded(
                        "Monitoring service temporarily unavailable", 
                        ex, 
                        data);
                }
                else
                {
                    return HealthCheckResult.Unhealthy(
                        "Service is not ready - monitoring service unavailable", 
                        ex, 
                        data);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness health check failed unexpectedly");
            
            return HealthCheckResult.Unhealthy(
                "Readiness check failed unexpectedly", 
                ex);
        }
    }
}
