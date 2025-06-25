using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using MonitoringWorker.Services;
using Moq;
using Xunit;

namespace MonitoringWorker.Tests;

public class ConfigurationServiceTests
{
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IOptionsMonitor<MonitoringOptions>> _mockMonitoringOptions;
    private readonly Mock<IOptionsMonitor<AuthenticationOptions>> _mockAuthOptions;
    private readonly Mock<IOptionsMonitor<DatabaseMonitoringOptions>> _mockDbOptions;
    private readonly Mock<IOptionsMonitor<QuartzOptions>> _mockQuartzOptions;
    private readonly ConfigurationService _configService;

    public ConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockMonitoringOptions = new Mock<IOptionsMonitor<MonitoringOptions>>();
        _mockAuthOptions = new Mock<IOptionsMonitor<AuthenticationOptions>>();
        _mockDbOptions = new Mock<IOptionsMonitor<DatabaseMonitoringOptions>>();
        _mockQuartzOptions = new Mock<IOptionsMonitor<QuartzOptions>>();

        // Setup default return values
        _mockMonitoringOptions.Setup(x => x.CurrentValue).Returns(new MonitoringOptions());
        _mockAuthOptions.Setup(x => x.CurrentValue).Returns(new AuthenticationOptions());
        _mockDbOptions.Setup(x => x.CurrentValue).Returns(new DatabaseMonitoringOptions());
        _mockQuartzOptions.Setup(x => x.CurrentValue).Returns(new QuartzOptions());

        _configService = new ConfigurationService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockMonitoringOptions.Object,
            _mockAuthOptions.Object,
            _mockDbOptions.Object,
            _mockQuartzOptions.Object);
    }

    [Fact]
    public void GetMonitoringOptions_ReturnsCurrentValue()
    {
        // Arrange
        var expectedOptions = new MonitoringOptions
        {
            Endpoints = new List<EndpointConfig>
            {
                new EndpointConfig { Name = "Test", Url = "http://test.com" }
            }
        };
        _mockMonitoringOptions.Setup(x => x.CurrentValue).Returns(expectedOptions);

        // Act
        var result = _configService.GetMonitoringOptions();

        // Assert
        result.Should().BeSameAs(expectedOptions);
    }

    [Fact]
    public void GetAuthenticationOptions_ReturnsCurrentValue()
    {
        // Arrange
        var expectedOptions = new AuthenticationOptions
        {
            Enabled = true,
            Jwt = new JwtOptions { SecretKey = "test-key" }
        };
        _mockAuthOptions.Setup(x => x.CurrentValue).Returns(expectedOptions);

        // Act
        var result = _configService.GetAuthenticationOptions();

        // Assert
        result.Should().BeSameAs(expectedOptions);
    }

    [Fact]
    public void GetDatabaseMonitoringOptions_ReturnsCurrentValue()
    {
        // Arrange
        var expectedOptions = new DatabaseMonitoringOptions
        {
            Enabled = true,
            Connections = new List<DatabaseConnectionConfig>()
        };
        _mockDbOptions.Setup(x => x.CurrentValue).Returns(expectedOptions);

        // Act
        var result = _configService.GetDatabaseMonitoringOptions();

        // Assert
        result.Should().BeSameAs(expectedOptions);
    }

    [Fact]
    public void GetQuartzOptions_ReturnsCurrentValue()
    {
        // Arrange
        var expectedOptions = new QuartzOptions
        {
            CronSchedule = "0 0/5 * * * ?"
        };
        _mockQuartzOptions.Setup(x => x.CurrentValue).Returns(expectedOptions);

        // Act
        var result = _configService.GetQuartzOptions();

        // Assert
        result.Should().BeSameAs(expectedOptions);
    }

    [Fact]
    public async Task ValidateOptionsAsync_ValidOptions_ReturnsValidResult()
    {
        // Arrange
        var options = new MonitoringOptions
        {
            Endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    Name = "ValidEndpoint",
                    Url = "https://example.com",
                    TimeoutSeconds = 30
                }
            }
        };

        // Act
        var result = await _configService.ValidateOptionsAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateOptionsAsync_InvalidOptions_ReturnsInvalidResult()
    {
        // Arrange
        var options = new MonitoringOptions
        {
            Endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    Name = "", // Invalid: empty name
                    Url = "invalid-url", // Invalid: not a valid URL
                    TimeoutSeconds = -1 // Invalid: negative timeout
                }
            }
        };

        // Act
        var result = await _configService.ValidateOptionsAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateOptionsAsync_DuplicateEndpointNames_ReturnsInvalidResult()
    {
        // Arrange
        var options = new MonitoringOptions
        {
            Endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    Name = "Duplicate",
                    Url = "https://example1.com",
                    TimeoutSeconds = 30
                },
                new EndpointConfig
                {
                    Name = "Duplicate", // Duplicate name
                    Url = "https://example2.com",
                    TimeoutSeconds = 30
                }
            }
        };

        // Act
        var result = await _configService.ValidateOptionsAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Duplicate endpoint name"));
    }

    [Fact]
    public async Task ValidateOptionsAsync_AuthenticationOptions_ValidatesJwtKey()
    {
        // Arrange
        var options = new AuthenticationOptions
        {
            Enabled = true,
            Jwt = new JwtOptions
            {
                SecretKey = "short", // Too short
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            },
            Users = new UsersOptions
            {
                DefaultUsers = new List<UserConfig>
                {
                    new UserConfig
                    {
                        Id = "test",
                        Username = "test",
                        Password = "test",
                        Roles = new List<string> { "Admin" },
                        Enabled = true
                    }
                }
            }
        };

        // Act
        var result = await _configService.ValidateOptionsAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("JWT secret key must be at least 32 characters"));
    }

    [Fact]
    public async Task ValidateOptionsAsync_DatabaseOptions_ValidatesConnections()
    {
        // Arrange
        var options = new DatabaseMonitoringOptions
        {
            Enabled = true,
            Connections = new List<DatabaseConnectionConfig>
            {
                new DatabaseConnectionConfig
                {
                    Name = "Duplicate",
                    ConnectionString = "Server=test;",
                    Provider = "SqlServer"
                },
                new DatabaseConnectionConfig
                {
                    Name = "Duplicate", // Duplicate name
                    ConnectionString = "Server=test2;",
                    Provider = "SqlServer"
                }
            },
            Queries = new List<DatabaseQueryConfig>()
        };

        // Act
        var result = await _configService.ValidateOptionsAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Duplicate connection name"));
    }

    [Fact]
    public async Task ValidateOptionsAsync_QuartzOptions_ValidatesCronExpression()
    {
        // Arrange
        var options = new QuartzOptions
        {
            CronSchedule = "invalid-cron-expression"
        };

        // Act
        var result = await _configService.ValidateOptionsAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid cron expression"));
    }

    [Fact]
    public async Task GetConfigurationValueAsync_ExistingKey_ReturnsValue()
    {
        // Arrange
        const string key = "TestKey";
        const string expectedValue = "TestValue";
        _mockConfiguration.Setup(x => x[key]).Returns(expectedValue);

        // Act
        var result = await _configService.GetConfigurationValueAsync(key);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetConfigurationValueAsync_NonExistingKey_ReturnsNull()
    {
        // Arrange
        const string key = "NonExistingKey";
        _mockConfiguration.Setup(x => x[key]).Returns((string?)null);

        // Act
        var result = await _configService.GetConfigurationValueAsync(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetConfigurationValueAsync_ValidKey_ReturnsTrue()
    {
        // Arrange
        const string key = "TestKey";
        const string value = "TestValue";

        // Act
        var result = await _configService.SetConfigurationValueAsync(key, value);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetChangeHistoryAsync_ReturnsHistory()
    {
        // Arrange
        await _configService.SetConfigurationValueAsync("TestKey", "TestValue");

        // Act
        var result = await _configService.GetChangeHistoryAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.First().Key.Should().Be("TestKey");
        result.First().NewValue.Should().Be("TestValue");
    }

    [Fact]
    public async Task GetAllConfigurationAsync_ReturnsAllSections()
    {
        // Arrange
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x.Key).Returns("TestSection");
        mockSection.Setup(x => x.Value).Returns("TestValue");
        mockSection.Setup(x => x.GetChildren()).Returns(new List<IConfigurationSection>());

        _mockConfiguration.Setup(x => x.GetChildren()).Returns(new[] { mockSection.Object });

        // Act
        var result = await _configService.GetAllConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("Monitoring");
        result.Should().ContainKey("Authentication");
        result.Should().ContainKey("DatabaseMonitoring");
        result.Should().ContainKey("Quartz");
    }

    [Fact]
    public async Task UpdateMonitoringOptionsAsync_ValidOptions_ReturnsTrue()
    {
        // Arrange
        var options = new MonitoringOptions
        {
            Endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    Name = "ValidEndpoint",
                    Url = "https://example.com",
                    TimeoutSeconds = 30
                }
            }
        };

        // Act
        var result = await _configService.UpdateMonitoringOptionsAsync(options);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateMonitoringOptionsAsync_InvalidOptions_ReturnsFalse()
    {
        // Arrange
        var options = new MonitoringOptions
        {
            Endpoints = new List<EndpointConfig>
            {
                new EndpointConfig
                {
                    Name = "", // Invalid
                    Url = "invalid-url",
                    TimeoutSeconds = -1
                }
            }
        };

        // Act
        var result = await _configService.UpdateMonitoringOptionsAsync(options);

        // Assert
        result.Should().BeFalse();
    }
}
