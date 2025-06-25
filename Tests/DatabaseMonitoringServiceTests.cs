using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using Moq;
using Xunit;

namespace MonitoringWorker.Tests;

public class DatabaseMonitoringServiceTests
{
    private readonly Mock<ILogger<DatabaseMonitoringService>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly DatabaseMonitoringOptions _dbOptions;
    private readonly DatabaseMonitoringService _dbService;

    public DatabaseMonitoringServiceTests()
    {
        _mockLogger = new Mock<ILogger<DatabaseMonitoringService>>();
        _mockCache = new Mock<IMemoryCache>();
        
        _dbOptions = new DatabaseMonitoringOptions
        {
            Enabled = true,
            DefaultConnectionTimeoutSeconds = 30,
            DefaultCommandTimeoutSeconds = 30,
            MaxConcurrentConnections = 10,
            Connections = new List<DatabaseConnectionConfig>
            {
                new DatabaseConnectionConfig
                {
                    Name = "TestConnection",
                    ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=TestDB;Trusted_Connection=true;",
                    Provider = "SqlServer",
                    Environment = "Test",
                    Enabled = true,
                    Tags = new List<string> { "test", "local" },
                    QueryNames = new List<string> { "TestQuery" }
                },
                new DatabaseConnectionConfig
                {
                    Name = "DisabledConnection",
                    ConnectionString = "Server=disabled;Database=TestDB;",
                    Provider = "SqlServer",
                    Environment = "Test",
                    Enabled = false
                }
            },
            Queries = new List<DatabaseQueryConfig>
            {
                new DatabaseQueryConfig
                {
                    Name = "TestQuery",
                    Sql = "SELECT 1 as TestResult",
                    Description = "Test query",
                    Type = "HealthCheck",
                    ResultType = "Scalar",
                    ExpectedValue = 1,
                    ComparisonOperator = "Equals",
                    Enabled = true
                },
                new DatabaseQueryConfig
                {
                    Name = "DisabledQuery",
                    Sql = "SELECT 2 as TestResult",
                    Description = "Disabled test query",
                    Type = "HealthCheck",
                    ResultType = "Scalar",
                    Enabled = false
                }
            }
        };

        var mockOptions = new Mock<IOptions<DatabaseMonitoringOptions>>();
        mockOptions.Setup(x => x.Value).Returns(_dbOptions);

        _dbService = new DatabaseMonitoringService(_mockLogger.Object, mockOptions.Object, _mockCache.Object);
    }

    [Fact]
    public async Task GetConnectionNamesAsync_ReturnsEnabledConnections()
    {
        // Act
        var result = await _dbService.GetConnectionNamesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("TestConnection");
        result.Should().NotContain("DisabledConnection");
    }

    [Fact]
    public async Task GetQueryNamesAsync_ReturnsEnabledQueries()
    {
        // Act
        var result = await _dbService.GetQueryNamesAsync();

        // Assert
        result.Should().NotBeNull();
        var queries = result.ToList();
        queries.Should().HaveCount(1);
        queries[0].Name.Should().Be("TestQuery");
        queries[0].Description.Should().Be("Test query");
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsBasicStatistics()
    {
        // Act
        var result = await _dbService.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalConnections.Should().Be(1); // Only enabled connections
        result.TotalQueries.Should().Be(1); // Only enabled queries
        result.ByEnvironment.Should().ContainKey("Test");
        result.ByEnvironment["Test"].ConnectionCount.Should().Be(1);
    }

    [Fact]
    public async Task TestConnectionAsync_NonExistentConnection_ReturnsNull()
    {
        // Act
        var result = await _dbService.TestConnectionAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TestConnectionAsync_DisabledConnection_ReturnsNull()
    {
        // Act
        var result = await _dbService.TestConnectionAsync("DisabledConnection");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_NonExistentConnection_ReturnsNull()
    {
        // Act
        var result = await _dbService.ExecuteQueryAsync("NonExistent", "TestQuery");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_NonExistentQuery_ReturnsNull()
    {
        // Act
        var result = await _dbService.ExecuteQueryAsync("TestConnection", "NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_DisabledQuery_ReturnsNull()
    {
        // Act
        var result = await _dbService.ExecuteQueryAsync("TestConnection", "DisabledQuery");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteCustomQueryAsync_NonExistentConnection_ReturnsErrorResult()
    {
        // Act
        var result = await _dbService.ExecuteCustomQueryAsync("NonExistent", "SELECT 1");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(MonitoringStatus.Error);
        result.Message.Should().Contain("Connection not found or disabled");
        result.ConnectionName.Should().Be("NonExistent");
        result.QueryName.Should().Be("CustomQuery");
    }

    [Fact]
    public async Task ExecuteCustomQueryAsync_ValidParameters_ReturnsResult()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "param1", "value1" },
            { "param2", 123 }
        };

        // Act
        var result = await _dbService.ExecuteCustomQueryAsync(
            "TestConnection", 
            "SELECT @param1 as StringValue, @param2 as IntValue", 
            parameters,
            60);

        // Assert
        result.Should().NotBeNull();
        result.ConnectionName.Should().Be("TestConnection");
        result.QueryName.Should().Be("CustomQuery");
        // Note: Actual execution will fail due to test environment, but we can verify the structure
    }

    [Fact]
    public async Task ExecuteConnectionQueriesAsync_NonExistentConnection_ReturnsEmpty()
    {
        // Act
        var result = await _dbService.ExecuteConnectionQueriesAsync("NonExistent");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteConnectionQueriesAsync_DisabledConnection_ReturnsEmpty()
    {
        // Act
        var result = await _dbService.ExecuteConnectionQueriesAsync("DisabledConnection");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void DatabaseMonitoringOptions_Validation_RequiredFields()
    {
        // Arrange
        var options = new DatabaseMonitoringOptions();

        // Act & Assert
        options.Connections.Should().NotBeNull();
        options.Queries.Should().NotBeNull();
        options.DefaultConnectionTimeoutSeconds.Should().BeGreaterThan(0);
        options.DefaultCommandTimeoutSeconds.Should().BeGreaterThan(0);
        options.MaxConcurrentConnections.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DatabaseConnectionConfig_Validation_RequiredFields()
    {
        // Arrange
        var config = new DatabaseConnectionConfig
        {
            Name = "Test",
            ConnectionString = "Server=test;",
            Provider = "SqlServer"
        };

        // Act & Assert
        config.Name.Should().NotBeNullOrEmpty();
        config.ConnectionString.Should().NotBeNullOrEmpty();
        config.Provider.Should().NotBeNullOrEmpty();
        config.Enabled.Should().BeTrue(); // Default value
        config.Environment.Should().Be("Development"); // Default value
        config.Tags.Should().NotBeNull();
        config.QueryNames.Should().NotBeNull();
    }

    [Fact]
    public void DatabaseQueryConfig_Validation_RequiredFields()
    {
        // Arrange
        var config = new DatabaseQueryConfig
        {
            Name = "Test",
            Sql = "SELECT 1",
            Description = "Test query"
        };

        // Act & Assert
        config.Name.Should().NotBeNullOrEmpty();
        config.Sql.Should().NotBeNullOrEmpty();
        config.Type.Should().Be("HealthCheck"); // Default value
        config.ResultType.Should().Be("Scalar"); // Default value
        config.ComparisonOperator.Should().Be("Equals"); // Default value
        config.Enabled.Should().BeTrue(); // Default value
    }

    [Theory]
    [InlineData("equals", 1, 1, true)]
    [InlineData("eq", 1, 1, true)]
    [InlineData("equals", 1, 2, false)]
    [InlineData("notequals", 1, 2, true)]
    [InlineData("ne", 1, 2, true)]
    [InlineData("notequals", 1, 1, false)]
    [InlineData("greaterthan", 2, 1, true)]
    [InlineData("gt", 2, 1, true)]
    [InlineData("greaterthan", 1, 2, false)]
    [InlineData("lessthan", 1, 2, true)]
    [InlineData("lt", 1, 2, true)]
    [InlineData("lessthan", 2, 1, false)]
    public void CompareValues_VariousOperators_ReturnsExpectedResult(
        string comparisonOperator, object actual, object expected, bool expectedResult)
    {
        // Act
        var result = CompareValuesViaReflection(actual, expected, comparisonOperator);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(null, null, MonitoringStatus.Healthy)]
    [InlineData(1, 1, MonitoringStatus.Healthy)]
    [InlineData(1, 2, MonitoringStatus.Unhealthy)]
    [InlineData(100, null, MonitoringStatus.Error)] // Above critical threshold
    [InlineData(50, null, MonitoringStatus.Unhealthy)] // Above warning threshold
    [InlineData(10, null, MonitoringStatus.Healthy)] // Below thresholds
    public void EvaluateQueryResult_VariousScenarios_ReturnsExpectedStatus(
        object? result, object? expectedValue, MonitoringStatus expectedStatus)
    {
        // Arrange
        var queryConfig = new DatabaseQueryConfig
        {
            Name = "Test",
            Sql = "SELECT 1",
            ExpectedValue = expectedValue,
            WarningThreshold = 25,
            CriticalThreshold = 75,
            ComparisonOperator = "Equals"
        };

        // Act
        var status = EvaluateQueryResultViaReflection(result, queryConfig);

        // Assert
        status.Should().Be(expectedStatus);
    }

    private static bool CompareValuesViaReflection(object actual, object expected, string comparisonOperator)
    {
        var method = typeof(DatabaseMonitoringService).GetMethod("CompareValues", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        return (bool)method!.Invoke(null, new object[] { actual, expected, comparisonOperator })!;
    }

    private static MonitoringStatus EvaluateQueryResultViaReflection(object? result, DatabaseQueryConfig queryConfig)
    {
        var method = typeof(DatabaseMonitoringService).GetMethod("EvaluateQueryResult", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        return (MonitoringStatus)method!.Invoke(null, new object?[] { result, queryConfig })!;
    }
}
