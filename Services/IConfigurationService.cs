using MonitoringWorker.Configuration;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for comprehensive configuration management
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the current monitoring configuration
    /// </summary>
    /// <returns>Monitoring options</returns>
    MonitoringOptions GetMonitoringOptions();

    /// <summary>
    /// Gets the current authentication configuration
    /// </summary>
    /// <returns>Authentication options</returns>
    AuthenticationOptions GetAuthenticationOptions();

    /// <summary>
    /// Gets the current database monitoring configuration
    /// </summary>
    /// <returns>Database monitoring options</returns>
    DatabaseMonitoringOptions GetDatabaseMonitoringOptions();

    /// <summary>
    /// Gets the current Quartz configuration
    /// </summary>
    /// <returns>Quartz options</returns>
    QuartzOptions GetQuartzOptions();

    /// <summary>
    /// Updates monitoring configuration
    /// </summary>
    /// <param name="options">New monitoring options</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateMonitoringOptionsAsync(MonitoringOptions options);

    /// <summary>
    /// Updates authentication configuration
    /// </summary>
    /// <param name="options">New authentication options</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateAuthenticationOptionsAsync(AuthenticationOptions options);

    /// <summary>
    /// Updates database monitoring configuration
    /// </summary>
    /// <param name="options">New database monitoring options</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateDatabaseMonitoringOptionsAsync(DatabaseMonitoringOptions options);

    /// <summary>
    /// Updates Quartz configuration
    /// </summary>
    /// <param name="options">New Quartz options</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateQuartzOptionsAsync(QuartzOptions options);

    /// <summary>
    /// Validates configuration options
    /// </summary>
    /// <typeparam name="T">Configuration type</typeparam>
    /// <param name="options">Options to validate</param>
    /// <returns>Validation results</returns>
    Task<ConfigurationValidationResult> ValidateOptionsAsync<T>(T options) where T : class;

    /// <summary>
    /// Gets all configuration as a dictionary
    /// </summary>
    /// <returns>Configuration dictionary</returns>
    Task<Dictionary<string, object>> GetAllConfigurationAsync();

    /// <summary>
    /// Gets configuration value by key
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <returns>Configuration value</returns>
    Task<string?> GetConfigurationValueAsync(string key);

    /// <summary>
    /// Sets configuration value by key
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    /// <returns>True if set was successful</returns>
    Task<bool> SetConfigurationValueAsync(string key, string value);

    /// <summary>
    /// Reloads configuration from sources
    /// </summary>
    /// <returns>True if reload was successful</returns>
    Task<bool> ReloadConfigurationAsync();

    /// <summary>
    /// Gets configuration change history
    /// </summary>
    /// <param name="limit">Maximum number of changes to return</param>
    /// <returns>Configuration change history</returns>
    Task<IEnumerable<ConfigurationChange>> GetChangeHistoryAsync(int limit = 100);

    /// <summary>
    /// Event fired when configuration changes
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Whether validation was successful
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warning messages
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Configuration change record
/// </summary>
public class ConfigurationChange
{
    /// <summary>
    /// Change timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Configuration key that changed
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Old value
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// User who made the change
    /// </summary>
    public string? ChangedBy { get; set; }

    /// <summary>
    /// Change reason/description
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Configuration changed event arguments
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Configuration section that changed
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// Configuration key that changed
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Old value
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// New value
    /// </summary>
    public object? NewValue { get; set; }

    /// <summary>
    /// Change timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
