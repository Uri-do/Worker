using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;

namespace MonitoringWorker.Services;

/// <summary>
/// Implementation of configuration validation service
/// </summary>
public class ConfigurationValidationService : IConfigurationValidationService
{
    private readonly ILogger<ConfigurationValidationService> _logger;
    private readonly IOptionsMonitor<MonitoringOptions> _monitoringOptions;
    private readonly IOptionsMonitor<DatabaseMonitoringOptions> _databaseOptions;
    private readonly IOptionsMonitor<AuthenticationOptions> _authOptions;

    public ConfigurationValidationService(
        ILogger<ConfigurationValidationService> logger,
        IOptionsMonitor<MonitoringOptions> monitoringOptions,
        IOptionsMonitor<DatabaseMonitoringOptions> databaseOptions,
        IOptionsMonitor<AuthenticationOptions> authOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitoringOptions = monitoringOptions ?? throw new ArgumentNullException(nameof(monitoringOptions));
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));
        _authOptions = authOptions ?? throw new ArgumentNullException(nameof(authOptions));
    }

    /// <summary>
    /// Validates monitoring configuration
    /// </summary>
    public async Task<ConfigurationValidationResult> ValidateMonitoringOptionsAsync(MonitoringOptions options)
    {
        var result = new ConfigurationValidationResult
        {
            Section = "Monitoring",
            IsValid = true
        };

        try
        {
            // Validate basic properties
            if (options.DefaultTimeoutSeconds <= 0)
            {
                result.Errors.Add("DefaultTimeoutSeconds must be greater than 0");
                result.IsValid = false;
            }

            if (options.DefaultTimeoutSeconds > 300)
            {
                result.Warnings.Add("DefaultTimeoutSeconds is very high (>300s), consider reducing for better responsiveness");
            }

            // Validate endpoints
            if (options.Endpoints?.Any() == true)
            {
                for (int i = 0; i < options.Endpoints.Count; i++)
                {
                    var endpoint = options.Endpoints[i];
                    
                    if (string.IsNullOrWhiteSpace(endpoint.Name))
                    {
                        result.Errors.Add($"Endpoint[{i}].Name is required");
                        result.IsValid = false;
                    }

                    if (string.IsNullOrWhiteSpace(endpoint.Url))
                    {
                        result.Errors.Add($"Endpoint[{i}].Url is required");
                        result.IsValid = false;
                    }
                    else if (!Uri.TryCreate(endpoint.Url, UriKind.Absolute, out var uri))
                    {
                        result.Errors.Add($"Endpoint[{i}].Url is not a valid URL: {endpoint.Url}");
                        result.IsValid = false;
                    }
                    else
                    {
                        // Test endpoint connectivity (async)
                        try
                        {
                            var ping = new Ping();
                            var reply = await ping.SendPingAsync(uri.Host, 5000);
                            if (reply.Status != IPStatus.Success)
                            {
                                result.Warnings.Add($"Endpoint[{i}] host {uri.Host} is not reachable: {reply.Status}");
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Warnings.Add($"Could not test connectivity to Endpoint[{i}] host {uri.Host}: {ex.Message}");
                        }
                    }

                    if (endpoint.TimeoutSeconds <= 0)
                    {
                        result.Errors.Add($"Endpoint[{i}].TimeoutSeconds must be greater than 0");
                        result.IsValid = false;
                    }
                }

                // Check for duplicate endpoint names
                var duplicateNames = options.Endpoints
                    .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var duplicateName in duplicateNames)
                {
                    result.Errors.Add($"Duplicate endpoint name found: {duplicateName}");
                    result.IsValid = false;
                }
            }
            else
            {
                result.Warnings.Add("No endpoints configured for monitoring");
            }

            result.Details["EndpointCount"] = options.Endpoints?.Count ?? 0;
            result.Details["DefaultTimeout"] = options.DefaultTimeoutSeconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating monitoring configuration");
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Validates database monitoring configuration
    /// </summary>
    public Task<ConfigurationValidationResult> ValidateDatabaseMonitoringOptionsAsync(DatabaseMonitoringOptions options)
    {
        var result = new ConfigurationValidationResult
        {
            Section = "DatabaseMonitoring",
            IsValid = true
        };

        try
        {
            // Validate basic properties
            if (options.MaxConcurrentConnections <= 0)
            {
                result.Errors.Add("MaxConcurrentConnections must be greater than 0");
                result.IsValid = false;
            }

            if (options.MaxConcurrentConnections > 100)
            {
                result.Warnings.Add("MaxConcurrentConnections is very high (>100), consider reducing to avoid resource exhaustion");
            }

            // Validate connections
            if (options.Connections?.Any() == true)
            {
                for (int i = 0; i < options.Connections.Count; i++)
                {
                    var connection = options.Connections[i];
                    
                    if (string.IsNullOrWhiteSpace(connection.Name))
                    {
                        result.Errors.Add($"Connection[{i}].Name is required");
                        result.IsValid = false;
                    }

                    if (string.IsNullOrWhiteSpace(connection.ConnectionString))
                    {
                        result.Errors.Add($"Connection[{i}].ConnectionString is required");
                        result.IsValid = false;
                    }

                    if (string.IsNullOrWhiteSpace(connection.Provider))
                    {
                        result.Errors.Add($"Connection[{i}].Provider is required");
                        result.IsValid = false;
                    }

                    if (connection.ConnectionTimeoutSeconds.HasValue && connection.ConnectionTimeoutSeconds <= 0)
                    {
                        result.Errors.Add($"Connection[{i}].ConnectionTimeoutSeconds must be greater than 0 if specified");
                        result.IsValid = false;
                    }
                }

                // Check for duplicate connection names
                var duplicateNames = options.Connections
                    .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var duplicateName in duplicateNames)
                {
                    result.Errors.Add($"Duplicate connection name found: {duplicateName}");
                    result.IsValid = false;
                }
            }
            else if (options.Enabled)
            {
                result.Warnings.Add("Database monitoring is enabled but no connections are configured");
            }

            // Validate queries
            if (options.Queries?.Any() == true)
            {
                for (int i = 0; i < options.Queries.Count; i++)
                {
                    var query = options.Queries[i];
                    
                    if (string.IsNullOrWhiteSpace(query.Name))
                    {
                        result.Errors.Add($"Query[{i}].Name is required");
                        result.IsValid = false;
                    }

                    if (string.IsNullOrWhiteSpace(query.Sql))
                    {
                        result.Errors.Add($"Query[{i}].Sql is required");
                        result.IsValid = false;
                    }

                    if (query.TimeoutSeconds <= 0)
                    {
                        result.Errors.Add($"Query[{i}].TimeoutSeconds must be greater than 0");
                        result.IsValid = false;
                    }
                }
            }

            result.Details["ConnectionCount"] = options.Connections?.Count ?? 0;
            result.Details["QueryCount"] = options.Queries?.Count ?? 0;
            result.Details["MaxConcurrentConnections"] = options.MaxConcurrentConnections;
            result.Details["Enabled"] = options.Enabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating database monitoring configuration");
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Validates authentication configuration
    /// </summary>
    public Task<ConfigurationValidationResult> ValidateAuthenticationOptionsAsync(AuthenticationOptions options)
    {
        var result = new ConfigurationValidationResult
        {
            Section = "Authentication",
            IsValid = true
        };

        try
        {
            if (options.Enabled)
            {
                // Validate JWT settings
                if (options.Jwt == null)
                {
                    result.Errors.Add("JWT configuration is required when authentication is enabled");
                    result.IsValid = false;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(options.Jwt.SecretKey))
                    {
                        result.Errors.Add("JWT.SecretKey is required");
                        result.IsValid = false;
                    }
                    else if (options.Jwt.SecretKey.Length < 32)
                    {
                        result.Errors.Add("JWT.SecretKey must be at least 32 characters long for security");
                        result.IsValid = false;
                    }

                    if (string.IsNullOrWhiteSpace(options.Jwt.Issuer))
                    {
                        result.Errors.Add("JWT.Issuer is required");
                        result.IsValid = false;
                    }

                    if (string.IsNullOrWhiteSpace(options.Jwt.Audience))
                    {
                        result.Errors.Add("JWT.Audience is required");
                        result.IsValid = false;
                    }

                    if (options.Jwt.ExpirationMinutes <= 0)
                    {
                        result.Errors.Add("JWT.ExpirationMinutes must be greater than 0");
                        result.IsValid = false;
                    }

                    if (options.Jwt.ExpirationMinutes > 1440) // 24 hours
                    {
                        result.Warnings.Add("JWT.ExpirationMinutes is very high (>24h), consider reducing for better security");
                    }
                }

                // Validate users
                if (options.Users?.DefaultUsers?.Any() != true)
                {
                    result.Warnings.Add("No users configured, authentication will not work");
                }
                else
                {
                    for (int i = 0; i < options.Users.DefaultUsers.Count; i++)
                    {
                        var user = options.Users.DefaultUsers[i];
                        
                        if (string.IsNullOrWhiteSpace(user.Username))
                        {
                            result.Errors.Add($"User[{i}].Username is required");
                            result.IsValid = false;
                        }

                        if (string.IsNullOrWhiteSpace(user.Password))
                        {
                            result.Errors.Add($"User[{i}].Password is required");
                            result.IsValid = false;
                        }

                        if (user.Roles?.Any() != true)
                        {
                            result.Warnings.Add($"User[{i}] ({user.Username}) has no roles assigned");
                        }
                    }
                }
            }

            result.Details["Enabled"] = options.Enabled;
            result.Details["UserCount"] = options.Users?.DefaultUsers?.Count ?? 0;
            result.Details["JwtExpirationMinutes"] = options.Jwt?.ExpirationMinutes ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating authentication configuration");
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Validates all configuration sections
    /// </summary>
    public async Task<ConfigurationValidationResult> ValidateAllConfigurationAsync()
    {
        var overallResult = new ConfigurationValidationResult
        {
            Section = "All",
            IsValid = true
        };

        try
        {
            var monitoringResult = await ValidateMonitoringOptionsAsync(_monitoringOptions.CurrentValue);
            var databaseResult = await ValidateDatabaseMonitoringOptionsAsync(_databaseOptions.CurrentValue);
            var authResult = await ValidateAuthenticationOptionsAsync(_authOptions.CurrentValue);

            // Combine results
            overallResult.Errors.AddRange(monitoringResult.Errors);
            overallResult.Errors.AddRange(databaseResult.Errors);
            overallResult.Errors.AddRange(authResult.Errors);

            overallResult.Warnings.AddRange(monitoringResult.Warnings);
            overallResult.Warnings.AddRange(databaseResult.Warnings);
            overallResult.Warnings.AddRange(authResult.Warnings);

            overallResult.IsValid = monitoringResult.IsValid && databaseResult.IsValid && authResult.IsValid;

            // Add summary details
            overallResult.Details["MonitoringValid"] = monitoringResult.IsValid;
            overallResult.Details["DatabaseMonitoringValid"] = databaseResult.IsValid;
            overallResult.Details["AuthenticationValid"] = authResult.IsValid;
            overallResult.Details["TotalErrors"] = overallResult.Errors.Count;
            overallResult.Details["TotalWarnings"] = overallResult.Warnings.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating all configuration");
            overallResult.Errors.Add($"Overall validation error: {ex.Message}");
            overallResult.IsValid = false;
        }

        return overallResult;
    }
}
