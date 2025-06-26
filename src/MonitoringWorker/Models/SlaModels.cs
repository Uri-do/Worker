namespace MonitoringWorker.Models;

/// <summary>
/// SLA event types
/// </summary>
public enum SlaEventType
{
    Compliance,
    Warning,
    Violation,
    Recovery
}

/// <summary>
/// SLA violation types
/// </summary>
public enum SlaViolationType
{
    Availability,
    SuccessRate,
    ResponseTime,
    Other
}

/// <summary>
/// SLA metrics for a specific period
/// </summary>
public class SlaMetrics
{
    /// <summary>
    /// Time period for these metrics
    /// </summary>
    public TimeSpan Period { get; set; }

    /// <summary>
    /// Start time of the measurement period
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the measurement period
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Availability percentage (0-100)
    /// </summary>
    public double Availability { get; set; }

    /// <summary>
    /// Success rate percentage (0-100)
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Error rate percentage (0-100)
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// 95th percentile response time in milliseconds
    /// </summary>
    public double P95ResponseTime { get; set; }

    /// <summary>
    /// 99th percentile response time in milliseconds
    /// </summary>
    public double P99ResponseTime { get; set; }

    /// <summary>
    /// Total number of requests
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Number of successful requests
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Number of failed requests
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Timestamp when metrics were calculated
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// SLA definition for a service
/// </summary>
public class SlaDefinition
{
    /// <summary>
    /// Service name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// SLA name/description
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Target availability percentage (0-100)
    /// </summary>
    public double AvailabilityTarget { get; set; } = 99.9;

    /// <summary>
    /// Target success rate percentage (0-100)
    /// </summary>
    public double SuccessRateTarget { get; set; } = 99.5;

    /// <summary>
    /// Target response time in milliseconds (95th percentile)
    /// </summary>
    public double ResponseTimeTarget { get; set; } = 1000;

    /// <summary>
    /// SLA measurement period
    /// </summary>
    public TimeSpan MeasurementPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Whether this SLA is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// SLA effective date
    /// </summary>
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// SLA expiration date (null = no expiration)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }
}

/// <summary>
/// Current SLA status for a service
/// </summary>
public class SlaStatus
{
    /// <summary>
    /// Service name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// SLA definition
    /// </summary>
    public SlaDefinition SlaDefinition { get; set; } = new();

    /// <summary>
    /// Current metrics
    /// </summary>
    public SlaMetrics CurrentMetrics { get; set; } = new();

    /// <summary>
    /// Whether the service is currently compliant with SLA
    /// </summary>
    public bool IsCompliant { get; set; }

    /// <summary>
    /// Overall compliance percentage
    /// </summary>
    public double CompliancePercentage { get; set; }

    /// <summary>
    /// Last time status was updated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Time until next SLA evaluation
    /// </summary>
    public TimeSpan? NextEvaluation { get; set; }
}

/// <summary>
/// SLA event record
/// </summary>
public class SlaEvent
{
    /// <summary>
    /// Service name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Event type
    /// </summary>
    public SlaEventType EventType { get; set; }

    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Event details
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Associated metrics at the time of the event
    /// </summary>
    public SlaMetrics? Metrics { get; set; }

    /// <summary>
    /// Event severity
    /// </summary>
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
}

/// <summary>
/// SLA violation record
/// </summary>
public class SlaViolation
{
    /// <summary>
    /// Service name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Type of SLA violation
    /// </summary>
    public SlaViolationType SlaType { get; set; }

    /// <summary>
    /// When the violation occurred
    /// </summary>
    public DateTime ViolationTime { get; set; }

    /// <summary>
    /// When the violation was resolved (null if ongoing)
    /// </summary>
    public DateTime? ResolvedTime { get; set; }

    /// <summary>
    /// Duration of the violation
    /// </summary>
    public TimeSpan? Duration => ResolvedTime?.Subtract(ViolationTime);

    /// <summary>
    /// Actual value that caused the violation
    /// </summary>
    public double ActualValue { get; set; }

    /// <summary>
    /// Expected/target value
    /// </summary>
    public double ExpectedValue { get; set; }

    /// <summary>
    /// Violation severity
    /// </summary>
    public NotificationSeverity Severity { get; set; }

    /// <summary>
    /// Violation description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether the violation is resolved
    /// </summary>
    public bool IsResolved => ResolvedTime.HasValue;
}

/// <summary>
/// SLA report for a specific period
/// </summary>
public class SlaReport
{
    /// <summary>
    /// Report period
    /// </summary>
    public DateRange ReportPeriod { get; set; } = new();

    /// <summary>
    /// When the report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Overall SLA metrics for the period
    /// </summary>
    public SlaMetrics OverallMetrics { get; set; } = new();

    /// <summary>
    /// Per-service SLA metrics
    /// </summary>
    public List<ServiceSlaMetrics> ServiceMetrics { get; set; } = new();

    /// <summary>
    /// SLA violations during the period
    /// </summary>
    public List<SlaViolation> Violations { get; set; } = new();

    /// <summary>
    /// Report summary
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Compliance score (0-100)
    /// </summary>
    public double ComplianceScore => ServiceMetrics.Any() ? 
        ServiceMetrics.Average(s => s.CompliancePercentage) : 0;
}

/// <summary>
/// SLA metrics for a specific service
/// </summary>
public class ServiceSlaMetrics
{
    /// <summary>
    /// Service name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Service SLA metrics
    /// </summary>
    public SlaMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Compliance percentage for this service
    /// </summary>
    public double CompliancePercentage { get; set; }

    /// <summary>
    /// Number of violations for this service
    /// </summary>
    public int ViolationCount { get; set; }

    /// <summary>
    /// Service-specific SLA targets
    /// </summary>
    public SlaDefinition? SlaDefinition { get; set; }
}

/// <summary>
/// Date range helper class
/// </summary>
public class DateRange
{
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// Duration of the range
    /// </summary>
    public TimeSpan Duration => End - Start;

    /// <summary>
    /// Whether the range contains the specified date
    /// </summary>
    public bool Contains(DateTime date) => date >= Start && date <= End;
}

/// <summary>
/// SLA configuration
/// </summary>
public class SlaConfiguration
{
    /// <summary>
    /// Whether SLA monitoring is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// SLA definitions for services
    /// </summary>
    public List<SlaDefinition> SlaDefinitions { get; set; } = new()
    {
        new SlaDefinition
        {
            ServiceName = "MonitoringWorker",
            Name = "MonitoringWorker SLA",
            AvailabilityTarget = 99.9,
            SuccessRateTarget = 99.5,
            ResponseTimeTarget = 1000,
            MeasurementPeriod = TimeSpan.FromDays(30)
        }
    };

    /// <summary>
    /// How often to evaluate SLA compliance
    /// </summary>
    public TimeSpan EvaluationInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long to retain SLA data
    /// </summary>
    public TimeSpan DataRetention { get; set; } = TimeSpan.FromDays(365);

    /// <summary>
    /// Whether to send notifications for SLA violations
    /// </summary>
    public bool SendNotifications { get; set; } = true;

    /// <summary>
    /// Whether to generate automatic reports
    /// </summary>
    public bool AutoGenerateReports { get; set; } = true;

    /// <summary>
    /// Report generation schedule (cron expression)
    /// </summary>
    public string ReportSchedule { get; set; } = "0 0 1 * *"; // Monthly on the 1st
}
