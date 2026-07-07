using FluentAssertions;
using VoucherSystem.Api.Middleware;

namespace VoucherSystem.UnitTests;

public class UserContextTests
{
    [Fact]
    public void HasPermission_WhenPermissionExists_ReturnsTrue()
    {
        var ctx = new UserContext
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            Permissions = new HashSet<string> { "organization.read", "users.invite" }
        };

        ctx.HasPermission("organization.read").Should().BeTrue();
        ctx.HasPermission("users.invite").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_WhenPermissionMissing_ReturnsFalse()
    {
        var ctx = new UserContext
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            Permissions = new HashSet<string> { "organization.read" }
        };

        ctx.HasPermission("campaigns.create").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_WithEmptyPermissions_ReturnsFalse()
    {
        var ctx = new UserContext
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            Permissions = new HashSet<string>()
        };

        ctx.HasPermission("anything").Should().BeFalse();
    }

    [Fact]
    public void UserContext_WithNullMemberId_IsValid()
    {
        var ctx = new UserContext
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            MemberId = null,
            Permissions = new HashSet<string> { "organization.read" }
        };

        ctx.MemberId.Should().BeNull();
        ctx.HasPermission("organization.read").Should().BeTrue();
    }
}
