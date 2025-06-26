using Microsoft.AspNetCore.Mvc;
using MonitoringWorker.Configuration;
using MonitoringWorker.Services;

namespace MonitoringWorker.Controllers;

/// <summary>
/// Controller for managing monitoring configuration and feature toggles
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MonitoringConfigurationController : ControllerBase
{
    private readonly IMonitoringConfigurationService _configService;
    private readonly IFeatureToggleService _featureToggleService;
    private readonly ILogger<MonitoringConfigurationController> _logger;

    public MonitoringConfigurationController(
        IMonitoringConfigurationService configService,
        IFeatureToggleService featureToggleService,
        ILogger<MonitoringConfigurationController> logger)
    {
        _configService = configService;
        _featureToggleService = featureToggleService;
        _logger = logger;
    }

    /// <summary>
    /// Get current monitoring configuration
    /// </summary>
    /// <returns>Current monitoring configuration</returns>
    [HttpGet]
    [ProducesResponseType(typeof(MonitoringConfiguration), 200)]
    public ActionResult<MonitoringConfiguration> GetConfiguration()
    {
        try
        {
            var config = _configService.Configuration;
            _logger.LogDebug("Retrieved monitoring configuration");
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitoring configuration");
            return StatusCode(500, new { error = "Failed to retrieve configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// Get monitoring configuration status
    /// </summary>
    /// <returns>Configuration status and validation results</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult GetConfigurationStatus()
    {
        try
        {
            var validation = _configService.ValidateConfiguration();
            var status = new
            {
                IsValid = validation == System.ComponentModel.DataAnnotations.ValidationResult.Success,
                ValidationMessage = validation?.ErrorMessage,
                IsMonitoringEnabled = _configService.IsMonitoringEnabled,
                EnabledFeatures = _featureToggleService.GetEnabledFeatures().ToList(),
                Configuration = new
                {
                    MetricsEndpoint = _configService.GetMetricsEndpointPath(),
                    HealthCheckEndpoint = _configService.GetHealthCheckEndpointPath(),
                    MetricsInterval = _configService.GetMetricsCollectionInterval(),
                    HealthCheckInterval = _configService.GetHealthCheckInterval()
                },
                Timestamp = DateTimeOffset.UtcNow
            };

            _logger.LogDebug("Retrieved monitoring configuration status");
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitoring configuration status");
            return StatusCode(500, new { error = "Failed to retrieve configuration status", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all feature toggles and their status
    /// </summary>
    /// <returns>Dictionary of feature names and their enabled status</returns>
    [HttpGet("features")]
    [ProducesResponseType(typeof(Dictionary<string, bool>), 200)]
    public ActionResult<Dictionary<string, bool>> GetFeatures()
    {
        try
        {
            var features = _featureToggleService.GetFeatureStatus();
            _logger.LogDebug("Retrieved feature toggle status for {FeatureCount} features", features.Count);
            return Ok(features);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature toggle status");
            return StatusCode(500, new { error = "Failed to retrieve feature status", details = ex.Message });
        }
    }

    /// <summary>
    /// Check if a specific feature is enabled
    /// </summary>
    /// <param name="featureName">Name of the feature to check</param>
    /// <returns>Feature status</returns>
    [HttpGet("features/{featureName}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public ActionResult GetFeatureStatus(string featureName)
    {
        if (string.IsNullOrWhiteSpace(featureName))
        {
            return BadRequest(new { error = "Feature name is required" });
        }

        try
        {
            var isEnabled = _featureToggleService.IsEnabled(featureName);
            var result = new
            {
                FeatureName = featureName,
                IsEnabled = isEnabled,
                Timestamp = DateTimeOffset.UtcNow
            };

            _logger.LogDebug("Checked feature '{FeatureName}': {Status}", featureName, isEnabled ? "enabled" : "disabled");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature '{FeatureName}' status", featureName);
            return StatusCode(500, new { error = "Failed to check feature status", details = ex.Message });
        }
    }

    /// <summary>
    /// Get enabled features only
    /// </summary>
    /// <returns>List of enabled feature names</returns>
    [HttpGet("features/enabled")]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    public ActionResult<IEnumerable<string>> GetEnabledFeatures()
    {
        try
        {
            var enabledFeatures = _featureToggleService.GetEnabledFeatures();
            _logger.LogDebug("Retrieved {Count} enabled features", enabledFeatures.Count());
            return Ok(enabledFeatures);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enabled features");
            return StatusCode(500, new { error = "Failed to retrieve enabled features", details = ex.Message });
        }
    }

    /// <summary>
    /// Get configuration summary for dashboard display
    /// </summary>
    /// <returns>Configuration summary</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult GetConfigurationSummary()
    {
        try
        {
            var enabledFeatures = _featureToggleService.GetEnabledFeatures().ToList();
            var summary = new
            {
                MonitoringEnabled = _configService.IsMonitoringEnabled,
                TotalFeatures = _featureToggleService.GetFeatureStatus().Count,
                EnabledFeatures = enabledFeatures.Count,
                DisabledFeatures = _featureToggleService.GetFeatureStatus().Count - enabledFeatures.Count,
                Categories = new
                {
                    Metrics = new
                    {
                        Enabled = _configService.IsBasicMetricsEnabled || _configService.IsDetailedMetricsEnabled,
                        Basic = _configService.IsBasicMetricsEnabled,
                        Detailed = _configService.IsDetailedMetricsEnabled,
                        System = _configService.IsSystemMetricsEnabled,
                        Http = _configService.IsHttpMetricsEnabled,
                        Custom = _configService.IsCustomMetricsEnabled
                    },
                    HealthChecks = new
                    {
                        Enabled = _configService.IsHealthChecksEnabled,
                        Detailed = _featureToggleService.IsEnabled("detailed-health-checks")
                    },
                    Dashboards = new
                    {
                        Enabled = _configService.IsDashboardsEnabled,
                        Basic = _featureToggleService.IsEnabled("basic-dashboard"),
                        Performance = _featureToggleService.IsEnabled("performance-dashboard"),
                        System = _featureToggleService.IsEnabled("system-dashboard")
                    },
                    Alerting = new
                    {
                        Enabled = _configService.IsAlertingEnabled,
                        JobFailures = _featureToggleService.IsEnabled("job-failure-alerts"),
                        HealthChecks = _featureToggleService.IsEnabled("health-check-alerts"),
                        Performance = _featureToggleService.IsEnabled("performance-alerts"),
                        SystemResources = _featureToggleService.IsEnabled("system-resource-alerts")
                    },
                    Performance = new
                    {
                        Enabled = _configService.IsPerformanceMonitoringEnabled,
                        Duration = _featureToggleService.IsEnabled("duration-tracking"),
                        Memory = _featureToggleService.IsEnabled("memory-tracking"),
                        Cpu = _featureToggleService.IsEnabled("cpu-tracking"),
                        GarbageCollection = _featureToggleService.IsEnabled("gc-tracking")
                    }
                },
                Endpoints = new
                {
                    Metrics = _configService.GetMetricsEndpointPath(),
                    HealthCheck = _configService.GetHealthCheckEndpointPath()
                },
                Intervals = new
                {
                    MetricsCollection = _configService.GetMetricsCollectionInterval(),
                    HealthCheck = _configService.GetHealthCheckInterval()
                },
                Timestamp = DateTimeOffset.UtcNow
            };

            _logger.LogDebug("Generated configuration summary");
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating configuration summary");
            return StatusCode(500, new { error = "Failed to generate configuration summary", details = ex.Message });
        }
    }

    /// <summary>
    /// Validate current configuration
    /// </summary>
    /// <returns>Validation results</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult ValidateConfiguration()
    {
        try
        {
            var validation = _configService.ValidateConfiguration();
            var result = new
            {
                IsValid = validation == System.ComponentModel.DataAnnotations.ValidationResult.Success,
                ValidationMessage = validation?.ErrorMessage,
                Timestamp = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("Configuration validation result: {IsValid}", result.IsValid);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration");
            return StatusCode(500, new { error = "Failed to validate configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// Get configuration recommendations
    /// </summary>
    /// <returns>Configuration recommendations</returns>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult GetConfigurationRecommendations()
    {
        try
        {
            var recommendations = new List<object>();

            // Check if monitoring is disabled
            if (!_configService.IsMonitoringEnabled)
            {
                recommendations.Add(new
                {
                    Type = "Warning",
                    Category = "Monitoring",
                    Message = "Monitoring is completely disabled. Consider enabling basic monitoring for production environments.",
                    Suggestion = "Set Monitoring.Enhanced.Enabled = true"
                });
            }

            // Check if no metrics are enabled
            if (_configService.IsMonitoringEnabled && 
                !_configService.IsBasicMetricsEnabled && 
                !_configService.IsDetailedMetricsEnabled)
            {
                recommendations.Add(new
                {
                    Type = "Warning",
                    Category = "Metrics",
                    Message = "No metrics collection is enabled. This limits observability.",
                    Suggestion = "Enable at least basic metrics for monitoring"
                });
            }

            // Check if alerting is disabled in production
            if (_configService.IsMonitoringEnabled && !_configService.IsAlertingEnabled)
            {
                recommendations.Add(new
                {
                    Type = "Info",
                    Category = "Alerting",
                    Message = "Alerting is disabled. Consider enabling alerts for production environments.",
                    Suggestion = "Configure alerting rules for critical failures"
                });
            }

            // Check performance monitoring
            if (_configService.IsMonitoringEnabled && !_configService.IsPerformanceMonitoringEnabled)
            {
                recommendations.Add(new
                {
                    Type = "Info",
                    Category = "Performance",
                    Message = "Performance monitoring is disabled. This may limit troubleshooting capabilities.",
                    Suggestion = "Enable performance tracking for better insights"
                });
            }

            var result = new
            {
                TotalRecommendations = recommendations.Count,
                Recommendations = recommendations,
                Timestamp = DateTimeOffset.UtcNow
            };

            _logger.LogDebug("Generated {Count} configuration recommendations", recommendations.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating configuration recommendations");
            return StatusCode(500, new { error = "Failed to generate recommendations", details = ex.Message });
        }
    }
}
