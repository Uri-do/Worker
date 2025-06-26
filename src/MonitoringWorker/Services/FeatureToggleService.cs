using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for managing feature toggles and conditional functionality
/// </summary>
public interface IFeatureToggleService
{
    /// <summary>
    /// Check if a feature is enabled
    /// </summary>
    bool IsEnabled(string featureName);

    /// <summary>
    /// Check if a feature is enabled with fallback
    /// </summary>
    bool IsEnabled(string featureName, bool defaultValue);

    /// <summary>
    /// Get feature configuration value
    /// </summary>
    T GetFeatureValue<T>(string featureName, T defaultValue);

    /// <summary>
    /// Execute action only if feature is enabled
    /// </summary>
    void ExecuteIfEnabled(string featureName, Action action);

    /// <summary>
    /// Execute function only if feature is enabled
    /// </summary>
    T? ExecuteIfEnabled<T>(string featureName, Func<T> function);

    /// <summary>
    /// Get all enabled features
    /// </summary>
    IEnumerable<string> GetEnabledFeatures();

    /// <summary>
    /// Get feature status summary
    /// </summary>
    Dictionary<string, bool> GetFeatureStatus();
}

/// <summary>
/// Implementation of feature toggle service
/// </summary>
public class FeatureToggleService : IFeatureToggleService
{
    private readonly MonitoringConfiguration _config;
    private readonly ILogger<FeatureToggleService> _logger;

    // Feature name constants
    public static class Features
    {
        public const string Monitoring = "monitoring";
        public const string BasicMetrics = "basic-metrics";
        public const string DetailedMetrics = "detailed-metrics";
        public const string SystemMetrics = "system-metrics";
        public const string HttpMetrics = "http-metrics";
        public const string CustomMetrics = "custom-metrics";
        public const string HealthChecks = "health-checks";
        public const string DetailedHealthChecks = "detailed-health-checks";
        public const string Dashboards = "dashboards";
        public const string BasicDashboard = "basic-dashboard";
        public const string PerformanceDashboard = "performance-dashboard";
        public const string SystemDashboard = "system-dashboard";
        public const string Alerting = "alerting";
        public const string JobFailureAlerts = "job-failure-alerts";
        public const string HealthCheckAlerts = "health-check-alerts";
        public const string PerformanceAlerts = "performance-alerts";
        public const string SystemResourceAlerts = "system-resource-alerts";
        public const string Performance = "performance";
        public const string DurationTracking = "duration-tracking";
        public const string MemoryTracking = "memory-tracking";
        public const string CpuTracking = "cpu-tracking";
        public const string GcTracking = "gc-tracking";
    }

    public FeatureToggleService(
        IOptionsMonitor<MonitoringConfiguration> options,
        ILogger<FeatureToggleService> logger)
    {
        _config = options.CurrentValue ?? new MonitoringConfiguration();
        _logger = logger;

        LogFeatureStatus();
    }

    public bool IsEnabled(string featureName)
    {
        return IsEnabled(featureName, false);
    }

    public bool IsEnabled(string featureName, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(featureName))
            return defaultValue;

        var normalizedName = featureName.ToLowerInvariant().Replace("_", "-");

        var isEnabled = normalizedName switch
        {
            Features.Monitoring => _config.Enabled,
            Features.BasicMetrics => _config.Enabled && _config.Metrics.EnableBasicMetrics,
            Features.DetailedMetrics => _config.Enabled && _config.Metrics.EnableDetailedMetrics,
            Features.SystemMetrics => _config.Enabled && _config.Metrics.EnableSystemMetrics,
            Features.HttpMetrics => _config.Enabled && _config.Metrics.EnableHttpMetrics,
            Features.CustomMetrics => _config.Enabled && _config.Metrics.EnableCustomMetrics,
            Features.HealthChecks => _config.Enabled && _config.HealthChecks.Enabled,
            Features.DetailedHealthChecks => _config.Enabled && _config.HealthChecks.Enabled && _config.HealthChecks.EnableDetailedResponses,
            Features.Dashboards => _config.Enabled && _config.Dashboards.Enabled,
            Features.BasicDashboard => _config.Enabled && _config.Dashboards.Enabled && _config.Dashboards.EnableBasicDashboard,
            Features.PerformanceDashboard => _config.Enabled && _config.Dashboards.Enabled && _config.Dashboards.EnablePerformanceDashboard,
            Features.SystemDashboard => _config.Enabled && _config.Dashboards.Enabled && _config.Dashboards.EnableSystemDashboard,
            Features.Alerting => _config.Enabled && _config.Alerting.Enabled,
            Features.JobFailureAlerts => _config.Enabled && _config.Alerting.Enabled && _config.Alerting.EnableJobFailureAlerts,
            Features.HealthCheckAlerts => _config.Enabled && _config.Alerting.Enabled && _config.Alerting.EnableHealthCheckAlerts,
            Features.PerformanceAlerts => _config.Enabled && _config.Alerting.Enabled && _config.Alerting.EnablePerformanceAlerts,
            Features.SystemResourceAlerts => _config.Enabled && _config.Alerting.Enabled && _config.Alerting.EnableSystemResourceAlerts,
            Features.Performance => _config.Enabled && _config.Performance.Enabled,
            Features.DurationTracking => _config.Enabled && _config.Performance.Enabled && _config.Performance.EnableDurationTracking,
            Features.MemoryTracking => _config.Enabled && _config.Performance.Enabled && _config.Performance.EnableMemoryTracking,
            Features.CpuTracking => _config.Enabled && _config.Performance.Enabled && _config.Performance.EnableCpuTracking,
            Features.GcTracking => _config.Enabled && _config.Performance.Enabled && _config.Performance.EnableGcTracking,
            _ => defaultValue
        };

        _logger.LogTrace("Feature '{FeatureName}' is {Status}", featureName, isEnabled ? "enabled" : "disabled");
        return isEnabled;
    }

    public T GetFeatureValue<T>(string featureName, T defaultValue)
    {
        if (string.IsNullOrWhiteSpace(featureName))
            return defaultValue;

        var normalizedName = featureName.ToLowerInvariant().Replace("_", "-");

        try
        {
            var value = normalizedName switch
            {
                "metrics-collection-interval" => (object)_config.Metrics.CollectionIntervalSeconds,
                "health-check-interval" => (object)_config.HealthChecks.IntervalSeconds,
                "health-check-timeout" => (object)_config.HealthChecks.TimeoutSeconds,
                "dashboard-refresh-interval" => (object)_config.Dashboards.RefreshIntervalSeconds,
                "job-failure-threshold" => (object)_config.Alerting.JobFailureThreshold,
                "health-check-failure-threshold" => (object)_config.Alerting.HealthCheckFailureThreshold,
                "performance-threshold-ms" => (object)_config.Alerting.PerformanceThresholdMs,
                "performance-sampling-rate" => (object)_config.Performance.SamplingRate,
                "performance-retention-days" => (object)_config.Performance.RetentionDays,
                "metrics-endpoint-path" => (object)_config.Metrics.EndpointPath,
                "health-check-endpoint-path" => (object)_config.HealthChecks.EndpointPath,
                _ => (object?)defaultValue
            };

            if (value is T typedValue)
                return typedValue;

            // Try to convert the value
            if (value != null)
                return (T)Convert.ChangeType(value, typeof(T));

            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get feature value for '{FeatureName}', returning default", featureName);
            return defaultValue;
        }
    }

    public void ExecuteIfEnabled(string featureName, Action action)
    {
        if (IsEnabled(featureName))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action for feature '{FeatureName}'", featureName);
                throw;
            }
        }
        else
        {
            _logger.LogTrace("Skipping action for disabled feature '{FeatureName}'", featureName);
        }
    }

    public T? ExecuteIfEnabled<T>(string featureName, Func<T> function)
    {
        if (IsEnabled(featureName))
        {
            try
            {
                return function();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function for feature '{FeatureName}'", featureName);
                throw;
            }
        }
        else
        {
            _logger.LogTrace("Skipping function for disabled feature '{FeatureName}'", featureName);
            return default(T);
        }
    }

    public IEnumerable<string> GetEnabledFeatures()
    {
        var features = new[]
        {
            Features.Monitoring, Features.BasicMetrics, Features.DetailedMetrics,
            Features.SystemMetrics, Features.HttpMetrics, Features.CustomMetrics,
            Features.HealthChecks, Features.DetailedHealthChecks,
            Features.Dashboards, Features.BasicDashboard, Features.PerformanceDashboard, Features.SystemDashboard,
            Features.Alerting, Features.JobFailureAlerts, Features.HealthCheckAlerts,
            Features.PerformanceAlerts, Features.SystemResourceAlerts,
            Features.Performance, Features.DurationTracking, Features.MemoryTracking,
            Features.CpuTracking, Features.GcTracking
        };

        return features.Where(IsEnabled);
    }

    public Dictionary<string, bool> GetFeatureStatus()
    {
        var features = new[]
        {
            Features.Monitoring, Features.BasicMetrics, Features.DetailedMetrics,
            Features.SystemMetrics, Features.HttpMetrics, Features.CustomMetrics,
            Features.HealthChecks, Features.DetailedHealthChecks,
            Features.Dashboards, Features.BasicDashboard, Features.PerformanceDashboard, Features.SystemDashboard,
            Features.Alerting, Features.JobFailureAlerts, Features.HealthCheckAlerts,
            Features.PerformanceAlerts, Features.SystemResourceAlerts,
            Features.Performance, Features.DurationTracking, Features.MemoryTracking,
            Features.CpuTracking, Features.GcTracking
        };

        return features.ToDictionary(feature => feature, IsEnabled);
    }

    private void LogFeatureStatus()
    {
        _logger.LogInformation("Feature Toggle Status:");
        var enabledFeatures = GetEnabledFeatures().ToList();
        
        if (enabledFeatures.Any())
        {
            _logger.LogInformation("Enabled features: {Features}", string.Join(", ", enabledFeatures));
        }
        else
        {
            _logger.LogInformation("No features are enabled");
        }
    }
}
