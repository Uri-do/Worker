using Microsoft.Extensions.Diagnostics.HealthChecks;
using MonitoringWorker.Services;

namespace MonitoringWorker.HealthChecks;

/// <summary>
/// Health check for configuration validation
/// </summary>
public class ConfigurationHealthCheck : IHealthCheck
{
    private readonly IConfigurationValidationService _validationService;
    private readonly ILogger<ConfigurationHealthCheck> _logger;

    public ConfigurationHealthCheck(
        IConfigurationValidationService validationService,
        ILogger<ConfigurationHealthCheck> logger)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs configuration health check
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await _validationService.ValidateAllConfigurationAsync();

            var data = new Dictionary<string, object>
            {
                ["IsValid"] = validationResult.IsValid,
                ["ErrorCount"] = validationResult.Errors.Count,
                ["WarningCount"] = validationResult.Warnings.Count,
                ["Details"] = validationResult.Details
            };

            if (!validationResult.IsValid)
            {
                var errorMessage = $"Configuration validation failed with {validationResult.Errors.Count} errors";
                data["Errors"] = validationResult.Errors;
                
                _logger.LogError("Configuration health check failed: {ErrorMessage}. Errors: {Errors}", 
                    errorMessage, string.Join("; ", validationResult.Errors));

                return HealthCheckResult.Unhealthy(errorMessage, data: data);
            }

            if (validationResult.Warnings.Any())
            {
                var warningMessage = $"Configuration has {validationResult.Warnings.Count} warnings";
                data["Warnings"] = validationResult.Warnings;
                
                _logger.LogWarning("Configuration health check has warnings: {WarningMessage}. Warnings: {Warnings}", 
                    warningMessage, string.Join("; ", validationResult.Warnings));

                return HealthCheckResult.Degraded(warningMessage, data: data);
            }

            _logger.LogDebug("Configuration health check passed successfully");
            return HealthCheckResult.Healthy("Configuration is valid", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration health check");
            
            var data = new Dictionary<string, object>
            {
                ["Exception"] = ex.Message,
                ["ExceptionType"] = ex.GetType().Name
            };

            return HealthCheckResult.Unhealthy("Configuration health check failed with exception", ex, data);
        }
    }
}
