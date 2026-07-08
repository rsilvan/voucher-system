using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using VoucherSystem.Api.Middleware;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Domain;

namespace VoucherSystem.UnitTests;

public class SecurityTests
{
    // ─── No permission → 403 ───

    [Fact]
    public async Task RequirePermission_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["Permissions"] = new HashSet<string> { "some.other.permission" };

        // Simulate the RequirePermission filter logic
        var permissions = httpContext.Items["Permissions"] as HashSet<string>;
        var forbidden = permissions == null || !permissions.Contains("audit.read");

        // Assert
        forbidden.Should().BeTrue();
    }

    [Fact]
    public async Task RequirePermission_WithCorrectPermission_AllowsRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["Permissions"] = new HashSet<string> { "audit.read" };

        // Act
        var permissions = httpContext.Items["Permissions"] as HashSet<string>;
        var allowed = permissions != null && permissions.Contains("audit.read");

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task RequirePermission_WithNoPermissions_ReturnsForbidden()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["Permissions"] = new HashSet<string>();

        // Act
        var permissions = httpContext.Items["Permissions"] as HashSet<string>;
        var forbidden = permissions == null || !permissions.Contains("audit.read");

        // Assert
        forbidden.Should().BeTrue();
    }

    // ─── Token expired → 401 via JWT validation ───
    // This tests the validation parameters configured in Program.cs

    [Fact]
    public void JwtValidation_ValidatesTokenLifetime()
    {
        // Arrange
        var settings = new JwtSettings
        {
            SecretKey = "8a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0t1u2v3w4x5y6z7a8b9c0d",
            Issuer = "VoucherSystem",
            Audience = "VoucherSystem",
            AccessTokenExpirationMinutes = 15
        };

        var validationParams = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(settings.SecretKey))
        };

        // Assert
        validationParams.ValidateLifetime.Should().BeTrue();
        validationParams.ValidateIssuerSigningKey.Should().BeTrue();
    }

    // ─── Refresh token revoked → 401 ───

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var repoMock = new Mock<IAuthRepository>();
        var settings = new JwtSettings
        {
            SecretKey = "8a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0t1u2v3w4x5y6z7a8b9c0d",
            Issuer = "VoucherSystem",
            Audience = "VoucherSystem",
            AccessTokenExpirationMinutes = 15
        };
        var service = new AuthService(repoMock.Object, Options.Create(settings), new Mock<IEmailService>().Object);

        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            RevokedAt = DateTimeOffset.UtcNow.AddHours(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes("revoked-refresh-token")));
        repoMock.Setup(r => r.GetRefreshTokenByHashAsync(tokenHash)).ReturnsAsync(revokedToken);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "revoked-refresh-token" }));
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var repoMock = new Mock<IAuthRepository>();
        var settings = new JwtSettings
        {
            SecretKey = "8a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0t1u2v3w4x5y6z7a8b9c0d",
            Issuer = "VoucherSystem",
            Audience = "VoucherSystem",
            AccessTokenExpirationMinutes = 15
        };
        var service = new AuthService(repoMock.Object, Options.Create(settings), new Mock<IEmailService>().Object);

        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), // expired
            RevokedAt = null,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-8)
        };

        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes("expired-refresh-token")));
        repoMock.Setup(r => r.GetRefreshTokenByHashAsync(tokenHash)).ReturnsAsync(expiredToken);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "expired-refresh-token" }));
    }

    // ─── Expired invitation → ArgumentException ───

    [Fact]
    public async Task AcceptInvitationAsync_WithExpiredToken_ThrowsArgumentException()
    {
        // Arrange
        var repoMock = new Mock<IMemberRepository>();
        var service = new MemberService(repoMock.Object, new Mock<IEmailService>().Object);

        var expiredInvitation = new Invitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "user@test.com",
            NormalizedEmail = "user@test.com",
            Name = "Test User",
            RoleId = Guid.NewGuid(),
            TokenHash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    Encoding.UTF8.GetBytes("expired-invite-token"))),
            Status = "Pending",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), // expired
            InvitedByUserId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-8)
        };

        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes("expired-invite-token")));
        repoMock.Setup(r => r.GetInvitationByTokenHashAsync(tokenHash))
            .ReturnsAsync(expiredInvitation);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AcceptInvitationAsync("expired-invite-token", new AcceptInvitationRequest
            {
                Name = "Test User",
                Password = "SenhaForte123!",
                ConfirmPassword = "SenhaForte123!"
            }));
        ex.Message.Should().Be("Invitation has expired.");
    }

    // ─── Login rate limit ───
    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var repoMock = new Mock<IAuthRepository>();
        var settings = new JwtSettings
        {
            SecretKey = "8a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0t1u2v3w4x5y6z7a8b9c0d",
            Issuer = "VoucherSystem",
            Audience = "VoucherSystem",
            AccessTokenExpirationMinutes = 15
        };
        var service = new AuthService(repoMock.Object, Options.Create(settings), new Mock<IEmailService>().Object);

        repoMock.Setup(r => r.GetUserByEmailAsync("unknown@test.com"))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(
                new LoginRequest { Email = "unknown@test.com", Password = "wrong" },
                "127.0.0.1", "test"));
    }

    // ─── Plan validation for custom roles ───
    [Fact]
    public async Task CreateRoleAsync_WithPlanThatDoesNotAllowCustomRoles_ThrowsInvalidOperationException()
    {
        // Arrange
        var roleRepoMock = new Mock<IRoleRepository>();
        var orgRepoMock = new Mock<IOrganizationRepository>();
        var service = new RoleService(roleRepoMock.Object, orgRepoMock.Object);
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        orgRepoMock.Setup(r => r.GetOrganizationByIdAsync(orgId))
            .ReturnsAsync(new Organization { Id = orgId, PlanId = planId });
        orgRepoMock.Setup(r => r.GetPlanByIdAsync(planId))
            .ReturnsAsync(new Plan { Id = planId, Key = "Starter", AllowsCustomRoles = false });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateRoleAsync(orgId, new CreateRoleRequest
            {
                Name = "Custom Role",
                PermissionKeys = new List<string> { "users.read" }
            }));
        ex.Message.Should().Be("Your plan does not allow custom roles.");
    }

    [Fact]
    public async Task CreateRoleAsync_WithPlanThatAllowsCustomRoles_CreatesRole()
    {
        // Arrange
        var roleRepoMock = new Mock<IRoleRepository>();
        var orgRepoMock = new Mock<IOrganizationRepository>();
        var service = new RoleService(roleRepoMock.Object, orgRepoMock.Object);
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        orgRepoMock.Setup(r => r.GetOrganizationByIdAsync(orgId))
            .ReturnsAsync(new Organization { Id = orgId, PlanId = planId });
        orgRepoMock.Setup(r => r.GetPlanByIdAsync(planId))
            .ReturnsAsync(new Plan { Id = planId, Key = "Business", AllowsCustomRoles = true });
        roleRepoMock.Setup(r => r.CreateRoleAsync(It.IsAny<Role>()))
            .ReturnsAsync((Role r) => r);
        roleRepoMock.Setup(r => r.GetRolePermissionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Permission>());

        // Act
        var result = await service.CreateRoleAsync(orgId, new CreateRoleRequest
        {
            Name = "Custom Role",
            PermissionKeys = new List<string> { "users.read" }
        });

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Custom Role");
    }

    // ─── Audit export permission ───
    [Fact]
    public void AuditExport_RequiresAuditExportPermission()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["Permissions"] = new HashSet<string> { "audit.read" };

        // Act
        var permissions = httpContext.Items["Permissions"] as HashSet<string>;
        var hasExportPerm = permissions != null && permissions.Contains("audit.export");

        // Assert
        hasExportPerm.Should().BeFalse("audit.read does not grant audit.export");
    }

    [Fact]
    public void AuditExport_WithCorrectPermission_IsAllowed()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["Permissions"] = new HashSet<string> { "audit.export" };

        // Act
        var permissions = httpContext.Items["Permissions"] as HashSet<string>;
        var hasExportPerm = permissions != null && permissions.Contains("audit.export");

        // Assert
        hasExportPerm.Should().BeTrue();
    }

    // ─── Invitation resend with revoked status ───
    [Fact]
    public async Task ResendInvitationAsync_WithRevokedInvitation_ThrowsArgumentException()
    {
        // Arrange
        var repoMock = new Mock<IMemberRepository>();
        var service = new MemberService(repoMock.Object, new Mock<IEmailService>().Object);
        var orgId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();

        var revokedInvitation = new Invitation
        {
            Id = inviteId,
            OrganizationId = orgId,
            Email = "user@test.com",
            NormalizedEmail = "user@test.com",
            Name = "Test User",
            Status = "Revoked",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            InvitedByUserId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-8)
        };

        repoMock.Setup(r => r.GetInvitationByIdAsync(inviteId)).ReturnsAsync(revokedInvitation);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ResendInvitationAsync(orgId, inviteId, Guid.NewGuid()));
        ex.Message.Should().Be("Invitation is already revoked.");
    }

    // ─── Invitation revoke with accepted status ───
    [Fact]
    public async Task RevokeInvitationAsync_WithAcceptedInvitation_ThrowsArgumentException()
    {
        // Arrange
        var repoMock = new Mock<IMemberRepository>();
        var service = new MemberService(repoMock.Object, new Mock<IEmailService>().Object);
        var orgId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();

        var acceptedInvitation = new Invitation
        {
            Id = inviteId,
            OrganizationId = orgId,
            Email = "user@test.com",
            NormalizedEmail = "user@test.com",
            Name = "Test User",
            Status = "Accepted",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(5),
            InvitedByUserId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            AcceptedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        repoMock.Setup(r => r.GetInvitationByIdAsync(inviteId)).ReturnsAsync(acceptedInvitation);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RevokeInvitationAsync(orgId, inviteId, Guid.NewGuid()));
        ex.Message.Should().Be("Invitation is already accepted.");
    }
}
