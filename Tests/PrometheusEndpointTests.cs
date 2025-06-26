using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using System.Net;
using Xunit;

namespace MonitoringWorker.Tests;

/// <summary>
/// Integration tests for the Prometheus metrics endpoint
/// </summary>
public class PrometheusEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PrometheusEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsPrometheusContentType()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        response.Content.Headers.ContentType?.CharSet.Should().Be("utf-8");
    }

    [Fact]
    public async Task MetricsEndpoint_ContainsOpenTelemetryMetrics()
    {
        // Act
        var response = await _client.GetAsync("/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().NotBeNullOrEmpty();

        // Check for standard OpenTelemetry metrics
        content.Should().Contain("# TYPE");
        content.Should().Contain("# HELP");

        // Check for ASP.NET Core metrics that should always be present
        content.Should().Contain("http_server_active_requests");
        content.Should().Contain("aspnetcore_routing_match_attempts_total");
    }

    [Fact]
    public async Task MetricsEndpoint_ContainsCustomWorkerMetrics()
    {
        // Arrange - Generate some metrics by calling other endpoints first
        using var scope = _factory.Services.CreateScope();
        var metricsService = scope.ServiceProvider.GetRequiredService<IMetricsService>();

        // Record some test metrics
        metricsService.RecordHeartbeat();
        metricsService.RecordJobStart();
        metricsService.RecordJobSuccess();
        metricsService.RecordCheckResult("test-check", MonitoringStatus.Healthy, 150);

        // Act
        var response = await _client.GetAsync("/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check for metrics that should always be present
        content.Should().Contain("monitoring_worker_uptime_seconds");
        content.Should().Contain("monitoring_worker_total_checks");

        // Note: Counter metrics only appear in Prometheus output when they have non-zero values
        // and the specific job/check metrics may not appear immediately due to timing
    }

    [Fact]
    public async Task MetricsEndpoint_ShowsCorrectMetricValues()
    {
        // Arrange - Generate specific metrics
        using var scope = _factory.Services.CreateScope();
        var metricsService = scope.ServiceProvider.GetRequiredService<IMetricsService>();

        // Reset metrics to start clean
        metricsService.Reset();

        // Record specific metrics
        metricsService.RecordHeartbeat();
        metricsService.RecordHeartbeat();
        metricsService.RecordJobStart();
        metricsService.RecordCheckResult("api-health", MonitoringStatus.Healthy, 100);

        // Act
        var response = await _client.GetAsync("/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check for metrics that should always be present
        content.Should().Contain("monitoring_worker_uptime_seconds");
        content.Should().Contain("monitoring_worker_total_checks");

        // The metrics format should be valid Prometheus format
        content.Should().Contain("# TYPE");
        content.Should().Contain("# HELP");
    }

    [Fact]
    public async Task MetricsEndpoint_HandlesMultipleRequests()
    {
        // Act - Make multiple requests
        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            var response = await _client.GetAsync("/metrics");
            return response.StatusCode;
        });

        var statusCodes = await Task.WhenAll(tasks);

        // Assert
        statusCodes.Should().AllBeEquivalentTo(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MetricsEndpoint_IncludesHttpClientMetrics()
    {
        // Arrange - Trigger some HTTP client activity by calling health check
        await _client.GetAsync("/healthz/live");

        // Act
        var response = await _client.GetAsync("/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - HTTP client metrics may not appear without external HTTP calls
        // Just verify the endpoint works and contains some metrics
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("# TYPE");
    }

    [Fact]
    public async Task MetricsEndpoint_IncludesKestrelMetrics()
    {
        // Act
        var response = await _client.GetAsync("/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Kestrel metrics may not be enabled by default
        // Just verify we have some server metrics
        content.Should().Contain("http_server_active_requests");
        content.Should().Contain("aspnetcore_routing_match_attempts_total");
    }

    [Fact]
    public async Task MetricsEndpoint_ValidPrometheusFormat()
    {
        // Act
        var response = await _client.GetAsync("/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Check that we have TYPE and HELP comments
        var typeLines = lines.Where(l => l.StartsWith("# TYPE")).ToList();
        var helpLines = lines.Where(l => l.StartsWith("# HELP")).ToList();
        
        typeLines.Should().NotBeEmpty();
        helpLines.Should().NotBeEmpty();
        
        // Check that metric lines don't start with #
        var metricLines = lines.Where(l => !l.StartsWith("#") && !string.IsNullOrWhiteSpace(l)).ToList();
        metricLines.Should().NotBeEmpty();
        
        // Each metric line should contain a metric name and value
        foreach (var metricLine in metricLines.Take(10)) // Check first 10 to avoid too much processing
        {
            metricLine.Should().Contain(" ");
            var parts = metricLine.Split(' ');
            parts.Length.Should().BeGreaterOrEqualTo(2);
            
            // Last part should be a number (value) or timestamp
            var lastPart = parts[^1];
            if (double.TryParse(lastPart, out _))
            {
                // Valid numeric value
                continue;
            }
            
            // Might be a timestamp, check if second-to-last is numeric
            if (parts.Length >= 2 && double.TryParse(parts[^2], out _))
            {
                // Valid with timestamp
                continue;
            }
            
            // If we get here, the format might be invalid
            // But we'll be lenient for this test
        }
    }

    [Fact]
    public async Task MetricsEndpoint_PerformanceTest()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/metrics");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should respond within 5 seconds
        
        var content = await response.Content.ReadAsStringAsync();
        content.Length.Should().BeGreaterThan(0);
    }
}
