using Moq;
using FluentAssertions;
using Microsoft.Extensions.Options;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Domain;

namespace VoucherSystem.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _repoMock;
    private readonly AuthService _service;
    private readonly JwtSettings _jwtSettings;

    public AuthServiceTests()
    {
        _repoMock = new Mock<IAuthRepository>();
        _jwtSettings = new JwtSettings
        {
            SecretKey = "8a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0t1u2v3w4x5y6z7a8b9c0d",
            Issuer = "VoucherSystem",
            Audience = "VoucherSystem",
            AccessTokenExpirationMinutes = 15
        };
        _service = new AuthService(_repoMock.Object, Options.Create(_jwtSettings), new Mock<IEmailService>().Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Name = "João",
            Email = "joao@teste.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Senha123!"),
            Status = "Active"
        };

        _repoMock.Setup(r => r.GetUserByEmailAsync("joao@teste.com")).ReturnsAsync(user);

        var member = new OrganizationMember
        {
            Id = memberId,
            OrganizationId = orgId,
            UserId = userId,
            RoleId = roleId,
            Status = "Active"
        };
        _repoMock.Setup(r => r.GetActiveMemberAsync(userId)).ReturnsAsync(member);

        var org = new Organization { Id = orgId, Name = "Empresa Teste", Status = "Active" };
        _repoMock.Setup(r => r.GetOrganizationAsync(orgId)).ReturnsAsync(org);

        var role = new Role { Id = roleId, Key = "OrganizationOwner", Name = "OrganizationOwner" };
        _repoMock.Setup(r => r.GetRoleAsync(roleId)).ReturnsAsync(role);

        var permissions = new List<Permission>
        {
            new() { Id = Guid.NewGuid(), Key = "organization.read", Resource = "organization", Action = "read" }
        };
        _repoMock.Setup(r => r.GetRolePermissionsAsync(roleId)).ReturnsAsync(permissions);
        _repoMock.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

        // Act
        var response = await _service.LoginAsync(
            new LoginRequest { Email = "joao@teste.com", Password = "Senha123!" },
            "127.0.0.1", "test-agent");

        // Assert
        response.Should().NotBeNull();
        response.AccessToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.ExpiresIn.Should().Be(900);
        response.User.Name.Should().Be("João");
        response.Organization.Name.Should().Be("Empresa Teste");
        response.Permissions.Should().Contain("organization.read");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "João",
            Email = "joao@teste.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SenhaCorreta!"),
            Status = "Active"
        };

        _repoMock.Setup(r => r.GetUserByEmailAsync("joao@teste.com")).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(
                new LoginRequest { Email = "joao@teste.com", Password = "SenhaErrada!" },
                null, null));
    }

    [Fact]
    public async Task LoginAsync_WithDisabledUser_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "João",
            Email = "joao@teste.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Senha123!"),
            Status = "Disabled"
        };

        _repoMock.Setup(r => r.GetUserByEmailAsync("joao@teste.com")).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(
                new LoginRequest { Email = "joao@teste.com", Password = "Senha123!" },
                null, null));
    }

    [Fact]
    public async Task LoginAsync_WithNonexistentUser_ThrowsUnauthorizedAccessException()
    {
        _repoMock.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(
                new LoginRequest { Email = "fake@email.com", Password = "Senha123!" },
                null, null));
    }

    [Fact]
    public async Task LoginAsync_WithSuspendedOrganization_ThrowsUnauthorizedAccessException()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Name = "João",
            Email = "joao@teste.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Senha123!"),
            Status = "Active"
        };

        _repoMock.Setup(r => r.GetUserByEmailAsync("joao@teste.com")).ReturnsAsync(user);

        var member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            UserId = userId,
            RoleId = roleId,
            Status = "Active"
        };
        _repoMock.Setup(r => r.GetActiveMemberAsync(userId)).ReturnsAsync(member);

        var org = new Organization { Id = orgId, Name = "Empresa", Status = "Suspended" };
        _repoMock.Setup(r => r.GetOrganizationAsync(orgId)).ReturnsAsync(org);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(
                new LoginRequest { Email = "joao@teste.com", Password = "Senha123!" },
                null, null));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithMismatchedPasswords_ThrowsArgumentException()
    {
        var request = new ResetPasswordRequest
        {
            Token = "valid-token",
            Password = "NovaSenha123!",
            ConfirmPassword = "Diferente456!"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ResetPasswordAsync(request));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithShortPassword_ThrowsArgumentException()
    {
        var request = new ResetPasswordRequest
        {
            Token = "valid-token",
            Password = "123",
            ConfirmPassword = "123"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ResetPasswordAsync(request));
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonexistentEmail_DoesNotThrow()
    {
        _repoMock.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var act = async () => await _service.ForgotPasswordAsync(
            new ForgotPasswordRequest { Email = "naoexiste@teste.com" });

        await act.Should().NotThrowAsync();
    }
}
