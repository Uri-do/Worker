using FluentAssertions;
using Microsoft.Extensions.Logging;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using Moq;
using System.Diagnostics.Metrics;
using Xunit;

namespace MonitoringWorker.Tests.Services;

public class MetricsServiceTests
{
    private readonly Mock<ILogger<MetricsService>> _loggerMock;
    private readonly MetricsService _service;

    public MetricsServiceTests()
    {
        _loggerMock = new Mock<ILogger<MetricsService>>();
        _service = new MetricsService(_loggerMock.Object);
    }

    [Fact]
    public void RecordHeartbeat_IncrementsHeartbeatCounter()
    {
        // Act
        _service.RecordHeartbeat();
        _service.RecordHeartbeat();

        // Assert
        var metrics = _service.GetMetrics();
        metrics["worker.heartbeat"].Should().Be(2);
    }

    [Fact]
    public void RecordJobStart_IncrementsJobStartedCounter()
    {
        // Act
        _service.RecordJobStart();

        // Assert
        var metrics = _service.GetMetrics();
        metrics["job.started"].Should().Be(1);
    }

    [Fact]
    public void RecordJobSuccess_IncrementsJobCompletedCounter()
    {
        // Act
        _service.RecordJobSuccess();

        // Assert
        var metrics = _service.GetMetrics();
        metrics["job.completed"].Should().Be(1);
    }

    [Fact]
    public void RecordJobFailure_IncrementsJobFailedCounter()
    {
        // Act
        _service.RecordJobFailure();

        // Assert
        var metrics = _service.GetMetrics();
        metrics["job.failed"].Should().Be(1);
    }

    [Fact]
    public void RecordJobCancellation_IncrementsJobCancelledCounter()
    {
        // Act
        _service.RecordJobCancellation();

        // Assert
        var metrics = _service.GetMetrics();
        metrics["job.cancelled"].Should().Be(1);
    }

    [Fact]
    public void RecordCheckResult_IncrementsCheckCounters()
    {
        // Act
        _service.RecordCheckResult("TestCheck", MonitoringStatus.Healthy, 100);
        _service.RecordCheckResult("TestCheck", MonitoringStatus.Unhealthy, 200);

        // Assert
        var metrics = _service.GetMetrics();
        metrics["check.testcheck.healthy"].Should().Be(1);
        metrics["check.testcheck.unhealthy"].Should().Be(1);
        metrics["check.testcheck.total"].Should().Be(2);
    }

    [Fact]
    public void RecordCheckResult_RecordsDurationMetrics()
    {
        // Act
        _service.RecordCheckResult("TestCheck", MonitoringStatus.Healthy, 100);
        _service.RecordCheckResult("TestCheck", MonitoringStatus.Healthy, 200);

        // Assert
        var metrics = _service.GetMetrics();
        metrics["check.testcheck.duration.avg"].Should().Be(150); // (100 + 200) / 2
        metrics["check.testcheck.duration.count"].Should().Be(2);
    }

    [Fact]
    public void RecordCheckResult_WithNullCheckName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _service.RecordCheckResult(null!, MonitoringStatus.Healthy, 100));
    }

    [Fact]
    public void RecordCheckResult_WithEmptyCheckName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _service.RecordCheckResult("", MonitoringStatus.Healthy, 100));
    }

    [Fact]
    public void RecordCheckResult_SanitizesCheckName()
    {
        // Act
        _service.RecordCheckResult("Test-Check_With.Special@Characters!", MonitoringStatus.Healthy, 100);

        // Assert
        var metrics = _service.GetMetrics();
        metrics.Should().ContainKey("check.test-check_withspecialcharacters.healthy");
        metrics.Should().ContainKey("check.test-check_withspecialcharacters.total");
    }

    [Fact]
    public void GetMetrics_ReturnsAllMetrics()
    {
        // Arrange
        _service.RecordHeartbeat();
        _service.RecordJobStart();
        _service.RecordJobSuccess();
        _service.RecordCheckResult("TestCheck", MonitoringStatus.Healthy, 100);

        // Act
        var metrics = _service.GetMetrics();

        // Assert
        metrics.Should().ContainKey("worker.heartbeat");
        metrics.Should().ContainKey("job.started");
        metrics.Should().ContainKey("job.completed");
        metrics.Should().ContainKey("check.testcheck.healthy");
        metrics.Should().ContainKey("check.testcheck.total");
        metrics.Should().ContainKey("check.testcheck.duration.avg");
        metrics.Should().ContainKey("check.testcheck.duration.count");
    }

    [Fact]
    public void Reset_ClearsAllMetrics()
    {
        // Arrange
        _service.RecordHeartbeat();
        _service.RecordJobStart();
        _service.RecordCheckResult("TestCheck", MonitoringStatus.Healthy, 100);

        // Act
        _service.Reset();

        // Assert
        var metrics = _service.GetMetrics();
        metrics["worker.heartbeat"].Should().Be(0);
        metrics["job.started"].Should().Be(0);
        metrics["job.completed"].Should().Be(0);
        metrics["job.failed"].Should().Be(0);
        metrics["job.cancelled"].Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MetricsService(null!));
    }

    [Theory]
    [InlineData(MonitoringStatus.Healthy)]
    [InlineData(MonitoringStatus.Unhealthy)]
    [InlineData(MonitoringStatus.Error)]
    [InlineData(MonitoringStatus.Unknown)]
    public void RecordCheckResult_WithDifferentStatuses_RecordsCorrectly(MonitoringStatus status)
    {
        // Act
        _service.RecordCheckResult("TestCheck", status, 100);

        // Assert
        var metrics = _service.GetMetrics();
        var statusKey = $"check.testcheck.{status.ToString().ToLowerInvariant()}";
        metrics[statusKey].Should().Be(1);
        metrics["check.testcheck.total"].Should().Be(1);
    }

    [Fact]
    public void RecordCheckResult_WithMultipleChecks_TracksIndependently()
    {
        // Act
        _service.RecordCheckResult("Check1", MonitoringStatus.Healthy, 100);
        _service.RecordCheckResult("Check2", MonitoringStatus.Unhealthy, 200);
        _service.RecordCheckResult("Check1", MonitoringStatus.Error, 150);

        // Assert
        var metrics = _service.GetMetrics();
        metrics["check.check1.healthy"].Should().Be(1);
        metrics["check.check1.error"].Should().Be(1);
        metrics["check.check1.total"].Should().Be(2);
        metrics["check.check2.unhealthy"].Should().Be(1);
        metrics["check.check2.total"].Should().Be(1);
    }

    [Fact]
    public void GetMonitoringMetrics_ReturnsCorrectStructure()
    {
        // Arrange
        _service.RecordHeartbeat();
        _service.RecordJobStart();
        _service.RecordJobSuccess();
        _service.RecordCheckResult("TestCheck", MonitoringStatus.Healthy, 100);
        _service.RecordCheckResult("TestCheck", MonitoringStatus.Unhealthy, 200);

        // Act
        var monitoringMetrics = _service.GetMonitoringMetrics();

        // Assert
        monitoringMetrics.Should().NotBeNull();
        monitoringMetrics.TotalJobs.Should().Be(1);
        monitoringMetrics.SuccessfulJobs.Should().Be(1);
        monitoringMetrics.FailedJobs.Should().Be(0);
        monitoringMetrics.LastHeartbeat.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        monitoringMetrics.Uptime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void GetCurrentMetrics_ReturnsStructuredData()
    {
        // Arrange
        _service.RecordHeartbeat();
        _service.RecordJobStart();
        _service.RecordCheckResult("TestCheck", MonitoringStatus.Healthy, 150);

        // Act
        var currentMetrics = _service.GetCurrentMetrics();

        // Assert
        currentMetrics.Should().NotBeNull();

        // Use reflection to check the anonymous object structure
        var metricsType = currentMetrics.GetType();
        var countersProperty = metricsType.GetProperty("Counters");
        var summaryProperty = metricsType.GetProperty("Summary");
        var timestampProperty = metricsType.GetProperty("Timestamp");

        countersProperty.Should().NotBeNull();
        summaryProperty.Should().NotBeNull();
        timestampProperty.Should().NotBeNull();

        var timestamp = (DateTime)timestampProperty!.GetValue(currentMetrics)!;
        timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GetMonitoringHistory_ReturnsHistoryStructure()
    {
        // Arrange
        var timeSpan = TimeSpan.FromHours(1);
        _service.RecordHeartbeat();

        // Act
        var history = _service.GetMonitoringHistory(timeSpan);

        // Assert
        history.Should().NotBeNull();

        // Use reflection to check the anonymous object structure
        var historyType = history.GetType();
        var timeSpanProperty = historyType.GetProperty("TimeSpan");
        var startTimeProperty = historyType.GetProperty("StartTime");
        var endTimeProperty = historyType.GetProperty("EndTime");
        var currentMetricsProperty = historyType.GetProperty("CurrentMetrics");

        timeSpanProperty.Should().NotBeNull();
        startTimeProperty.Should().NotBeNull();
        endTimeProperty.Should().NotBeNull();
        currentMetricsProperty.Should().NotBeNull();

        var returnedTimeSpan = (TimeSpan)timeSpanProperty!.GetValue(history)!;
        returnedTimeSpan.Should().Be(timeSpan);
    }
}
