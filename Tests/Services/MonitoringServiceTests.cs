using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace MonitoringWorker.Tests.Services;

public class MonitoringServiceTests
{
    private readonly Mock<ILogger<MonitoringService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly MonitoringOptions _options;
    private readonly MonitoringService _service;

    public MonitoringServiceTests()
    {
        _loggerMock = new Mock<ILogger<MonitoringService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        _options = new MonitoringOptions
        {
            DefaultTimeoutSeconds = 30,
            Endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    Name = "TestEndpoint1",
                    Url = "https://api.test.com/health",
                    TimeoutSeconds = 10
                },
                new EndpointConfig
                {
                    Name = "TestEndpoint2",
                    Url = "https://api.test.com/status",
                    TimeoutSeconds = 15,
                    ExpectedStatusCodes = new List<int> { 200, 202 }
                }
            }
        };

        var optionsMock = new Mock<IOptions<MonitoringOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        _service = new MonitoringService(_httpClient, _loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public async Task PerformChecksAsync_WithHealthyEndpoints_ReturnsHealthyResults()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                ReasonPhrase = "OK"
            });

        // Act
        var results = await _service.PerformChecksAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Status.Should().Be(MonitoringStatus.Healthy));
        results.Should().AllSatisfy(r => r.Message.Should().Contain("200 OK"));
    }

    [Fact]
    public async Task PerformChecksAsync_WithUnhealthyEndpoint_ReturnsUnhealthyResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                ReasonPhrase = "Internal Server Error"
            });

        // Act
        var results = await _service.PerformChecksAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Status.Should().Be(MonitoringStatus.Unhealthy));
        results.Should().AllSatisfy(r => r.Message.Should().Contain("500 Internal Server Error"));
    }

    [Fact]
    public async Task PerformChecksAsync_WithHttpException_ReturnsErrorResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act
        var results = await _service.PerformChecksAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Status.Should().Be(MonitoringStatus.Error));
        results.Should().AllSatisfy(r => r.Message.Should().Be("HTTP request failed"));
    }

    [Fact]
    public async Task PerformChecksAsync_WithTimeout_ReturnsErrorResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        // Act
        var results = await _service.PerformChecksAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Status.Should().Be(MonitoringStatus.Error));
        results.Should().AllSatisfy(r => r.Message.Should().Be("Request timed out"));
    }

    [Fact]
    public async Task PerformCheckAsync_WithValidEndpointName_ReturnsResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await _service.PerformCheckAsync("TestEndpoint1");

        // Assert
        result.Should().NotBeNull();
        result!.CheckName.Should().Be("TestEndpoint1");
        result.Status.Should().Be(MonitoringStatus.Healthy);
    }

    [Fact]
    public async Task PerformCheckAsync_WithInvalidEndpointName_ReturnsNull()
    {
        // Act
        var result = await _service.PerformCheckAsync("NonExistentEndpoint");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PerformChecksAsync_WithCancellation_ReturnsCancelledResults()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var results = await _service.PerformChecksAsync(cts.Token);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Status.Should().Be(MonitoringStatus.Error));
        results.Should().AllSatisfy(r => r.Message.Should().BeOneOf("Check was cancelled", "Unexpected error occurred"));
    }

    [Fact]
    public async Task PerformChecksAsync_WithCustomStatusCodes_ReturnsCorrectStatus()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("status")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted)); // 202

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("health")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)); // 200

        // Act
        var results = await _service.PerformChecksAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Status.Should().Be(MonitoringStatus.Healthy));
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var optionsMock = new Mock<IOptions<MonitoringOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        Assert.Throws<ArgumentNullException>(() => 
            new MonitoringService(null!, _loggerMock.Object, optionsMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var optionsMock = new Mock<IOptions<MonitoringOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        Assert.Throws<ArgumentNullException>(() => 
            new MonitoringService(_httpClient, null!, optionsMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MonitoringService(_httpClient, _loggerMock.Object, null!));
    }
}
