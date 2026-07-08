using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VoucherSystem.Contracts;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepo;
    private readonly JwtSettings _jwt;
    private readonly IEmailService _emailService;
    private static readonly Meter AuthMeter = new("VoucherSystem.Auth");
    private static readonly Counter<int> LoginAttempts = AuthMeter.CreateCounter<int>("login_attempts_total", description: "Total login attempts");
    private static readonly Counter<int> AuthErrors = AuthMeter.CreateCounter<int>("auth_errors_total", description: "Total authentication errors");

    public AuthService(IAuthRepository authRepo, IOptions<JwtSettings> jwtOptions, IEmailService emailService)
    {
        _authRepo = authRepo;
        _jwt = jwtOptions.Value;
        _emailService = emailService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            AuthErrors.Add(1, new KeyValuePair<string, object?>("error_type", "missing_email"));
            throw new ArgumentException("Email is required.");
        }
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            AuthErrors.Add(1, new KeyValuePair<string, object?>("error_type", "missing_password"));
            throw new ArgumentException("Password is required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _authRepo.GetUserByEmailAsync(normalizedEmail);

        if (user == null)
        {
            LoginAttempts.Add(1, new KeyValuePair<string, object?>("success", false));
            AuthErrors.Add(1, new KeyValuePair<string, object?>("error_type", "invalid_credentials"));
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (user.Status == "Disabled" || user.Status == "Locked")
        {
            LoginAttempts.Add(1, new KeyValuePair<string, object?>("success", false));
            AuthErrors.Add(1, new KeyValuePair<string, object?>("error_type", "account_disabled"));
            throw new UnauthorizedAccessException("Account is disabled or locked.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.AccessFailedCount++;
            LoginAttempts.Add(1, new KeyValuePair<string, object?>("success", false));
            AuthErrors.Add(1, new KeyValuePair<string, object?>("error_type", "wrong_password"));
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        user.AccessFailedCount = 0;
        user.LastLoginAt = DateTimeOffset.UtcNow;

        var member = await _authRepo.GetActiveMemberAsync(user.Id)
            ?? throw new UnauthorizedAccessException("No active organization membership.");

        var organization = await _authRepo.GetOrganizationAsync(member.OrganizationId)
            ?? throw new UnauthorizedAccessException("Organization not found.");

        if (organization.Status == "Suspended" || organization.Status == "Canceled")
        {
            LoginAttempts.Add(1, new KeyValuePair<string, object?>("success", false));
            AuthErrors.Add(1, new KeyValuePair<string, object?>("error_type", "org_suspended"));
            throw new UnauthorizedAccessException("Organization is suspended or canceled.");
        }

        var role = await _authRepo.GetRoleAsync(member.RoleId)
            ?? throw new UnauthorizedAccessException("Role not found.");

        var permissions = await _authRepo.GetRolePermissionsAsync(role.Id);

        var accessToken = GenerateAccessToken(user, organization, member, permissions);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress, userAgent);

        LoginAttempts.Add(1, new KeyValuePair<string, object?>("success", true));

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = (int)_jwt.AccessTokenExpiration.TotalSeconds,
            User = new UserInfo { Id = user.Id, Name = user.Name, Email = user.Email },
            Organization = new OrgInfo { Id = organization.Id, Name = organization.Name },
            Permissions = permissions.Select(p => p.Key).ToList()
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var stored = await _authRepo.GetRefreshTokenByHashAsync(hash);
        if (stored != null && stored.RevokedAt == null)
        {
            await _authRepo.RevokeRefreshTokenAsync(stored.Id);
        }
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var hash = HashToken(request.RefreshToken);
        var stored = await _authRepo.GetRefreshTokenByHashAsync(hash);
        if (stored == null || stored.RevokedAt != null || stored.ExpiresAt < DateTimeOffset.UtcNow)
        {
            AuthErrors.Add(1, new KeyValuePair<string, object?>("error_type", "invalid_refresh_token"));
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        await _authRepo.RevokeRefreshTokenAsync(stored.Id);

        var user = await _authRepo.GetUserByIdAsync(stored.UserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (user.Status == "Disabled" || user.Status == "Locked")
            throw new UnauthorizedAccessException("Account is disabled or locked.");

        var member = await _authRepo.GetActiveMemberAsync(user.Id)
            ?? throw new UnauthorizedAccessException("No active organization membership.");

        var organization = await _authRepo.GetOrganizationAsync(member.OrganizationId)
            ?? throw new UnauthorizedAccessException("Organization not found.");

        if (organization.Status == "Suspended" || organization.Status == "Canceled")
            throw new UnauthorizedAccessException("Organization is suspended or canceled.");

        var role = await _authRepo.GetRoleAsync(member.RoleId)
            ?? throw new UnauthorizedAccessException("Role not found.");

        var permissions = await _authRepo.GetRolePermissionsAsync(role.Id);

        var accessToken = GenerateAccessToken(user, organization, member, permissions);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id, stored.IpAddress, stored.UserAgent);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = (int)_jwt.AccessTokenExpiration.TotalSeconds,
            User = new UserInfo { Id = user.Id, Name = user.Name, Email = user.Email },
            Organization = new OrgInfo { Id = organization.Id, Name = organization.Name },
            Permissions = permissions.Select(p => p.Key).ToList()
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return; // Don't reveal if email exists
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _authRepo.GetUserByEmailAsync(normalizedEmail);
        if (user == null) return;

        var rawToken = Guid.NewGuid().ToString("N");
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _authRepo.SavePasswordResetTokenAsync(token);

        await _emailService.SendPasswordResetEmailAsync(user.Email, rawToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new ArgumentException("Token is required.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");
        if (request.Password != request.ConfirmPassword)
            throw new ArgumentException("Passwords do not match.");
        if (request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        var hash = HashToken(request.Token);
        var stored = await _authRepo.GetPasswordResetTokenByHashAsync(hash);
        if (stored == null || stored.Used || stored.ExpiresAt < DateTimeOffset.UtcNow)
            throw new ArgumentException("Invalid or expired reset token.");

        await _authRepo.MarkResetTokenUsedAsync(stored.Id);
        await _authRepo.UpdateUserPasswordAsync(stored.UserId, BCrypt.Net.BCrypt.HashPassword(request.Password));
    }

    private string GenerateAccessToken(User user, Organization organization, OrganizationMember member, List<Permission> permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new("organization_id", organization.Id.ToString()),
            // project_id is intentionally omitted here — resolved dynamically
            // by ProjectContextMiddleware via X-Project-Id header or primary project lookup
            new("member_id", member.Id.ToString()),
            new("role_id", member.RoleId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_jwt.AccessTokenExpiration),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateRefreshTokenAsync(Guid userId, string? ipAddress, string? userAgent)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var tokenString = Convert.ToBase64String(tokenBytes);
        var tokenHash = HashToken(tokenString);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _authRepo.SaveRefreshTokenAsync(refreshToken);
        return tokenString;
    }

    private static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public class JwtSettings
{
    public string SecretKey { get; set; } = default!;
    public string Issuer { get; set; } = "VoucherSystem";
    public string Audience { get; set; } = "VoucherSystem";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public TimeSpan AccessTokenExpiration => TimeSpan.FromMinutes(AccessTokenExpirationMinutes);
}
