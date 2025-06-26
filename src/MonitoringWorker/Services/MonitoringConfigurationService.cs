using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for managing monitoring configuration and feature toggles
/// </summary>
public interface IMonitoringConfigurationService
{
    /// <summary>
    /// Get the current monitoring configuration
    /// </summary>
    MonitoringConfiguration Configuration { get; }

    /// <summary>
    /// Check if monitoring is enabled
    /// </summary>
    bool IsMonitoringEnabled { get; }

    /// <summary>
    /// Check if basic metrics are enabled
    /// </summary>
    bool IsBasicMetricsEnabled { get; }

    /// <summary>
    /// Check if detailed metrics are enabled
    /// </summary>
    bool IsDetailedMetricsEnabled { get; }

    /// <summary>
    /// Check if system metrics are enabled
    /// </summary>
    bool IsSystemMetricsEnabled { get; }

    /// <summary>
    /// Check if HTTP metrics are enabled
    /// </summary>
    bool IsHttpMetricsEnabled { get; }

    /// <summary>
    /// Check if custom metrics are enabled
    /// </summary>
    bool IsCustomMetricsEnabled { get; }

    /// <summary>
    /// Check if health checks are enabled
    /// </summary>
    bool IsHealthChecksEnabled { get; }

    /// <summary>
    /// Check if dashboards are enabled
    /// </summary>
    bool IsDashboardsEnabled { get; }

    /// <summary>
    /// Check if alerting is enabled
    /// </summary>
    bool IsAlertingEnabled { get; }

    /// <summary>
    /// Check if performance monitoring is enabled
    /// </summary>
    bool IsPerformanceMonitoringEnabled { get; }

    /// <summary>
    /// Get metrics endpoint path
    /// </summary>
    string GetMetricsEndpointPath();

    /// <summary>
    /// Get health check endpoint path
    /// </summary>
    string GetHealthCheckEndpointPath();

    /// <summary>
    /// Get metrics collection interval
    /// </summary>
    TimeSpan GetMetricsCollectionInterval();

    /// <summary>
    /// Get health check interval
    /// </summary>
    TimeSpan GetHealthCheckInterval();

    /// <summary>
    /// Validate configuration
    /// </summary>
    ValidationResult ValidateConfiguration();
}

/// <summary>
/// Implementation of monitoring configuration service
/// </summary>
public class MonitoringConfigurationService : IMonitoringConfigurationService
{
    private readonly MonitoringConfiguration _configuration;
    private readonly ILogger<MonitoringConfigurationService> _logger;

    public MonitoringConfigurationService(
        IOptionsMonitor<MonitoringConfiguration> options,
        ILogger<MonitoringConfigurationService> logger)
    {
        _configuration = options.CurrentValue ?? new MonitoringConfiguration();
        _logger = logger;

        // Log configuration on startup
        LogConfigurationStatus();
    }

    public MonitoringConfiguration Configuration => _configuration;

    public bool IsMonitoringEnabled => _configuration.Enabled;

    public bool IsBasicMetricsEnabled => 
        IsMonitoringEnabled && _configuration.Metrics.EnableBasicMetrics;

    public bool IsDetailedMetricsEnabled => 
        IsMonitoringEnabled && _configuration.Metrics.EnableDetailedMetrics;

    public bool IsSystemMetricsEnabled => 
        IsMonitoringEnabled && _configuration.Metrics.EnableSystemMetrics;

    public bool IsHttpMetricsEnabled => 
        IsMonitoringEnabled && _configuration.Metrics.EnableHttpMetrics;

    public bool IsCustomMetricsEnabled => 
        IsMonitoringEnabled && _configuration.Metrics.EnableCustomMetrics;

    public bool IsHealthChecksEnabled => 
        IsMonitoringEnabled && _configuration.HealthChecks.Enabled;

    public bool IsDashboardsEnabled => 
        IsMonitoringEnabled && _configuration.Dashboards.Enabled;

    public bool IsAlertingEnabled => 
        IsMonitoringEnabled && _configuration.Alerting.Enabled;

    public bool IsPerformanceMonitoringEnabled => 
        IsMonitoringEnabled && _configuration.Performance.Enabled;

    public string GetMetricsEndpointPath() => _configuration.Metrics.EndpointPath;

    public string GetHealthCheckEndpointPath() => _configuration.HealthChecks.EndpointPath;

    public TimeSpan GetMetricsCollectionInterval() => 
        TimeSpan.FromSeconds(_configuration.Metrics.CollectionIntervalSeconds);

    public TimeSpan GetHealthCheckInterval() => 
        TimeSpan.FromSeconds(_configuration.HealthChecks.IntervalSeconds);

    public ValidationResult ValidateConfiguration()
    {
        var context = new ValidationContext(_configuration);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(_configuration, context, results, true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            _logger.LogError("Monitoring configuration validation failed: {Errors}", errors);
            return new ValidationResult($"Configuration validation failed: {errors}");
        }

        // Additional custom validation
        if (_configuration.Metrics.CollectionIntervalSeconds < 1)
        {
            return new ValidationResult("Metrics collection interval must be at least 1 second");
        }

        if (_configuration.HealthChecks.IntervalSeconds < 1)
        {
            return new ValidationResult("Health check interval must be at least 1 second");
        }

        if (_configuration.Performance.SamplingRate < 0.0 || _configuration.Performance.SamplingRate > 1.0)
        {
            return new ValidationResult("Performance sampling rate must be between 0.0 and 1.0");
        }

        _logger.LogDebug("Monitoring configuration validation passed");
        return ValidationResult.Success!;
    }

    private void LogConfigurationStatus()
    {
        _logger.LogInformation("Monitoring Configuration Status:");
        _logger.LogInformation("  - Monitoring Enabled: {Enabled}", IsMonitoringEnabled);
        _logger.LogInformation("  - Basic Metrics: {Enabled}", IsBasicMetricsEnabled);
        _logger.LogInformation("  - Detailed Metrics: {Enabled}", IsDetailedMetricsEnabled);
        _logger.LogInformation("  - System Metrics: {Enabled}", IsSystemMetricsEnabled);
        _logger.LogInformation("  - HTTP Metrics: {Enabled}", IsHttpMetricsEnabled);
        _logger.LogInformation("  - Custom Metrics: {Enabled}", IsCustomMetricsEnabled);
        _logger.LogInformation("  - Health Checks: {Enabled}", IsHealthChecksEnabled);
        _logger.LogInformation("  - Dashboards: {Enabled}", IsDashboardsEnabled);
        _logger.LogInformation("  - Alerting: {Enabled}", IsAlertingEnabled);
        _logger.LogInformation("  - Performance Monitoring: {Enabled}", IsPerformanceMonitoringEnabled);
        _logger.LogInformation("  - Metrics Endpoint: {Path}", GetMetricsEndpointPath());
        _logger.LogInformation("  - Health Check Endpoint: {Path}", GetHealthCheckEndpointPath());
        _logger.LogInformation("  - Collection Interval: {Interval}s", _configuration.Metrics.CollectionIntervalSeconds);
    }
}

/// <summary>
/// Extension methods for monitoring configuration
/// </summary>
public static class MonitoringConfigurationExtensions
{
    /// <summary>
    /// Add monitoring configuration services
    /// </summary>
    public static IServiceCollection AddMonitoringConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<MonitoringConfiguration>(
            configuration.GetSection("Monitoring:Enhanced"));

        // Add configuration service
        services.AddSingleton<IMonitoringConfigurationService, MonitoringConfigurationService>();

        // Validate configuration on startup
        services.AddOptions<MonitoringConfiguration>()
            .Bind(configuration.GetSection("Monitoring:Enhanced"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Check if a specific feature is enabled
    /// </summary>
    public static bool IsFeatureEnabled(this IMonitoringConfigurationService config, string featureName)
    {
        return featureName.ToLowerInvariant() switch
        {
            "monitoring" => config.IsMonitoringEnabled,
            "basicmetrics" => config.IsBasicMetricsEnabled,
            "detailedmetrics" => config.IsDetailedMetricsEnabled,
            "systemmetrics" => config.IsSystemMetricsEnabled,
            "httpmetrics" => config.IsHttpMetricsEnabled,
            "custommetrics" => config.IsCustomMetricsEnabled,
            "healthchecks" => config.IsHealthChecksEnabled,
            "dashboards" => config.IsDashboardsEnabled,
            "alerting" => config.IsAlertingEnabled,
            "performance" => config.IsPerformanceMonitoringEnabled,
            _ => false
        };
    }
}
