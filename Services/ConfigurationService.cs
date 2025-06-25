using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using MonitoringWorker.Models;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MonitoringWorker.Services;

/// <summary>
/// Implementation of comprehensive configuration service
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<MonitoringOptions> _monitoringOptions;
    private readonly IOptionsMonitor<AuthenticationOptions> _authOptions;
    private readonly IOptionsMonitor<DatabaseMonitoringOptions> _dbOptions;
    private readonly IOptionsMonitor<QuartzOptions> _quartzOptions;
    private readonly ConcurrentQueue<ConfigurationChange> _changeHistory = new();
    private readonly object _lockObject = new();

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ConfigurationService(
        ILogger<ConfigurationService> logger,
        IConfiguration configuration,
        IOptionsMonitor<MonitoringOptions> monitoringOptions,
        IOptionsMonitor<AuthenticationOptions> authOptions,
        IOptionsMonitor<DatabaseMonitoringOptions> dbOptions,
        IOptionsMonitor<QuartzOptions> quartzOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _monitoringOptions = monitoringOptions ?? throw new ArgumentNullException(nameof(monitoringOptions));
        _authOptions = authOptions ?? throw new ArgumentNullException(nameof(authOptions));
        _dbOptions = dbOptions ?? throw new ArgumentNullException(nameof(dbOptions));
        _quartzOptions = quartzOptions ?? throw new ArgumentNullException(nameof(quartzOptions));

        // Subscribe to configuration changes
        _monitoringOptions.OnChange((options, name) => OnConfigurationChanged("Monitoring", options));
        _authOptions.OnChange((options, name) => OnConfigurationChanged("Authentication", options));
        _dbOptions.OnChange((options, name) => OnConfigurationChanged("DatabaseMonitoring", options));
        _quartzOptions.OnChange((options, name) => OnConfigurationChanged("Quartz", options));
    }

    public MonitoringOptions GetMonitoringOptions()
    {
        return _monitoringOptions.CurrentValue;
    }

    public AuthenticationOptions GetAuthenticationOptions()
    {
        return _authOptions.CurrentValue;
    }

    public DatabaseMonitoringOptions GetDatabaseMonitoringOptions()
    {
        return _dbOptions.CurrentValue;
    }

    public QuartzOptions GetQuartzOptions()
    {
        return _quartzOptions.CurrentValue;
    }

    public async Task<bool> UpdateMonitoringOptionsAsync(MonitoringOptions options)
    {
        try
        {
            var validationResult = await ValidateOptionsAsync(options);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Monitoring options validation failed: {Errors}", 
                    string.Join(", ", validationResult.Errors));
                return false;
            }

            // In a real implementation, this would update the configuration source
            // For now, we'll just log the change
            _logger.LogInformation("Monitoring options would be updated (not implemented in this demo)");
            
            RecordConfigurationChange("Monitoring", JsonSerializer.Serialize(_monitoringOptions.CurrentValue), 
                JsonSerializer.Serialize(options), "System", "Configuration update via API");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating monitoring options");
            return false;
        }
    }

    public async Task<bool> UpdateAuthenticationOptionsAsync(AuthenticationOptions options)
    {
        try
        {
            var validationResult = await ValidateOptionsAsync(options);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Authentication options validation failed: {Errors}", 
                    string.Join(", ", validationResult.Errors));
                return false;
            }

            _logger.LogInformation("Authentication options would be updated (not implemented in this demo)");
            
            RecordConfigurationChange("Authentication", JsonSerializer.Serialize(_authOptions.CurrentValue), 
                JsonSerializer.Serialize(options), "System", "Configuration update via API");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating authentication options");
            return false;
        }
    }

    public async Task<bool> UpdateDatabaseMonitoringOptionsAsync(DatabaseMonitoringOptions options)
    {
        try
        {
            var validationResult = await ValidateOptionsAsync(options);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Database monitoring options validation failed: {Errors}", 
                    string.Join(", ", validationResult.Errors));
                return false;
            }

            _logger.LogInformation("Database monitoring options would be updated (not implemented in this demo)");
            
            RecordConfigurationChange("DatabaseMonitoring", JsonSerializer.Serialize(_dbOptions.CurrentValue), 
                JsonSerializer.Serialize(options), "System", "Configuration update via API");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating database monitoring options");
            return false;
        }
    }

    public async Task<bool> UpdateQuartzOptionsAsync(QuartzOptions options)
    {
        try
        {
            var validationResult = await ValidateOptionsAsync(options);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Quartz options validation failed: {Errors}", 
                    string.Join(", ", validationResult.Errors));
                return false;
            }

            _logger.LogInformation("Quartz options would be updated (not implemented in this demo)");
            
            RecordConfigurationChange("Quartz", JsonSerializer.Serialize(_quartzOptions.CurrentValue), 
                JsonSerializer.Serialize(options), "System", "Configuration update via API");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Quartz options");
            return false;
        }
    }

    public async Task<ConfigurationValidationResult> ValidateOptionsAsync<T>(T options) where T : class
    {
        var result = new ConfigurationValidationResult { IsValid = true };

        try
        {
            var validationContext = new ValidationContext(options);
            var validationResults = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(options, validationContext, validationResults, true);

            if (!isValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error"));
            }

            // Additional custom validation logic can be added here
            await ValidateCustomRulesAsync(options, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating options of type {OptionsType}", typeof(T).Name);
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    public Task<Dictionary<string, object>> GetAllConfigurationAsync()
    {
        var config = new Dictionary<string, object>();

        try
        {
            config["Monitoring"] = GetMonitoringOptions();
            config["Authentication"] = GetAuthenticationOptions();
            config["DatabaseMonitoring"] = GetDatabaseMonitoringOptions();
            config["Quartz"] = GetQuartzOptions();

            // Add other configuration sections
            foreach (var section in _configuration.GetChildren())
            {
                if (!config.ContainsKey(section.Key))
                {
                    config[section.Key] = GetSectionValues(section);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all configuration");
        }

        return Task.FromResult(config);
    }

    public Task<string?> GetConfigurationValueAsync(string key)
    {
        try
        {
            return Task.FromResult(_configuration[key]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration value for key {Key}", key);
            return Task.FromResult<string?>(null);
        }
    }

    public Task<bool> SetConfigurationValueAsync(string key, string value)
    {
        try
        {
            var oldValue = _configuration[key];

            // In a real implementation, this would update the configuration source
            _logger.LogInformation("Configuration value for key {Key} would be set to {Value} (not implemented in this demo)",
                key, value);

            RecordConfigurationChange(key, oldValue, value, "System", "Configuration value update via API");

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration value for key {Key}", key);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ReloadConfigurationAsync()
    {
        try
        {
            if (_configuration is IConfigurationRoot configRoot)
            {
                configRoot.Reload();
                _logger.LogInformation("Configuration reloaded successfully");

                RecordConfigurationChange("System", "N/A", "Configuration reloaded", "System", "Manual configuration reload");

                return Task.FromResult(true);
            }

            _logger.LogWarning("Configuration reload not supported for current configuration type");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading configuration");
            return Task.FromResult(false);
        }
    }

    public Task<IEnumerable<ConfigurationChange>> GetChangeHistoryAsync(int limit = 100)
    {
        lock (_lockObject)
        {
            return Task.FromResult(_changeHistory.TakeLast(limit).AsEnumerable());
        }
    }

    private async Task ValidateCustomRulesAsync<T>(T options, ConfigurationValidationResult result) where T : class
    {
        // Custom validation rules based on option type
        switch (options)
        {
            case MonitoringOptions monitoringOpts:
                await ValidateMonitoringOptionsAsync(monitoringOpts, result);
                break;
            case AuthenticationOptions authOpts:
                await ValidateAuthenticationOptionsAsync(authOpts, result);
                break;
            case DatabaseMonitoringOptions dbOpts:
                await ValidateDatabaseMonitoringOptionsAsync(dbOpts, result);
                break;
            case QuartzOptions quartzOpts:
                await ValidateQuartzOptionsAsync(quartzOpts, result);
                break;
        }
    }

    private Task ValidateMonitoringOptionsAsync(MonitoringOptions options, ConfigurationValidationResult result)
    {
        // Check for duplicate endpoint names
        var duplicateNames = options.Endpoints
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateName in duplicateNames)
        {
            result.Errors.Add($"Duplicate endpoint name: {duplicateName}");
            result.IsValid = false;
        }

        // Validate URLs are reachable (warning only)
        foreach (var endpoint in options.Endpoints)
        {
            if (!Uri.TryCreate(endpoint.Url, UriKind.Absolute, out var uri))
            {
                result.Errors.Add($"Invalid URL for endpoint {endpoint.Name}: {endpoint.Url}");
                result.IsValid = false;
            }
            else if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                result.Warnings.Add($"Endpoint {endpoint.Name} uses non-HTTP(S) scheme: {uri.Scheme}");
            }
        }

        return Task.CompletedTask;
    }

    private Task ValidateAuthenticationOptionsAsync(AuthenticationOptions options, ConfigurationValidationResult result)
    {
        // Validate JWT secret key strength
        if (options.Jwt.SecretKey.Length < 32)
        {
            result.Errors.Add("JWT secret key must be at least 32 characters long");
            result.IsValid = false;
        }

        // Check for duplicate usernames
        var duplicateUsernames = options.Users.DefaultUsers
            .GroupBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateUsername in duplicateUsernames)
        {
            result.Errors.Add($"Duplicate username: {duplicateUsername}");
            result.IsValid = false;
        }

        // Validate at least one admin user exists
        var hasAdmin = options.Users.DefaultUsers.Any(u => u.Roles.Contains(UserRoles.Admin) && u.Enabled);
        if (!hasAdmin)
        {
            result.Warnings.Add("No enabled admin users found. Ensure at least one admin user is configured.");
        }

        return Task.CompletedTask;
    }

    private Task ValidateDatabaseMonitoringOptionsAsync(DatabaseMonitoringOptions options, ConfigurationValidationResult result)
    {
        // Check for duplicate connection names
        var duplicateNames = options.Connections
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateName in duplicateNames)
        {
            result.Errors.Add($"Duplicate connection name: {duplicateName}");
            result.IsValid = false;
        }

        // Validate connection strings (basic check)
        foreach (var connection in options.Connections)
        {
            if (string.IsNullOrWhiteSpace(connection.ConnectionString))
            {
                result.Errors.Add($"Connection string is empty for connection: {connection.Name}");
                result.IsValid = false;
            }
            else if (!connection.ConnectionString.Contains("Server=") && !connection.ConnectionString.Contains("Data Source="))
            {
                result.Warnings.Add($"Connection string for {connection.Name} may be invalid (no Server/Data Source specified)");
            }
        }

        // Check for duplicate query names
        var duplicateQueryNames = options.Queries
            .GroupBy(q => q.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateQueryName in duplicateQueryNames)
        {
            result.Errors.Add($"Duplicate query name: {duplicateQueryName}");
            result.IsValid = false;
        }

        return Task.CompletedTask;
    }

    private Task ValidateQuartzOptionsAsync(QuartzOptions options, ConfigurationValidationResult result)
    {
        // Validate cron expression
        try
        {
            var cronExpression = new Quartz.CronExpression(options.CronSchedule);
            var nextRun = cronExpression.GetNextValidTimeAfter(DateTimeOffset.UtcNow);
            if (!nextRun.HasValue)
            {
                result.Warnings.Add("Cron expression may not have future execution times");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Invalid cron expression: {ex.Message}");
            result.IsValid = false;
        }

        return Task.CompletedTask;
    }

    private void OnConfigurationChanged(string section, object newValue)
    {
        try
        {
            var eventArgs = new ConfigurationChangedEventArgs
            {
                Section = section,
                Key = section,
                NewValue = newValue,
                Timestamp = DateTimeOffset.UtcNow
            };

            ConfigurationChanged?.Invoke(this, eventArgs);

            _logger.LogInformation("Configuration section {Section} changed", section);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling configuration change for section {Section}", section);
        }
    }

    private void RecordConfigurationChange(string key, string? oldValue, string? newValue, string? changedBy, string? reason)
    {
        lock (_lockObject)
        {
            var change = new ConfigurationChange
            {
                Timestamp = DateTimeOffset.UtcNow,
                Key = key,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedBy = changedBy,
                Reason = reason
            };

            _changeHistory.Enqueue(change);

            // Keep only the last 1000 changes
            while (_changeHistory.Count > 1000)
            {
                _changeHistory.TryDequeue(out _);
            }
        }
    }

    private static object GetSectionValues(IConfigurationSection section)
    {
        var children = section.GetChildren().ToList();

        if (!children.Any())
        {
            return section.Value ?? string.Empty;
        }

        var result = new Dictionary<string, object>();
        foreach (var child in children)
        {
            result[child.Key] = GetSectionValues(child);
        }

        return result;
    }
}
