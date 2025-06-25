using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Configuration;

/// <summary>
/// Configuration options for Quartz scheduling
/// </summary>
public class QuartzOptions
{
    /// <summary>
    /// The configuration section name
    /// </summary>
    public const string SectionName = "Quartz";
    
    /// <summary>
    /// Cron expression for scheduling monitoring jobs
    /// </summary>
    [Required(ErrorMessage = "Cron schedule is required")]
    public string CronSchedule { get; set; } = "0 0/1 * * * ?"; // Every minute by default
    
    /// <summary>
    /// Maximum number of concurrent jobs
    /// </summary>
    [Range(1, 10, ErrorMessage = "Max concurrent jobs must be between 1 and 10")]
    public int MaxConcurrentJobs { get; set; } = 1;
    
    /// <summary>
    /// Whether to wait for jobs to complete on shutdown
    /// </summary>
    public bool WaitForJobsToComplete { get; set; } = true;
}
