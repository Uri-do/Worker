using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MonitoringWorker;
using MonitoringWorker.Configuration;
using MonitoringWorker.HealthChecks;
using MonitoringWorker.Hubs;
using MonitoringWorker.Jobs;
using MonitoringWorker.Middleware;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;
using Quartz;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Enhanced Serilog configuration
builder.Host.ConfigureLogging(builder.Configuration);

// Configuration with validation
builder.Services.AddOptions<MonitoringOptions>()
    .Bind(builder.Configuration.GetSection(MonitoringOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<MonitoringWorker.Configuration.QuartzOptions>()
    .Bind(builder.Configuration.GetSection(MonitoringWorker.Configuration.QuartzOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(AuthenticationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DatabaseMonitoringOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseMonitoringOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Enhanced monitoring configuration
builder.Services.AddMonitoringConfiguration(builder.Configuration);

// Core services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IEventNotificationService, EventNotificationService>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<IConfigurationValidationService, ConfigurationValidationService>();
builder.Services.AddSingleton<IFeatureToggleService, FeatureToggleService>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IDatabaseMonitoringService, DatabaseMonitoringService>();

// OpenTelemetry configuration
builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder
            .AddMeter("MonitoringWorker.Metrics")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();
    })
    .WithTracing(builder =>
    {
        builder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation();
    });

// HTTP clients with resilience policies
builder.Services.AddHttpClient<IMonitoringService, MonitoringService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MonitoringWorker/1.0");
    client.Timeout = TimeSpan.FromMinutes(2); // Global timeout
})
.AddTransientHttpErrorPolicy(policy => 
    policy.WaitAndRetryAsync(
        3, 
        retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Log.Warning("HTTP retry {RetryCount} after {Delay}ms",
                retryCount, timespan.TotalMilliseconds);
        }))
.AddTransientHttpErrorPolicy(policy =>
    policy.CircuitBreakerAsync(
        5,
        TimeSpan.FromSeconds(30),
        onBreak: (result, timespan) => Log.Warning("Circuit breaker opened for {Timespan}s", timespan.TotalSeconds),
        onReset: () => Log.Information("Circuit breaker reset")));

// Quartz configuration
builder.Services.AddQuartz(q =>
{
    
    var jobKey = new JobKey("monitoring-job");
    q.AddJob<MonitoringJob>(opts => opts
        .WithIdentity(jobKey)
        .StoreDurably()
        .WithDescription("Scheduled monitoring job that checks configured endpoints"));
    
    var cronSchedule = builder.Configuration["Quartz:CronSchedule"] ?? "0 0/1 * * * ?";
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("monitoring-trigger")
        .WithDescription($"Trigger for monitoring job with schedule: {cronSchedule}")
        .WithCronSchedule(cronSchedule)
        .StartNow());
});

var quartzOptions = builder.Configuration.GetSection(MonitoringWorker.Configuration.QuartzOptions.SectionName).Get<MonitoringWorker.Configuration.QuartzOptions>() ?? new MonitoringWorker.Configuration.QuartzOptions();
builder.Services.AddQuartzHostedService(q =>
{
    q.WaitForJobsToComplete = quartzOptions.WaitForJobsToComplete;
    q.AwaitApplicationStarted = true;
    q.StartDelay = TimeSpan.Zero; // Don't auto-start, wait for manual start
});

// Background worker service (commented out - now managed via API)
// builder.Services.AddHostedService<Worker>();

// Authentication
var authOptions = builder.Configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>();
if (authOptions?.Enabled == true)
{
    var key = Encoding.UTF8.GetBytes(authOptions.Jwt.SecretKey);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = authOptions.Jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = authOptions.Jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(authOptions.Jwt.ClockSkewMinutes)
            };

            // Configure JWT for SignalR
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/monitoringHub"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ViewMonitoring", policy =>
            policy.RequireClaim("permission", Permissions.ViewMonitoring));
        options.AddPolicy("ManageMonitoring", policy =>
            policy.RequireClaim("permission", Permissions.ManageMonitoring));
        options.AddPolicy("ViewMetrics", policy =>
            policy.RequireClaim("permission", Permissions.ViewMetrics));
        options.AddPolicy("ManageConfiguration", policy =>
            policy.RequireClaim("permission", Permissions.ManageConfiguration));
    });
}

// Web host for SignalR and health checks
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MonitoringWorker API",
        Version = "v1",
        Description = "API for managing monitoring worker and endpoints",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "MonitoringWorker",
            Email = "support@monitoringworker.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT authentication to Swagger
    if (authOptions?.Enabled == true)
    {
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    }
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<LivenessHealthCheck>("liveness",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "live" })
    .AddCheck<ReadinessHealthCheck>("readiness",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" })
    .AddCheck<ConfigurationHealthCheck>("configuration",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "config" });

// CORS for SignalR (configure for production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // Configure for production - replace with your actual domains
            policy.WithOrigins("https://yourdomain.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Configure URLs
builder.WebHost.UseUrls("http://localhost:5002", "https://localhost:5003");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MonitoringWorker API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MonitoringWorker API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();
app.UseCors("SignalRPolicy");

// API response middleware for consistent formatting
app.UseMiddleware<ApiResponseMiddleware>();

// Authentication and authorization
if (authOptions?.Enabled == true)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Serve static files
app.UseStaticFiles();

// Controllers
app.MapControllers();

// Default route - redirect to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// API info endpoint
app.MapGet("/api", () => new
{
    Name = "MonitoringWorker API",
    Version = "1.0.0",
    Description = "API for managing monitoring worker and endpoints",
    Endpoints = new
    {
        Swagger = "/swagger",
        Health = "/healthz",
        SignalR = "/monitoringHub",
        API = "/api"
    },
    Status = "Running",
    Timestamp = DateTime.UtcNow
}).WithTags("Info");

// SignalR hub
app.MapHub<MonitoringHub>("/monitoringHub");

// Health check endpoints
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration,
                description = e.Value.Description,
                data = e.Value.Data
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration,
                description = e.Value.Description,
                data = e.Value.Data
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Combined health check endpoint
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration,
                description = e.Value.Description,
                tags = e.Value.Tags,
                data = e.Value.Data
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting MonitoringWorker application");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("Application started at: {StartTime}", DateTimeOffset.UtcNow);

// Setup graceful shutdown
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    logger.LogInformation("Shutdown requested by user (Ctrl+C)");
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await app.RunAsync(cts.Token);
}
catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
{
    logger.LogInformation("Application shutdown gracefully");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    logger.LogInformation("Application stopped at: {StopTime}", DateTimeOffset.UtcNow);

    // Ensure all background services are stopped
    var lifetime = app.Services.GetService<IHostApplicationLifetime>();
    if (lifetime != null && !lifetime.ApplicationStopping.IsCancellationRequested)
    {
        lifetime.StopApplication();
    }

    Log.CloseAndFlush();

    // Small delay to ensure cleanup
    await Task.Delay(1000);
}

// Make Program class accessible for testing
public partial class Program { }
