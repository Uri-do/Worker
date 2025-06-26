using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Configuration;

/// <summary>
/// Configuration settings for monitoring features
/// </summary>
public class MonitoringConfiguration
{
    public const string SectionName = "Monitoring";

    /// <summary>
    /// Enable or disable all monitoring features
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Metrics collection configuration
    /// </summary>
    public MetricsConfiguration Metrics { get; set; } = new();

    /// <summary>
    /// Health check configuration
    /// </summary>
    public HealthCheckConfiguration HealthChecks { get; set; } = new();

    /// <summary>
    /// Dashboard and visualization configuration
    /// </summary>
    public DashboardConfiguration Dashboards { get; set; } = new();

    /// <summary>
    /// Alerting configuration
    /// </summary>
    public AlertingConfiguration Alerting { get; set; } = new();

    /// <summary>
    /// Performance monitoring configuration
    /// </summary>
    public PerformanceConfiguration Performance { get; set; } = new();
}

/// <summary>
/// Metrics collection configuration
/// </summary>
public class MetricsConfiguration
{
    /// <summary>
    /// Enable basic application metrics (jobs, health checks)
    /// </summary>
    public bool EnableBasicMetrics { get; set; } = true;

    /// <summary>
    /// Enable detailed performance metrics (histograms, percentiles)
    /// </summary>
    public bool EnableDetailedMetrics { get; set; } = true;

    /// <summary>
    /// Enable system resource metrics (CPU, memory)
    /// </summary>
    public bool EnableSystemMetrics { get; set; } = true;

    /// <summary>
    /// Enable HTTP request metrics
    /// </summary>
    public bool EnableHttpMetrics { get; set; } = true;

    /// <summary>
    /// Enable custom business metrics
    /// </summary>
    public bool EnableCustomMetrics { get; set; } = false;

    /// <summary>
    /// Metrics endpoint path
    /// </summary>
    public string EndpointPath { get; set; } = "/metrics";

    /// <summary>
    /// Metrics collection interval in seconds
    /// </summary>
    [Range(1, 3600)]
    public int CollectionIntervalSeconds { get; set; } = 15;
}

/// <summary>
/// Health check configuration
/// </summary>
public class HealthCheckConfiguration
{
    /// <summary>
    /// Enable health check endpoint
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Health check endpoint path
    /// </summary>
    public string EndpointPath { get; set; } = "/health";

    /// <summary>
    /// Health check interval in seconds
    /// </summary>
    [Range(1, 3600)]
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Enable detailed health check responses
    /// </summary>
    public bool EnableDetailedResponses { get; set; } = true;

    /// <summary>
    /// Health check timeout in seconds
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Dashboard configuration
/// </summary>
public class DashboardConfiguration
{
    /// <summary>
    /// Enable dashboard generation and provisioning
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Generate basic monitoring dashboard
    /// </summary>
    public bool EnableBasicDashboard { get; set; } = true;

    /// <summary>
    /// Generate detailed performance dashboard
    /// </summary>
    public bool EnablePerformanceDashboard { get; set; } = true;

    /// <summary>
    /// Generate system resource dashboard
    /// </summary>
    public bool EnableSystemDashboard { get; set; } = true;

    /// <summary>
    /// Auto-refresh interval for dashboards in seconds
    /// </summary>
    [Range(5, 300)]
    public int RefreshIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Alerting configuration
/// </summary>
public class AlertingConfiguration
{
    /// <summary>
    /// Enable alerting rules
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Enable job failure alerts
    /// </summary>
    public bool EnableJobFailureAlerts { get; set; } = true;

    /// <summary>
    /// Enable health check failure alerts
    /// </summary>
    public bool EnableHealthCheckAlerts { get; set; } = true;

    /// <summary>
    /// Enable performance degradation alerts
    /// </summary>
    public bool EnablePerformanceAlerts { get; set; } = false;

    /// <summary>
    /// Enable system resource alerts
    /// </summary>
    public bool EnableSystemResourceAlerts { get; set; } = false;

    /// <summary>
    /// Job failure threshold (failures per minute)
    /// </summary>
    [Range(1, 100)]
    public int JobFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Health check failure threshold (consecutive failures)
    /// </summary>
    [Range(1, 20)]
    public int HealthCheckFailureThreshold { get; set; } = 3;

    /// <summary>
    /// Performance alert threshold (95th percentile in milliseconds)
    /// </summary>
    [Range(100, 30000)]
    public int PerformanceThresholdMs { get; set; } = 5000;
}

/// <summary>
/// Performance monitoring configuration
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// Enable performance tracking
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Enable request duration tracking
    /// </summary>
    public bool EnableDurationTracking { get; set; } = true;

    /// <summary>
    /// Enable memory usage tracking
    /// </summary>
    public bool EnableMemoryTracking { get; set; } = true;

    /// <summary>
    /// Enable CPU usage tracking
    /// </summary>
    public bool EnableCpuTracking { get; set; } = true;

    /// <summary>
    /// Enable garbage collection tracking
    /// </summary>
    public bool EnableGcTracking { get; set; } = false;

    /// <summary>
    /// Performance sampling rate (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// Performance data retention in days
    /// </summary>
    [Range(1, 365)]
    public int RetentionDays { get; set; } = 30;
}
