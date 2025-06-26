using MonitoringWorker.Models;
using System.Collections.Concurrent;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for monitoring Service Level Agreements (SLAs)
/// </summary>
public interface ISlaMonitoringService
{
    /// <summary>
    /// Calculate current SLA metrics
    /// </summary>
    Task<SlaMetrics> CalculateSlaMetricsAsync(TimeSpan period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SLA status for a specific service
    /// </summary>
    Task<SlaStatus> GetSlaStatusAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record SLA event
    /// </summary>
    Task RecordSlaEventAsync(SlaEvent slaEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate SLA report
    /// </summary>
    Task<SlaReport> GenerateSlaReportAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check for SLA violations
    /// </summary>
    Task<List<SlaViolation>> CheckSlaViolationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SLA trends
    /// </summary>
    Task<SlaMetrics[]> GetSlaTrendsAsync(TimeSpan period, TimeSpan interval, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of SLA monitoring service
/// </summary>
public class SlaMonitoringService : ISlaMonitoringService
{
    private readonly ILogger<SlaMonitoringService> _logger;
    private readonly IMetricsService _metricsService;
    private readonly INotificationService _notificationService;
    private readonly IFeatureToggleService _featureToggle;
    private readonly SlaConfiguration _config;
    private readonly ConcurrentDictionary<string, SlaTracker> _slaTrackers = new();

    public SlaMonitoringService(
        ILogger<SlaMonitoringService> logger,
        IMetricsService metricsService,
        INotificationService notificationService,
        IFeatureToggleService featureToggle,
        IConfiguration configuration)
    {
        _logger = logger;
        _metricsService = metricsService;
        _notificationService = notificationService;
        _featureToggle = featureToggle;
        _config = configuration.GetSection("SlaMonitoring").Get<SlaConfiguration>() ?? new();

        InitializeSlaTrackers();
    }

    public async Task<SlaMetrics> CalculateSlaMetricsAsync(TimeSpan period, CancellationToken cancellationToken = default)
    {
        if (!_featureToggle.IsEnabled("sla-monitoring"))
        {
            _logger.LogDebug("SLA monitoring is disabled");
            return new SlaMetrics();
        }

        var endTime = DateTime.UtcNow;
        var startTime = endTime.Subtract(period);

        // Get monitoring metrics for the period
        var monitoringMetrics = _metricsService.GetMonitoringMetrics();

        // Calculate availability
        var totalTime = period.TotalMinutes;
        var downtime = CalculateDowntime(startTime, endTime);
        var availability = Math.Max(0, (totalTime - downtime) / totalTime * 100);

        // Calculate success rate
        var totalJobs = monitoringMetrics.TotalJobs;
        var successfulJobs = monitoringMetrics.SuccessfulJobs;
        var successRate = totalJobs > 0 ? (double)successfulJobs / totalJobs * 100 : 100;

        // Calculate response time metrics
        var responseTimeMetrics = await CalculateResponseTimeMetricsAsync(startTime, endTime, cancellationToken);

        var slaMetrics = new SlaMetrics
        {
            Period = period,
            StartTime = startTime,
            EndTime = endTime,
            Availability = Math.Round(availability, 4),
            SuccessRate = Math.Round(successRate, 4),
            AverageResponseTime = responseTimeMetrics.Average,
            P95ResponseTime = responseTimeMetrics.P95,
            P99ResponseTime = responseTimeMetrics.P99,
            TotalRequests = totalJobs,
            SuccessfulRequests = successfulJobs,
            FailedRequests = monitoringMetrics.FailedJobs,
            ErrorRate = totalJobs > 0 ? (double)monitoringMetrics.FailedJobs / totalJobs * 100 : 0,
            Timestamp = DateTime.UtcNow
        };

        // Check SLA compliance
        CheckSlaCompliance(slaMetrics);

        _logger.LogDebug("Calculated SLA metrics for period {Period}: Availability={Availability}%, SuccessRate={SuccessRate}%",
            period, slaMetrics.Availability, slaMetrics.SuccessRate);

        return slaMetrics;
    }

    public async Task<SlaStatus> GetSlaStatusAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var slaDefinition = _config.SlaDefinitions.FirstOrDefault(s => s.ServiceName == serviceName);
        if (slaDefinition == null)
        {
            throw new ArgumentException($"SLA definition not found for service: {serviceName}");
        }

        var metrics = await CalculateSlaMetricsAsync(TimeSpan.FromHours(24), cancellationToken);

        var status = new SlaStatus
        {
            ServiceName = serviceName,
            SlaDefinition = slaDefinition,
            CurrentMetrics = metrics,
            IsCompliant = IsCompliant(metrics, slaDefinition),
            LastUpdated = DateTime.UtcNow
        };

        // Calculate compliance percentage
        status.CompliancePercentage = CalculateCompliancePercentage(metrics, slaDefinition);

        return status;
    }

    public async Task RecordSlaEventAsync(SlaEvent slaEvent, CancellationToken cancellationToken = default)
    {
        if (!_featureToggle.IsEnabled("sla-monitoring"))
        {
            return;
        }

        var tracker = _slaTrackers.GetOrAdd(slaEvent.ServiceName, _ => new SlaTracker());
        tracker.RecordEvent(slaEvent);

        // Check for immediate SLA violations
        if (slaEvent.EventType == SlaEventType.Violation)
        {
            await HandleSlaViolationAsync(slaEvent, cancellationToken);
        }

        _logger.LogDebug("Recorded SLA event: {ServiceName} - {EventType}", 
            slaEvent.ServiceName, slaEvent.EventType);
    }

    public async Task<SlaReport> GenerateSlaReportAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var period = endDate - startDate;
        var metrics = await CalculateSlaMetricsAsync(period, cancellationToken);

        var report = new SlaReport
        {
            ReportPeriod = new DateRange { Start = startDate, End = endDate },
            GeneratedAt = DateTime.UtcNow,
            OverallMetrics = metrics,
            ServiceMetrics = new List<ServiceSlaMetrics>(),
            Violations = await GetViolationsForPeriodAsync(startDate, endDate, cancellationToken),
            Summary = GenerateReportSummary(metrics)
        };

        // Generate per-service metrics
        foreach (var slaDefinition in _config.SlaDefinitions)
        {
            var serviceMetrics = await CalculateServiceMetricsAsync(slaDefinition.ServiceName, startDate, endDate, cancellationToken);
            report.ServiceMetrics.Add(serviceMetrics);
        }

        _logger.LogInformation("Generated SLA report for period {StartDate} to {EndDate}", startDate, endDate);

        return report;
    }

    public async Task<List<SlaViolation>> CheckSlaViolationsAsync(CancellationToken cancellationToken = default)
    {
        var violations = new List<SlaViolation>();

        foreach (var slaDefinition in _config.SlaDefinitions)
        {
            var status = await GetSlaStatusAsync(slaDefinition.ServiceName, cancellationToken);
            
            if (!status.IsCompliant)
            {
                var violation = new SlaViolation
                {
                    ServiceName = slaDefinition.ServiceName,
                    SlaType = DetermineSlaViolationType(status.CurrentMetrics, slaDefinition),
                    ViolationTime = DateTime.UtcNow,
                    ActualValue = GetActualValue(status.CurrentMetrics, slaDefinition),
                    ExpectedValue = GetExpectedValue(slaDefinition),
                    Severity = DetermineSeverity(status.CurrentMetrics, slaDefinition),
                    Description = GenerateViolationDescription(status.CurrentMetrics, slaDefinition)
                };

                violations.Add(violation);
            }
        }

        return violations;
    }

    public async Task<SlaMetrics[]> GetSlaTrendsAsync(TimeSpan period, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var trends = new List<SlaMetrics>();
        var endTime = DateTime.UtcNow;
        var currentTime = endTime.Subtract(period);

        while (currentTime < endTime)
        {
            var nextTime = currentTime.Add(interval);
            if (nextTime > endTime) nextTime = endTime;

            var periodMetrics = await CalculateSlaMetricsAsync(nextTime - currentTime, cancellationToken);
            periodMetrics.StartTime = currentTime;
            periodMetrics.EndTime = nextTime;
            
            trends.Add(periodMetrics);
            currentTime = nextTime;
        }

        return trends.ToArray();
    }

    private void InitializeSlaTrackers()
    {
        foreach (var slaDefinition in _config.SlaDefinitions)
        {
            _slaTrackers.TryAdd(slaDefinition.ServiceName, new SlaTracker());
        }
    }

    private double CalculateDowntime(DateTime startTime, DateTime endTime)
    {
        // In a real implementation, this would query historical uptime data
        // For now, we'll use a simple calculation based on current metrics
        var monitoringMetrics = _metricsService.GetMonitoringMetrics();
        var totalPeriod = (endTime - startTime).TotalMinutes;
        
        // Estimate downtime based on failure rate
        var failureRate = monitoringMetrics.TotalJobs > 0 ? 
            (double)monitoringMetrics.FailedJobs / monitoringMetrics.TotalJobs : 0;
        
        return totalPeriod * failureRate * 0.1; // Assume 10% of failures cause downtime
    }

    private async Task<ResponseTimeMetrics> CalculateResponseTimeMetricsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken)
    {
        // In a real implementation, this would query historical response time data
        // For now, we'll return estimated values
        await Task.Delay(1, cancellationToken);
        
        return new ResponseTimeMetrics
        {
            Average = 150,
            P95 = 500,
            P99 = 1000
        };
    }

    private void CheckSlaCompliance(SlaMetrics metrics)
    {
        foreach (var slaDefinition in _config.SlaDefinitions)
        {
            if (!IsCompliant(metrics, slaDefinition))
            {
                var violation = new SlaEvent
                {
                    ServiceName = slaDefinition.ServiceName,
                    EventType = SlaEventType.Violation,
                    Timestamp = DateTime.UtcNow,
                    Details = $"SLA violation detected: {GenerateViolationDescription(metrics, slaDefinition)}"
                };

                _ = Task.Run(async () => await RecordSlaEventAsync(violation));
            }
        }
    }

    private bool IsCompliant(SlaMetrics metrics, SlaDefinition slaDefinition)
    {
        return metrics.Availability >= slaDefinition.AvailabilityTarget &&
               metrics.SuccessRate >= slaDefinition.SuccessRateTarget &&
               metrics.P95ResponseTime <= slaDefinition.ResponseTimeTarget;
    }

    private double CalculateCompliancePercentage(SlaMetrics metrics, SlaDefinition slaDefinition)
    {
        var availabilityScore = Math.Min(100, metrics.Availability / slaDefinition.AvailabilityTarget * 100);
        var successRateScore = Math.Min(100, metrics.SuccessRate / slaDefinition.SuccessRateTarget * 100);
        var responseTimeScore = slaDefinition.ResponseTimeTarget > 0 ? 
            Math.Min(100, slaDefinition.ResponseTimeTarget / Math.Max(1, metrics.P95ResponseTime) * 100) : 100;

        return (availabilityScore + successRateScore + responseTimeScore) / 3;
    }

    private async Task HandleSlaViolationAsync(SlaEvent slaEvent, CancellationToken cancellationToken)
    {
        var notification = new NotificationMessage
        {
            Subject = $"SLA Violation: {slaEvent.ServiceName}",
            Body = slaEvent.Details,
            Severity = NotificationSeverity.Critical,
            Category = "sla",
            Source = "SlaMonitoringService",
            Metadata = new Dictionary<string, string>
            {
                ["ServiceName"] = slaEvent.ServiceName,
                ["ViolationType"] = slaEvent.EventType.ToString(),
                ["Timestamp"] = slaEvent.Timestamp.ToString("O")
            }
        };

        await _notificationService.SendNotificationAsync(notification, cancellationToken);
    }

    private async Task<List<SlaViolation>> GetViolationsForPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        // In a real implementation, this would query a database for historical violations
        await Task.Delay(1, cancellationToken);
        return new List<SlaViolation>();
    }

    private async Task<ServiceSlaMetrics> CalculateServiceMetricsAsync(string serviceName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var period = endDate - startDate;
        var metrics = await CalculateSlaMetricsAsync(period, cancellationToken);

        return new ServiceSlaMetrics
        {
            ServiceName = serviceName,
            Metrics = metrics,
            CompliancePercentage = 99.5, // Placeholder
            ViolationCount = 0 // Placeholder
        };
    }

    private string GenerateReportSummary(SlaMetrics metrics)
    {
        return $"Overall availability: {metrics.Availability:F2}%, Success rate: {metrics.SuccessRate:F2}%, " +
               $"Average response time: {metrics.AverageResponseTime:F0}ms";
    }

    private SlaViolationType DetermineSlaViolationType(SlaMetrics metrics, SlaDefinition slaDefinition)
    {
        if (metrics.Availability < slaDefinition.AvailabilityTarget)
            return SlaViolationType.Availability;
        if (metrics.SuccessRate < slaDefinition.SuccessRateTarget)
            return SlaViolationType.SuccessRate;
        if (metrics.P95ResponseTime > slaDefinition.ResponseTimeTarget)
            return SlaViolationType.ResponseTime;
        
        return SlaViolationType.Other;
    }

    private double GetActualValue(SlaMetrics metrics, SlaDefinition slaDefinition)
    {
        var violationType = DetermineSlaViolationType(metrics, slaDefinition);
        return violationType switch
        {
            SlaViolationType.Availability => metrics.Availability,
            SlaViolationType.SuccessRate => metrics.SuccessRate,
            SlaViolationType.ResponseTime => metrics.P95ResponseTime,
            _ => 0
        };
    }

    private double GetExpectedValue(SlaDefinition slaDefinition)
    {
        // Return the most restrictive target as the expected value
        return Math.Min(slaDefinition.AvailabilityTarget, 
               Math.Min(slaDefinition.SuccessRateTarget, slaDefinition.ResponseTimeTarget));
    }

    private NotificationSeverity DetermineSeverity(SlaMetrics metrics, SlaDefinition slaDefinition)
    {
        var availabilityGap = slaDefinition.AvailabilityTarget - metrics.Availability;
        var successRateGap = slaDefinition.SuccessRateTarget - metrics.SuccessRate;

        if (availabilityGap > 1 || successRateGap > 5)
            return NotificationSeverity.Critical;
        if (availabilityGap > 0.1 || successRateGap > 1)
            return NotificationSeverity.Warning;
        
        return NotificationSeverity.Info;
    }

    private string GenerateViolationDescription(SlaMetrics metrics, SlaDefinition slaDefinition)
    {
        var violations = new List<string>();

        if (metrics.Availability < slaDefinition.AvailabilityTarget)
            violations.Add($"Availability {metrics.Availability:F2}% below target {slaDefinition.AvailabilityTarget:F2}%");
        
        if (metrics.SuccessRate < slaDefinition.SuccessRateTarget)
            violations.Add($"Success rate {metrics.SuccessRate:F2}% below target {slaDefinition.SuccessRateTarget:F2}%");
        
        if (metrics.P95ResponseTime > slaDefinition.ResponseTimeTarget)
            violations.Add($"Response time {metrics.P95ResponseTime:F0}ms above target {slaDefinition.ResponseTimeTarget:F0}ms");

        return string.Join("; ", violations);
    }
}

/// <summary>
/// Internal class for tracking SLA events
/// </summary>
internal class SlaTracker
{
    private readonly List<SlaEvent> _events = new();
    private readonly object _lock = new();

    public void RecordEvent(SlaEvent slaEvent)
    {
        lock (_lock)
        {
            _events.Add(slaEvent);
            
            // Keep only recent events (last 24 hours)
            var cutoff = DateTime.UtcNow.AddHours(-24);
            _events.RemoveAll(e => e.Timestamp < cutoff);
        }
    }

    public List<SlaEvent> GetEvents(DateTime since)
    {
        lock (_lock)
        {
            return _events.Where(e => e.Timestamp >= since).ToList();
        }
    }
}

/// <summary>
/// Response time metrics helper class
/// </summary>
internal class ResponseTimeMetrics
{
    public double Average { get; set; }
    public double P95 { get; set; }
    public double P99 { get; set; }
}
