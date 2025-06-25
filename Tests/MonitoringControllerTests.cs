using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MonitoringWorker.Controllers;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace MonitoringWorker.Tests;

public class MonitoringControllerTests
{
    private readonly Mock<IMonitoringService> _mockMonitoringService;
    private readonly Mock<IMetricsService> _mockMetricsService;
    private readonly Mock<IEventNotificationService> _mockEventNotificationService;
    private readonly Mock<ILogger<MonitoringController>> _mockLogger;
    private readonly MonitoringController _controller;

    public MonitoringControllerTests()
    {
        _mockMonitoringService = new Mock<IMonitoringService>();
        _mockMetricsService = new Mock<IMetricsService>();
        _mockEventNotificationService = new Mock<IEventNotificationService>();
        _mockLogger = new Mock<ILogger<MonitoringController>>();

        _controller = new MonitoringController(
            _mockMonitoringService.Object,
            _mockMetricsService.Object,
            _mockEventNotificationService.Object,
            _mockLogger.Object);

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task TriggerManualCheck_ReturnsOkWithResults()
    {
        // Arrange
        var expectedResults = new List<MonitoringResult>
        {
            new()
            {
                CheckName = "Test Endpoint",
                Status = MonitoringStatus.Healthy,
                Message = "OK",
                Timestamp = DateTime.UtcNow,
                DurationMs = 100
            }
        };

        _mockMonitoringService
            .Setup(x => x.CheckAllEndpointsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _controller.TriggerManualCheck();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResults = Assert.IsType<List<MonitoringResult>>(okResult.Value);
        Assert.Single(actualResults);
        Assert.Equal("Test Endpoint", actualResults[0].CheckName);

        _mockEventNotificationService.Verify(
            x => x.NotifyAsync(It.IsAny<MonitoringEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TriggerEndpointCheck_WithValidEndpoint_ReturnsOkWithResult()
    {
        // Arrange
        var endpointName = "TestEndpoint";
        var expectedResult = new MonitoringResult
        {
            CheckName = endpointName,
            Status = MonitoringStatus.Healthy,
            Message = "OK",
            Timestamp = DateTime.UtcNow,
            DurationMs = 150
        };

        _mockMonitoringService
            .Setup(x => x.CheckEndpointByNameAsync(endpointName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.TriggerEndpointCheck(endpointName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResult = Assert.IsType<MonitoringResult>(okResult.Value);
        Assert.Equal(endpointName, actualResult.CheckName);

        _mockEventNotificationService.Verify(
            x => x.NotifyAsync(It.IsAny<MonitoringEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TriggerEndpointCheck_WithInvalidEndpoint_ReturnsNotFound()
    {
        // Arrange
        var endpointName = "NonExistentEndpoint";

        _mockMonitoringService
            .Setup(x => x.CheckEndpointByNameAsync(endpointName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MonitoringResult?)null);

        // Act
        var result = await _controller.TriggerEndpointCheck(endpointName);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = notFoundResult.Value;
        Assert.NotNull(response);

        _mockEventNotificationService.Verify(
            x => x.NotifyAsync(It.IsAny<MonitoringEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void GetMetrics_ReturnsOkWithMetrics()
    {
        // Arrange
        var expectedMetrics = new { TotalChecks = 10, HealthyChecks = 8 };
        _mockMetricsService
            .Setup(x => x.GetCurrentMetrics())
            .Returns(expectedMetrics);

        // Act
        var result = _controller.GetMetrics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedMetrics, okResult.Value);
    }

    [Fact]
    public void GetMonitoringStatus_ReturnsOkWithStatus()
    {
        // Arrange
        var mockMetrics = new Dictionary<string, long>
        {
            { "check.healthy", 5 },
            { "check.unhealthy", 2 },
            { "check.error", 1 }
        };

        _mockMetricsService
            .Setup(x => x.GetCurrentMetrics())
            .Returns(mockMetrics);

        // Act
        var result = _controller.GetMonitoringStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetEndpoints_ReturnsOkWithEndpoints()
    {
        // Arrange
        var expectedEndpoints = new List<object>
        {
            new { Name = "Test1", Url = "https://test1.com" },
            new { Name = "Test2", Url = "https://test2.com" }
        };

        _mockMonitoringService
            .Setup(x => x.GetConfiguredEndpoints())
            .Returns(expectedEndpoints);

        // Act
        var result = _controller.GetEndpoints();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualEndpoints = Assert.IsType<List<object>>(okResult.Value);
        Assert.Equal(2, actualEndpoints.Count);
    }

    [Fact]
    public void GetMonitoringHistory_WithValidHours_ReturnsOkWithHistory()
    {
        // Arrange
        var hours = 24;
        var expectedHistory = new { Hours = hours, Data = "test" };

        _mockMetricsService
            .Setup(x => x.GetMonitoringHistory(TimeSpan.FromHours(hours)))
            .Returns(expectedHistory);

        // Act
        var result = _controller.GetMonitoringHistory(hours);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedHistory, okResult.Value);
    }

    [Fact]
    public void GetMonitoringHistory_WithInvalidHours_ReturnsBadRequest()
    {
        // Arrange
        var invalidHours = 200; // > 168

        // Act
        var result = _controller.GetMonitoringHistory(invalidHours);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task TriggerManualCheck_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockMonitoringService
            .Setup(x => x.CheckAllEndpointsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.TriggerManualCheck();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
}
