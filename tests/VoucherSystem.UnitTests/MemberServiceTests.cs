using Moq;
using FluentAssertions;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Domain;

namespace VoucherSystem.UnitTests;

public class MemberServiceTests
{
    private readonly Mock<IMemberRepository> _repoMock;
    private readonly MemberService _service;

    public MemberServiceTests()
    {
        _repoMock = new Mock<IMemberRepository>();
        _service = new MemberService(_repoMock.Object);
    }

    [Fact]
    public async Task InviteMemberAsync_WithValidRequest_ReturnsInviteResponse()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _repoMock.Setup(r => r.IsEmailInvitedAsync(orgId, "maria@teste.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveInvitationAsync(It.IsAny<Invitation>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveInvitationProjectAccessAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>()))
            .Returns(Task.CompletedTask);

        var result = await _service.InviteMemberAsync(orgId, userId, new InviteMemberRequest
        {
            Name = "Maria",
            Email = "maria@teste.com",
            RoleId = roleId,
            ProjectIds = new List<Guid> { Guid.NewGuid() }
        });

        result.Should().NotBeNull();
        result.Status.Should().Be("Pending");
        result.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task InviteMemberAsync_WithDuplicatedEmail_ThrowsArgumentException()
    {
        var orgId = Guid.NewGuid();
        _repoMock.Setup(r => r.IsEmailInvitedAsync(orgId, "maria@teste.com")).ReturnsAsync(true);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.InviteMemberAsync(orgId, Guid.NewGuid(), new InviteMemberRequest
            {
                Name = "Maria",
                Email = "maria@teste.com",
                RoleId = Guid.NewGuid()
            }));
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithValidToken_CreatesUserAndMember()
    {
        var orgId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Email = "maria@teste.com",
            NormalizedEmail = "maria@teste.com",
            Name = "Maria",
            RoleId = roleId,
            TokenHash = "hash",
            Status = "Pending",
            ExpiresAt = now.AddDays(7),
            CreatedAt = now
        };

        _repoMock.Setup(r => r.GetInvitationByTokenHashAsync(It.IsAny<string>())).ReturnsAsync(invitation);
        _repoMock.Setup(r => r.SaveNewUserMemberAsync(It.IsAny<User>(), It.IsAny<OrganizationMember>()))
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateInvitationStatusAsync(It.IsAny<Guid>(), "Accepted", It.IsAny<DateTimeOffset>()))
            .Returns(Task.CompletedTask);

        var result = await _service.AcceptInvitationAsync("valid-token", new AcceptInvitationRequest
        {
            Name = "Maria",
            Password = "SenhaForte123!",
            ConfirmPassword = "SenhaForte123!"
        });

        result.Should().NotBeNull();
        result.Status.Should().Be("Accepted");
        result.OrganizationId.Should().Be(orgId);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithExpiredToken_ThrowsArgumentException()
    {
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "maria@teste.com",
            Status = "Pending",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) // expired
        };

        _repoMock.Setup(r => r.GetInvitationByTokenHashAsync(It.IsAny<string>())).ReturnsAsync(invitation);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AcceptInvitationAsync("expired-token", new AcceptInvitationRequest
            {
                Password = "SenhaForte123!",
                ConfirmPassword = "SenhaForte123!"
            }));
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithAlreadyAcceptedToken_ThrowsArgumentException()
    {
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Status = "Accepted",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        _repoMock.Setup(r => r.GetInvitationByTokenHashAsync(It.IsAny<string>())).ReturnsAsync(invitation);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AcceptInvitationAsync("used-token", new AcceptInvitationRequest
            {
                Password = "SenhaForte123!",
                ConfirmPassword = "SenhaForte123!"
            }));
    }

    [Fact]
    public async Task DisableMemberAsync_WithLastOwner_ThrowsInvalidOperationException()
    {
        var orgId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var member = new OrganizationMember
        {
            Id = memberId,
            OrganizationId = orgId,
            UserId = Guid.NewGuid(),
            RoleId = roleId,
            Status = "Active"
        };

        _repoMock.Setup(r => r.GetMemberByIdAsync(orgId, memberId)).ReturnsAsync(member);
        _repoMock.Setup(r => r.GetRoleAsync(roleId)).ReturnsAsync(new Role { Key = "OrganizationOwner" });
        _repoMock.Setup(r => r.GetOrganizationMembersAsync(orgId))
            .ReturnsAsync(new List<OrganizationMember> { member }); // only 1 owner

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DisableMemberAsync(orgId, memberId));
    }

    [Fact]
    public async Task DisableMemberAsync_WithNonOwner_DisablesSuccessfully()
    {
        var orgId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var member = new OrganizationMember
        {
            Id = memberId,
            OrganizationId = orgId,
            UserId = Guid.NewGuid(),
            RoleId = roleId,
            Status = "Active"
        };

        _repoMock.Setup(r => r.GetMemberByIdAsync(orgId, memberId)).ReturnsAsync(member);
        _repoMock.Setup(r => r.GetRoleAsync(roleId)).ReturnsAsync(new Role { Key = "MarketingManager" });
        _repoMock.Setup(r => r.UpdateMemberStatusAsync(memberId, "Disabled")).Returns(Task.CompletedTask);

        await _service.DisableMemberAsync(orgId, memberId);
        // Should not throw
    }
}
