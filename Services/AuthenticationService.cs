using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MonitoringWorker.Configuration;
using MonitoringWorker.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MonitoringWorker.Services;

/// <summary>
/// Implementation of authentication service
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly AuthenticationOptions _authOptions;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IOptions<AuthenticationOptions> authOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
        _tokenHandler = new JwtSecurityTokenHandler();
        
        var key = Encoding.UTF8.GetBytes(_authOptions.Jwt.SecretKey);
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _authOptions.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = _authOptions.Jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(_authOptions.Jwt.ClockSkewMinutes)
        };
    }

    public async Task<LoginResponse?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Authentication attempt with empty username or password");
            return null;
        }

        try
        {
            var user = _authOptions.Users.DefaultUsers
                .FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.Enabled);

            if (user == null)
            {
                _logger.LogWarning("Authentication failed: User {Username} not found or disabled", username);
                return null;
            }

            // In production, use proper password hashing (BCrypt, Argon2, etc.)
            if (!VerifyPassword(password, user.Password))
            {
                _logger.LogWarning("Authentication failed: Invalid password for user {Username}", username);
                return null;
            }

            var token = GenerateJwtToken(user);
            var userInfo = await GetUserAsync(user.Username);

            _logger.LogInformation("User {Username} authenticated successfully", username);

            return new LoginResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = _authOptions.Jwt.ExpirationMinutes * 60,
                User = userInfo!
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user {Username}", username);
            return null;
        }
    }

    public Task<UserInfo?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult<UserInfo?>(null);

        try
        {
            var principal = _tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return Task.FromResult<UserInfo?>(null);
            }

            var username = principal.FindFirst(TokenClaims.Username)?.Value;
            if (string.IsNullOrEmpty(username))
                return Task.FromResult<UserInfo?>(null);

            return GetUserAsync(username);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token validation failed");
            return Task.FromResult<UserInfo?>(null);
        }
    }

    public Task<UserInfo?> GetUserAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Task.FromResult<UserInfo?>(null);

        var user = _authOptions.Users.DefaultUsers
            .FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.Enabled);

        if (user == null)
            return Task.FromResult<UserInfo?>(null);

        return Task.FromResult<UserInfo?>(new UserInfo
        {
            Id = user.Id,
            Username = user.Username,
            Roles = user.Roles.ToList(),
            Permissions = GetUserPermissions(user).ToList()
        });
    }

    public async Task<bool> HasPermissionAsync(string username, string permission)
    {
        var user = await GetUserAsync(username);
        return user?.Permissions.Contains(permission) == true;
    }

    public async Task<bool> HasRoleAsync(string username, string role)
    {
        var user = await GetUserAsync(username);
        return user?.Roles.Contains(role) == true;
    }

    private string GenerateJwtToken(UserConfig user)
    {
        var key = Encoding.UTF8.GetBytes(_authOptions.Jwt.SecretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(TokenClaims.UserId, user.Id),
                new Claim(TokenClaims.Username, user.Username),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            }.Concat(user.Roles.Select(role => new Claim(TokenClaims.Role, role)))
             .Concat(GetUserPermissions(user).Select(permission => new Claim(TokenClaims.Permission, permission)))),
            
            Expires = DateTime.UtcNow.AddMinutes(_authOptions.Jwt.ExpirationMinutes),
            Issuer = _authOptions.Jwt.Issuer,
            Audience = _authOptions.Jwt.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        // Simple comparison for development - use proper password hashing in production
        // Example: BCrypt.Net.BCrypt.Verify(password, hash)
        return password == hash;
    }

    private static IEnumerable<string> GetUserPermissions(UserConfig user)
    {
        var permissions = new HashSet<string>(user.Permissions);

        // Add role-based permissions
        foreach (var role in user.Roles)
        {
            switch (role)
            {
                case UserRoles.Admin:
                    permissions.Add(Permissions.ViewMonitoring);
                    permissions.Add(Permissions.ManageMonitoring);
                    permissions.Add(Permissions.ViewMetrics);
                    permissions.Add(Permissions.ManageConfiguration);
                    permissions.Add(Permissions.ViewLogs);
                    permissions.Add(Permissions.ManageUsers);
                    break;
                
                case UserRoles.Operator:
                    permissions.Add(Permissions.ViewMonitoring);
                    permissions.Add(Permissions.ManageMonitoring);
                    permissions.Add(Permissions.ViewMetrics);
                    permissions.Add(Permissions.ViewLogs);
                    break;
                
                case UserRoles.Viewer:
                    permissions.Add(Permissions.ViewMonitoring);
                    permissions.Add(Permissions.ViewMetrics);
                    break;
            }
        }

        return permissions;
    }
}
