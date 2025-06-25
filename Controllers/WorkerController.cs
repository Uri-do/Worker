using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using Quartz;

namespace MonitoringWorker.Controllers;

/// <summary>
/// Controller for managing the monitoring worker lifecycle
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkerController : ControllerBase
{
    private readonly ILogger<WorkerController> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IMetricsService _metricsService;
    private readonly IEventNotificationService _eventNotificationService;

    public WorkerController(
        ILogger<WorkerController> logger,
        ISchedulerFactory schedulerFactory,
        IMetricsService metricsService,
        IEventNotificationService eventNotificationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _eventNotificationService = eventNotificationService ?? throw new ArgumentNullException(nameof(eventNotificationService));
    }

    /// <summary>
    /// Get current worker status
    /// </summary>
    /// <returns>Worker status information</returns>
    [HttpGet("status")]
    [Authorize(Policy = "ViewMonitoring")]
    public async Task<ActionResult<WorkerStatus>> GetStatus()
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var isStarted = scheduler.IsStarted;
            var isShutdown = scheduler.IsShutdown;
            var jobKeys = await scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.AnyGroup());
            
            var jobs = new List<JobInfo>();
            foreach (var jobKey in jobKeys)
            {
                var jobDetail = await scheduler.GetJobDetail(jobKey);
                var triggers = await scheduler.GetTriggersOfJob(jobKey);
                
                foreach (var trigger in triggers)
                {
                    var triggerState = await scheduler.GetTriggerState(trigger.Key);
                    jobs.Add(new JobInfo
                    {
                        JobName = jobKey.Name,
                        JobGroup = jobKey.Group,
                        TriggerName = trigger.Key.Name,
                        TriggerGroup = trigger.Key.Group,
                        State = triggerState.ToString(),
                        NextFireTime = trigger.GetNextFireTimeUtc()?.DateTime,
                        PreviousFireTime = trigger.GetPreviousFireTimeUtc()?.DateTime,
                        Description = jobDetail?.Description
                    });
                }
            }

            var status = new WorkerStatus
            {
                IsRunning = isStarted && !isShutdown,
                IsStarted = isStarted,
                IsShutdown = isShutdown,
                StartTime = scheduler.IsStarted ? DateTime.UtcNow : null, // Approximate
                Jobs = jobs,
                Metrics = _metricsService.GetMonitoringMetrics()
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worker status");
            return StatusCode(500, new { error = "Failed to get worker status", details = ex.Message });
        }
    }

    /// <summary>
    /// Start the monitoring worker
    /// </summary>
    /// <returns>Operation result</returns>
    [HttpPost("start")]
    [Authorize(Policy = "ManageMonitoring")]
    public async Task<ActionResult<OperationResult>> StartWorker()
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            
            if (scheduler.IsStarted)
            {
                return BadRequest(new OperationResult 
                { 
                    Success = false, 
                    Message = "Worker is already running" 
                });
            }

            await scheduler.Start();
            
            _logger.LogInformation("Monitoring worker started by user {User}", User.Identity?.Name);
            
            // Notify clients
            await _eventNotificationService.NotifyAsync(new MonitoringEvent
            {
                Id = Guid.NewGuid().ToString(),
                Type = "WorkerStarted",
                Message = "Monitoring worker has been started",
                Timestamp = DateTime.UtcNow,
                Severity = "Info"
            }, CancellationToken.None);

            return Ok(new OperationResult 
            { 
                Success = true, 
                Message = "Worker started successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting worker");
            return StatusCode(500, new OperationResult 
            { 
                Success = false, 
                Message = "Failed to start worker", 
                Details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Stop the monitoring worker
    /// </summary>
    /// <returns>Operation result</returns>
    [HttpPost("stop")]
    [Authorize(Policy = "ManageMonitoring")]
    public async Task<ActionResult<OperationResult>> StopWorker()
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            
            if (!scheduler.IsStarted || scheduler.IsShutdown)
            {
                return BadRequest(new OperationResult 
                { 
                    Success = false, 
                    Message = "Worker is not running" 
                });
            }

            await scheduler.Standby();
            
            _logger.LogInformation("Monitoring worker stopped by user {User}", User.Identity?.Name);
            
            // Notify clients
            await _eventNotificationService.NotifyAsync(new MonitoringEvent
            {
                Id = Guid.NewGuid().ToString(),
                Type = "WorkerStopped",
                Message = "Monitoring worker has been stopped",
                Timestamp = DateTime.UtcNow,
                Severity = "Warning"
            }, CancellationToken.None);

            return Ok(new OperationResult 
            { 
                Success = true, 
                Message = "Worker stopped successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping worker");
            return StatusCode(500, new OperationResult 
            { 
                Success = false, 
                Message = "Failed to stop worker", 
                Details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Restart the monitoring worker
    /// </summary>
    /// <returns>Operation result</returns>
    [HttpPost("restart")]
    [Authorize(Policy = "ManageMonitoring")]
    public async Task<ActionResult<OperationResult>> RestartWorker()
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            
            // Stop if running
            if (scheduler.IsStarted && !scheduler.IsShutdown)
            {
                await scheduler.Standby();
                await Task.Delay(1000); // Brief pause
            }
            
            // Start
            await scheduler.Start();
            
            _logger.LogInformation("Monitoring worker restarted by user {User}", User.Identity?.Name);
            
            // Notify clients
            await _eventNotificationService.NotifyAsync(new MonitoringEvent
            {
                Id = Guid.NewGuid().ToString(),
                Type = "WorkerRestarted",
                Message = "Monitoring worker has been restarted",
                Timestamp = DateTime.UtcNow,
                Severity = "Info"
            }, CancellationToken.None);

            return Ok(new OperationResult 
            { 
                Success = true, 
                Message = "Worker restarted successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting worker");
            return StatusCode(500, new OperationResult 
            { 
                Success = false, 
                Message = "Failed to restart worker", 
                Details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Pause a specific job
    /// </summary>
    /// <param name="jobName">Name of the job to pause</param>
    /// <returns>Operation result</returns>
    [HttpPost("jobs/{jobName}/pause")]
    [Authorize(Policy = "ManageMonitoring")]
    public async Task<ActionResult<OperationResult>> PauseJob(string jobName)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName);
            
            if (!await scheduler.CheckExists(jobKey))
            {
                return NotFound(new OperationResult 
                { 
                    Success = false, 
                    Message = $"Job '{jobName}' not found" 
                });
            }

            await scheduler.PauseJob(jobKey);
            
            _logger.LogInformation("Job {JobName} paused by user {User}", jobName, User.Identity?.Name);

            return Ok(new OperationResult 
            { 
                Success = true, 
                Message = $"Job '{jobName}' paused successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing job {JobName}", jobName);
            return StatusCode(500, new OperationResult 
            { 
                Success = false, 
                Message = $"Failed to pause job '{jobName}'", 
                Details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Resume a specific job
    /// </summary>
    /// <param name="jobName">Name of the job to resume</param>
    /// <returns>Operation result</returns>
    [HttpPost("jobs/{jobName}/resume")]
    [Authorize(Policy = "ManageMonitoring")]
    public async Task<ActionResult<OperationResult>> ResumeJob(string jobName)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName);
            
            if (!await scheduler.CheckExists(jobKey))
            {
                return NotFound(new OperationResult 
                { 
                    Success = false, 
                    Message = $"Job '{jobName}' not found" 
                });
            }

            await scheduler.ResumeJob(jobKey);
            
            _logger.LogInformation("Job {JobName} resumed by user {User}", jobName, User.Identity?.Name);

            return Ok(new OperationResult 
            { 
                Success = true, 
                Message = $"Job '{jobName}' resumed successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming job {JobName}", jobName);
            return StatusCode(500, new OperationResult 
            { 
                Success = false, 
                Message = $"Failed to resume job '{jobName}'", 
                Details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Get detailed worker metrics
    /// </summary>
    /// <param name="metricType">Filter by metric type</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <returns>Worker metrics data</returns>
    [HttpGet("metrics")]
    [Authorize(Policy = "ViewMetrics")]
    public async Task<ActionResult<object>> GetWorkerMetrics(
        [FromQuery] string? metricType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var metrics = _metricsService.GetMonitoringMetrics();

            // In a full implementation, this would query the database for historical metrics
            var result = new
            {
                Current = metrics,
                Historical = new[]
                {
                    new { Timestamp = DateTime.UtcNow.AddMinutes(-30), TotalChecks = metrics.TotalChecks - 5, SuccessRate = 85.0 },
                    new { Timestamp = DateTime.UtcNow.AddMinutes(-15), TotalChecks = metrics.TotalChecks - 2, SuccessRate = 90.0 },
                    new { Timestamp = DateTime.UtcNow, TotalChecks = metrics.TotalChecks, SuccessRate = metrics.SuccessRate }
                },
                Summary = new
                {
                    AverageSuccessRate = 88.3,
                    TotalUptime = metrics.Uptime,
                    AverageResponseTime = 245.5,
                    PeakMemoryUsage = "125 MB"
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worker metrics");
            return StatusCode(500, new { error = "Failed to get worker metrics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get worker instances overview
    /// </summary>
    /// <returns>List of worker instances</returns>
    [HttpGet("instances")]
    [Authorize(Policy = "ViewMonitoring")]
    public async Task<ActionResult<object>> GetWorkerInstances()
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var metrics = _metricsService.GetMonitoringMetrics();

            // In a full implementation, this would query the database for actual worker instances
            var instances = new[]
            {
                new
                {
                    WorkerInstanceId = Guid.NewGuid(),
                    WorkerName = "MonitoringWorker-01",
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId,
                    Version = "1.0.0",
                    Environment = "Production",
                    Status = scheduler.IsStarted ? "Running" : "Stopped",
                    StartedAt = DateTime.UtcNow.AddHours(-2),
                    LastHeartbeat = metrics.LastHeartbeat,
                    Uptime = metrics.Uptime,
                    RunningJobs = 0,
                    QueuedJobs = 0
                }
            };

            return Ok(instances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worker instances");
            return StatusCode(500, new { error = "Failed to get worker instances", details = ex.Message });
        }
    }

    /// <summary>
    /// Get worker jobs with filtering
    /// </summary>
    /// <param name="status">Filter by job status</param>
    /// <param name="jobType">Filter by job type</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <returns>Paginated worker jobs</returns>
    [HttpGet("jobs")]
    [Authorize(Policy = "ViewMonitoring")]
    public async Task<ActionResult<object>> GetWorkerJobs(
        [FromQuery] string? status = null,
        [FromQuery] string? jobType = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageNumber = 1)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());

            var jobs = new List<object>();

            foreach (var jobKey in jobKeys)
            {
                var jobDetail = await scheduler.GetJobDetail(jobKey);
                var triggers = await scheduler.GetTriggersOfJob(jobKey);

                foreach (var trigger in triggers)
                {
                    var triggerState = await scheduler.GetTriggerState(trigger.Key);

                    jobs.Add(new
                    {
                        JobId = Guid.NewGuid(), // In real implementation, this would come from database
                        JobName = jobKey.Name,
                        JobGroup = jobKey.Group,
                        JobType = "DatabaseMonitoring", // Would be determined from job data
                        Status = MapTriggerStateToJobStatus(triggerState),
                        Priority = 5, // Default priority
                        ScheduledAt = DateTime.UtcNow,
                        NextFireTime = trigger.GetNextFireTimeUtc()?.DateTime,
                        PreviousFireTime = trigger.GetPreviousFireTimeUtc()?.DateTime,
                        Description = jobDetail?.Description
                    });
                }
            }

            // Apply filtering
            var filteredJobs = jobs.AsEnumerable();

            if (!string.IsNullOrEmpty(status))
                filteredJobs = filteredJobs.Where(j => j.GetType().GetProperty("Status")?.GetValue(j)?.ToString()?.Equals(status, StringComparison.OrdinalIgnoreCase) == true);

            if (!string.IsNullOrEmpty(jobType))
                filteredJobs = filteredJobs.Where(j => j.GetType().GetProperty("JobType")?.GetValue(j)?.ToString()?.Equals(jobType, StringComparison.OrdinalIgnoreCase) == true);

            var totalCount = filteredJobs.Count();
            var pagedJobs = filteredJobs
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new
            {
                Items = pagedJobs,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = pageNumber * pageSize < totalCount,
                HasPreviousPage = pageNumber > 1
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worker jobs");
            return StatusCode(500, new { error = "Failed to get worker jobs", details = ex.Message });
        }
    }

    private static string MapTriggerStateToJobStatus(TriggerState triggerState)
    {
        return triggerState switch
        {
            TriggerState.Normal => "Queued",
            TriggerState.Paused => "Paused",
            TriggerState.Complete => "Completed",
            TriggerState.Error => "Failed",
            TriggerState.Blocked => "Running",
            _ => "Unknown"
        };
    }
}
