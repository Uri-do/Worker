using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using MonitoringWorker.Models;
using MonitoringWorker.Services;
using Moq;
using Xunit;

namespace MonitoringWorker.Tests;

public class AuthenticationServiceTests
{
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly AuthenticationOptions _authOptions;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _mockLogger = new Mock<ILogger<AuthenticationService>>();
        
        _authOptions = new AuthenticationOptions
        {
            Enabled = true,
            Jwt = new JwtOptions
            {
                SecretKey = "test-secret-key-that-is-at-least-32-characters-long",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 60,
                ClockSkewMinutes = 5
            },
            Users = new UsersOptions
            {
                DefaultUsers = new List<UserConfig>
                {
                    new UserConfig
                    {
                        Id = "test-user-1",
                        Username = "testuser",
                        Password = "testpass",
                        Roles = new List<string> { UserRoles.Admin },
                        Enabled = true
                    },
                    new UserConfig
                    {
                        Id = "test-user-2",
                        Username = "viewer",
                        Password = "viewerpass",
                        Roles = new List<string> { UserRoles.Viewer },
                        Enabled = true
                    },
                    new UserConfig
                    {
                        Id = "test-user-3",
                        Username = "disabled",
                        Password = "disabledpass",
                        Roles = new List<string> { UserRoles.Viewer },
                        Enabled = false
                    }
                }
            }
        };

        var mockOptions = new Mock<IOptions<AuthenticationOptions>>();
        mockOptions.Setup(x => x.Value).Returns(_authOptions);

        _authService = new AuthenticationService(_mockLogger.Object, mockOptions.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsLoginResponse()
    {
        // Act
        var result = await _authService.AuthenticateAsync("testuser", "testpass");

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().Be(3600); // 60 minutes * 60 seconds
        result.User.Username.Should().Be("testuser");
        result.User.Roles.Should().Contain(UserRoles.Admin);
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidUsername_ReturnsNull()
    {
        // Act
        var result = await _authService.AuthenticateAsync("nonexistent", "testpass");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidPassword_ReturnsNull()
    {
        // Act
        var result = await _authService.AuthenticateAsync("testuser", "wrongpass");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_DisabledUser_ReturnsNull()
    {
        // Act
        var result = await _authService.AuthenticateAsync("disabled", "disabledpass");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_EmptyCredentials_ReturnsNull()
    {
        // Act
        var result1 = await _authService.AuthenticateAsync("", "testpass");
        var result2 = await _authService.AuthenticateAsync("testuser", "");
        var result3 = await _authService.AuthenticateAsync("", "");

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }

    [Fact]
    public async Task GetUserAsync_ValidUsername_ReturnsUserInfo()
    {
        // Act
        var result = await _authService.GetUserAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Id.Should().Be("test-user-1");
        result.Roles.Should().Contain(UserRoles.Admin);
        result.Permissions.Should().Contain(Permissions.ViewMonitoring);
        result.Permissions.Should().Contain(Permissions.ManageMonitoring);
        result.Permissions.Should().Contain(Permissions.ManageUsers);
    }

    [Fact]
    public async Task GetUserAsync_InvalidUsername_ReturnsNull()
    {
        // Act
        var result = await _authService.GetUserAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserAsync_DisabledUser_ReturnsNull()
    {
        // Act
        var result = await _authService.GetUserAsync("disabled");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HasPermissionAsync_UserWithPermission_ReturnsTrue()
    {
        // Act
        var result = await _authService.HasPermissionAsync("testuser", Permissions.ViewMonitoring);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_UserWithoutPermission_ReturnsFalse()
    {
        // Act
        var result = await _authService.HasPermissionAsync("viewer", Permissions.ManageUsers);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasRoleAsync_UserWithRole_ReturnsTrue()
    {
        // Act
        var result = await _authService.HasRoleAsync("testuser", UserRoles.Admin);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasRoleAsync_UserWithoutRole_ReturnsFalse()
    {
        // Act
        var result = await _authService.HasRoleAsync("viewer", UserRoles.Admin);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsUserInfo()
    {
        // Arrange
        var loginResponse = await _authService.AuthenticateAsync("testuser", "testpass");
        loginResponse.Should().NotBeNull();

        // Act
        var result = await _authService.ValidateTokenAsync(loginResponse!.AccessToken);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsNull()
    {
        // Act
        var result = await _authService.ValidateTokenAsync("invalid-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_EmptyToken_ReturnsNull()
    {
        // Act
        var result = await _authService.ValidateTokenAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(UserRoles.Admin, Permissions.ViewMonitoring, true)]
    [InlineData(UserRoles.Admin, Permissions.ManageMonitoring, true)]
    [InlineData(UserRoles.Admin, Permissions.ManageUsers, true)]
    [InlineData(UserRoles.Operator, Permissions.ViewMonitoring, true)]
    [InlineData(UserRoles.Operator, Permissions.ManageMonitoring, true)]
    [InlineData(UserRoles.Operator, Permissions.ManageUsers, false)]
    [InlineData(UserRoles.Viewer, Permissions.ViewMonitoring, true)]
    [InlineData(UserRoles.Viewer, Permissions.ManageMonitoring, false)]
    [InlineData(UserRoles.Viewer, Permissions.ManageUsers, false)]
    public void GetUserPermissions_RoleBasedPermissions_ReturnsCorrectPermissions(
        string role, string permission, bool shouldHave)
    {
        // Arrange
        var userConfig = new UserConfig
        {
            Id = "test",
            Username = "test",
            Password = "test",
            Roles = new List<string> { role },
            Enabled = true
        };

        // Act
        var permissions = GetUserPermissionsViaReflection(userConfig);

        // Assert
        if (shouldHave)
        {
            permissions.Should().Contain(permission);
        }
        else
        {
            permissions.Should().NotContain(permission);
        }
    }

    private static IEnumerable<string> GetUserPermissionsViaReflection(UserConfig user)
    {
        // Use reflection to access the private GetUserPermissions method
        var method = typeof(AuthenticationService).GetMethod("GetUserPermissions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        return (IEnumerable<string>)method!.Invoke(null, new object[] { user })!;
    }
}
