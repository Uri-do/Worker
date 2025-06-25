using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace MonitoringWorker.Configuration;

/// <summary>
/// Enhanced logging configuration
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configures Serilog with enhanced settings
    /// </summary>
    public static void ConfigureLogging(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "MonitoringWorker")
                .Enrich.WithProperty("Version", GetApplicationVersion());

            // Configure minimum log levels
            loggerConfiguration.MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Quartz", LogEventLevel.Information);

            // Console logging with different formats for dev/prod
            if (context.HostingEnvironment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
            }
            else
            {
                loggerConfiguration.WriteTo.Console(new JsonFormatter());
            }

            // File logging with rolling
            var logPath = configuration["Logging:FilePath"] ?? "logs/monitoringworker-.log";
            loggerConfiguration.WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                formatter: new JsonFormatter(),
                restrictedToMinimumLevel: LogEventLevel.Information);

            // Error file logging
            var errorLogPath = configuration["Logging:ErrorFilePath"] ?? "logs/errors/monitoringworker-errors-.log";
            loggerConfiguration.WriteTo.File(
                path: errorLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                formatter: new JsonFormatter(),
                restrictedToMinimumLevel: LogEventLevel.Error);

            // Performance logging
            var performanceLogPath = configuration["Logging:PerformanceFilePath"] ?? "logs/performance/monitoringworker-performance-.log";
            loggerConfiguration.WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("PerformanceMetric"))
                .WriteTo.File(
                    path: performanceLogPath,
                    rollingInterval: RollingInterval.Hour,
                    retainedFileCountLimit: 168, // 7 days of hourly logs
                    formatter: new JsonFormatter()));

            // Additional sinks can be configured here when the appropriate packages are installed:
            // - Serilog.Sinks.MSSqlServer for database logging
            // - Serilog.Sinks.Seq for Seq logging
            // - Serilog.Sinks.ApplicationInsights for Application Insights logging
        });
    }

    /// <summary>
    /// Gets the application version
    /// </summary>
    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0.0";
        }
        catch
        {
            return "1.0.0.0";
        }
    }
}

/// <summary>
/// Performance logging extensions
/// </summary>
public static class PerformanceLoggingExtensions
{
    /// <summary>
    /// Logs performance metrics
    /// </summary>
    public static void LogPerformance(this Microsoft.Extensions.Logging.ILogger logger, string operation, long durationMs, Dictionary<string, object>? additionalData = null)
    {
        var logData = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["DurationMs"] = durationMs,
            ["PerformanceMetric"] = true
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                logData[kvp.Key] = kvp.Value;
            }
        }

        logger.LogInformation("Performance: {Operation} completed in {DurationMs}ms {@AdditionalData}",
            operation, durationMs, logData);
    }

    /// <summary>
    /// Logs database performance metrics
    /// </summary>
    public static void LogDatabasePerformance(this Microsoft.Extensions.Logging.ILogger logger, string operation, string connectionName, long durationMs, int? recordCount = null)
    {
        var additionalData = new Dictionary<string, object>
        {
            ["ConnectionName"] = connectionName,
            ["OperationType"] = "Database"
        };

        if (recordCount.HasValue)
        {
            additionalData["RecordCount"] = recordCount.Value;
        }

        logger.LogPerformance(operation, durationMs, additionalData);
    }

    /// <summary>
    /// Logs HTTP performance metrics
    /// </summary>
    public static void LogHttpPerformance(this Microsoft.Extensions.Logging.ILogger logger, string operation, string url, int statusCode, long durationMs)
    {
        var additionalData = new Dictionary<string, object>
        {
            ["Url"] = url,
            ["StatusCode"] = statusCode,
            ["OperationType"] = "HTTP"
        };

        logger.LogPerformance(operation, durationMs, additionalData);
    }

    /// <summary>
    /// Logs monitoring check performance
    /// </summary>
    public static void LogMonitoringPerformance(this Microsoft.Extensions.Logging.ILogger logger, string checkName, string checkType, bool success, long durationMs)
    {
        var additionalData = new Dictionary<string, object>
        {
            ["CheckName"] = checkName,
            ["CheckType"] = checkType,
            ["Success"] = success,
            ["OperationType"] = "Monitoring"
        };

        logger.LogPerformance($"MonitoringCheck_{checkName}", durationMs, additionalData);
    }
}
