using MonitoringWorker.Models;
using MonitoringWorker.Services;
using Quartz;
using System.Diagnostics;

namespace MonitoringWorker.Jobs;

/// <summary>
/// Quartz job that performs scheduled monitoring checks
/// </summary>
[DisallowConcurrentExecution]
public class MonitoringJob : IJob
{
    private readonly ILogger<MonitoringJob> _logger;
    private readonly IMonitoringService _monitoringService;
    private readonly IEventNotificationService _notificationService;
    private readonly IMetricsService _metricsService;

    public MonitoringJob(
        ILogger<MonitoringJob> logger,
        IMonitoringService monitoringService,
        IEventNotificationService notificationService,
        IMetricsService metricsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var jobId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting monitoring job {JobId} at {Time}", jobId, DateTimeOffset.UtcNow);
        
        try
        {
            _metricsService.RecordJobStart();
            
            // Perform monitoring checks
            var results = await _monitoringService.PerformChecksAsync(context.CancellationToken);
            
            // Process and notify results
            await ProcessResultsAsync(results, jobId, context.CancellationToken);
            
            stopwatch.Stop();
            _metricsService.RecordJobSuccess();
            
            _logger.LogInformation("Monitoring job {JobId} completed successfully in {Duration}ms", 
                jobId, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _metricsService.RecordJobCancellation();
            
            _logger.LogWarning("Monitoring job {JobId} was cancelled after {Duration}ms", 
                jobId, stopwatch.ElapsedMilliseconds);
            
            // Send cancellation notification
            await SendJobStatusNotificationAsync(jobId, MonitoringStatus.Error, 
                "Job was cancelled", stopwatch.ElapsedMilliseconds, context.CancellationToken);
            
            throw; // Re-throw to let Quartz handle the cancellation
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsService.RecordJobFailure();
            
            _logger.LogError(ex, "Error executing monitoring job {JobId} after {Duration}ms", 
                jobId, stopwatch.ElapsedMilliseconds);
            
            // Send error notification
            await SendJobStatusNotificationAsync(jobId, MonitoringStatus.Error, 
                "Job execution failed", stopwatch.ElapsedMilliseconds, context.CancellationToken, ex);
            
            throw; // Re-throw to let Quartz handle the error
        }
    }

    private async Task ProcessResultsAsync(IEnumerable<MonitoringResult> results, string jobId, CancellationToken cancellationToken)
    {
        foreach (var result in results)
        {
            try
            {
                _logger.LogDebug("Processing result for check {CheckName}: {Status} in {Duration}ms", 
                    result.CheckName, result.Status, result.DurationMs);
                
                // Record metrics for this check
                _metricsService.RecordCheckResult(result.CheckName, result.Status, result.DurationMs);
                
                // Create and send monitoring event
                var monitoringEvent = MonitoringEvent.FromResult(result, jobId);
                await _notificationService.NotifyAsync(monitoringEvent, cancellationToken);
                
                // Log status changes or errors
                if (result.Status != MonitoringStatus.Healthy)
                {
                    _logger.LogWarning("Check {CheckName} is {Status}: {Message}", 
                        result.CheckName, result.Status, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing result for check {CheckName}", result.CheckName);
                // Continue processing other results even if one fails
            }
        }
    }

    private async Task SendJobStatusNotificationAsync(
        string jobId, 
        MonitoringStatus status, 
        string message, 
        long durationMs, 
        CancellationToken cancellationToken,
        Exception? exception = null)
    {
        try
        {
            var jobEvent = new MonitoringEvent
            {
                Id = Guid.NewGuid().ToString(),
                JobId = jobId,
                CheckName = "MonitoringJob",
                Status = status,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow,
                DurationMs = durationMs,
                Details = exception != null ? new { Error = exception.Message, Type = exception.GetType().Name } : null
            };

            await _notificationService.NotifyAsync(jobEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send job status notification for job {JobId}", jobId);
            // Don't re-throw as this is a secondary operation
        }
    }
}
