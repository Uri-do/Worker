using FluentAssertions;
using Microsoft.Extensions.Logging;
using MonitoringWorker.Jobs;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using Moq;
using Quartz;
using Xunit;

namespace MonitoringWorker.Tests.Jobs;

public class MonitoringJobTests
{
    private readonly Mock<ILogger<MonitoringJob>> _loggerMock;
    private readonly Mock<IMonitoringService> _monitoringServiceMock;
    private readonly Mock<IEventNotificationService> _notificationServiceMock;
    private readonly Mock<IMetricsService> _metricsServiceMock;
    private readonly Mock<IJobExecutionContext> _contextMock;
    private readonly MonitoringJob _job;

    public MonitoringJobTests()
    {
        _loggerMock = new Mock<ILogger<MonitoringJob>>();
        _monitoringServiceMock = new Mock<IMonitoringService>();
        _notificationServiceMock = new Mock<IEventNotificationService>();
        _metricsServiceMock = new Mock<IMetricsService>();
        _contextMock = new Mock<IJobExecutionContext>();
        
        _contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        
        _job = new MonitoringJob(
            _loggerMock.Object,
            _monitoringServiceMock.Object,
            _notificationServiceMock.Object,
            _metricsServiceMock.Object);
    }

    [Fact]
    public async Task Execute_WithSuccessfulChecks_RecordsMetricsAndNotifies()
    {
        // Arrange
        var results = new List<MonitoringResult>
        {
            new MonitoringResult 
            { 
                CheckName = "Test1", 
                Status = MonitoringStatus.Healthy,
                Message = "OK",
                DurationMs = 100
            },
            new MonitoringResult 
            { 
                CheckName = "Test2", 
                Status = MonitoringStatus.Unhealthy,
                Message = "Failed",
                DurationMs = 200
            }
        };

        _monitoringServiceMock
            .Setup(x => x.PerformChecksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        // Act
        await _job.Execute(_contextMock.Object);

        // Assert
        _metricsServiceMock.Verify(x => x.RecordJobStart(), Times.Once);
        _metricsServiceMock.Verify(x => x.RecordJobSuccess(), Times.Once);
        _metricsServiceMock.Verify(x => x.RecordCheckResult("Test1", MonitoringStatus.Healthy, 100), Times.Once);
        _metricsServiceMock.Verify(x => x.RecordCheckResult("Test2", MonitoringStatus.Unhealthy, 200), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyAsync(It.IsAny<MonitoringEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Execute_WithMonitoringServiceException_RecordsFailureAndNotifies()
    {
        // Arrange
        var exception = new Exception("Test exception");
        _monitoringServiceMock
            .Setup(x => x.PerformChecksAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _job.Execute(_contextMock.Object));

        _metricsServiceMock.Verify(x => x.RecordJobStart(), Times.Once);
        _metricsServiceMock.Verify(x => x.RecordJobFailure(), Times.Once);
        _metricsServiceMock.Verify(x => x.RecordJobSuccess(), Times.Never);
        _notificationServiceMock.Verify(x => x.NotifyAsync(
            It.Is<MonitoringEvent>(e => e.Status == MonitoringStatus.Error && e.CheckName == "MonitoringJob"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WithCancellation_RecordsCancellationAndNotifies()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _contextMock.Setup(x => x.CancellationToken).Returns(cts.Token);

        _monitoringServiceMock
            .Setup(x => x.PerformChecksAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _job.Execute(_contextMock.Object));

        _metricsServiceMock.Verify(x => x.RecordJobStart(), Times.Once);
        _metricsServiceMock.Verify(x => x.RecordJobCancellation(), Times.Once);
        _metricsServiceMock.Verify(x => x.RecordJobSuccess(), Times.Never);
        _notificationServiceMock.Verify(x => x.NotifyAsync(
            It.Is<MonitoringEvent>(e => e.Status == MonitoringStatus.Error && e.Message == "Job was cancelled"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WithNotificationException_ContinuesProcessing()
    {
        // Arrange
        var results = new List<MonitoringResult>
        {
            new MonitoringResult 
            { 
                CheckName = "Test1", 
                Status = MonitoringStatus.Healthy,
                Message = "OK",
                DurationMs = 100
            }
        };

        _monitoringServiceMock
            .Setup(x => x.PerformChecksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        _notificationServiceMock
            .Setup(x => x.NotifyAsync(It.IsAny<MonitoringEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Notification failed"));

        // Act
        await _job.Execute(_contextMock.Object);

        // Assert
        _metricsServiceMock.Verify(x => x.RecordJobStart(), Times.Once);
        _metricsServiceMock.Verify(x => x.RecordJobSuccess(), Times.Once);
        _metricsServiceMock.Verify(x => x.RecordCheckResult("Test1", MonitoringStatus.Healthy, 100), Times.Once);
    }

    [Fact]
    public async Task Execute_WithEmptyResults_CompletesSuccessfully()
    {
        // Arrange
        _monitoringServiceMock
            .Setup(x => x.PerformChecksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonitoringResult>());

        // Act
        await _job.Execute(_contextMock.Object);

        // Assert
        _metricsServiceMock.Verify(x => x.RecordJobStart(), Times.Once);
        _metricsServiceMock.Verify(x => x.RecordJobSuccess(), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyAsync(It.IsAny<MonitoringEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MonitoringJob(null!, _monitoringServiceMock.Object, _notificationServiceMock.Object, _metricsServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullMonitoringService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MonitoringJob(_loggerMock.Object, null!, _notificationServiceMock.Object, _metricsServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MonitoringJob(_loggerMock.Object, _monitoringServiceMock.Object, null!, _metricsServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullMetricsService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MonitoringJob(_loggerMock.Object, _monitoringServiceMock.Object, _notificationServiceMock.Object, null!));
    }

    [Fact]
    public async Task Execute_CreatesUniqueJobIds()
    {
        // Arrange
        var capturedEvents = new List<MonitoringEvent>();
        var results = new List<MonitoringResult>
        {
            new MonitoringResult { CheckName = "Test", Status = MonitoringStatus.Healthy, DurationMs = 100 }
        };

        _monitoringServiceMock
            .Setup(x => x.PerformChecksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        _notificationServiceMock
            .Setup(x => x.NotifyAsync(It.IsAny<MonitoringEvent>(), It.IsAny<CancellationToken>()))
            .Callback<MonitoringEvent, CancellationToken>((evt, ct) => capturedEvents.Add(evt));

        // Act
        await _job.Execute(_contextMock.Object);
        await _job.Execute(_contextMock.Object);

        // Assert
        capturedEvents.Should().HaveCount(2);
        capturedEvents[0].JobId.Should().NotBe(capturedEvents[1].JobId);
        capturedEvents.Should().AllSatisfy(e => e.JobId.Should().NotBeNullOrEmpty());
    }
}
