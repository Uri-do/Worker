using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using MonitoringWorker.Services;

namespace MonitoringWorker;

/// <summary>
/// Background worker service that provides heartbeat and lifecycle management
/// </summary>
public class Worker(
    ILogger<Worker> logger,
    IOptions<MonitoringOptions> options,
    IMetricsService metricsService) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly MonitoringOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly IMetricsService _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monitoring Worker started at {Time}. Configured endpoints: {EndpointCount}", 
            DateTimeOffset.UtcNow, _options.Endpoints.Count);

        // Log configured endpoints
        foreach (var endpoint in _options.Endpoints)
        {
            _logger.LogInformation("Monitoring endpoint: {Name} -> {Url} (timeout: {Timeout}s)", 
                endpoint.Name, endpoint.Url, endpoint.TimeoutSeconds ?? _options.DefaultTimeoutSeconds);
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Record heartbeat
                _metricsService.RecordHeartbeat();
                
                _logger.LogTrace("Worker heartbeat at {Time}", DateTimeOffset.UtcNow);
                
                // Wait for 60 seconds or until cancellation
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            _logger.LogInformation("Worker cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in worker execution");
            throw;
        }
        finally
        {
            _logger.LogInformation("Monitoring Worker stopped at {Time}", DateTimeOffset.UtcNow);
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Monitoring Worker service");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Monitoring Worker service");
        await base.StopAsync(cancellationToken);
    }
}
