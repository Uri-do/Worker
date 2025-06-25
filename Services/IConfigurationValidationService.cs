using MonitoringWorker.Configuration;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for validating configuration settings
/// </summary>
public interface IConfigurationValidationService
{
    /// <summary>
    /// Validates monitoring configuration
    /// </summary>
    /// <param name="options">Monitoring options to validate</param>
    /// <returns>Validation result</returns>
    Task<ConfigurationValidationResult> ValidateMonitoringOptionsAsync(MonitoringOptions options);

    /// <summary>
    /// Validates database monitoring configuration
    /// </summary>
    /// <param name="options">Database monitoring options to validate</param>
    /// <returns>Validation result</returns>
    Task<ConfigurationValidationResult> ValidateDatabaseMonitoringOptionsAsync(DatabaseMonitoringOptions options);

    /// <summary>
    /// Validates authentication configuration
    /// </summary>
    /// <param name="options">Authentication options to validate</param>
    /// <returns>Validation result</returns>
    Task<ConfigurationValidationResult> ValidateAuthenticationOptionsAsync(AuthenticationOptions options);

    /// <summary>
    /// Validates all configuration sections
    /// </summary>
    /// <returns>Comprehensive validation result</returns>
    Task<ConfigurationValidationResult> ValidateAllConfigurationAsync();
}


