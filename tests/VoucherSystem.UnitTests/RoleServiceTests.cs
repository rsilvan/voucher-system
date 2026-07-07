using Moq;
using FluentAssertions;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Domain;

namespace VoucherSystem.UnitTests;

public class RoleServiceTests
{
    private readonly Mock<IRoleRepository> _repoMock;
    private readonly Mock<IOrganizationRepository> _orgRepoMock;
    private readonly RoleService _service;

    public RoleServiceTests()
    {
        _repoMock = new Mock<IRoleRepository>();
        _orgRepoMock = new Mock<IOrganizationRepository>();
        _service = new RoleService(_repoMock.Object, _orgRepoMock.Object);
    }

    [Fact]
    public async Task CreateRoleAsync_WithValidRequest_CreatesCustomRole()
    {
        var orgId = Guid.NewGuid();
        _repoMock.Setup(r => r.CreateRoleAsync(It.IsAny<Role>()))
            .ReturnsAsync((Role r) => r);
        _repoMock.Setup(r => r.GetRolePermissionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Permission>());
        _orgRepoMock.Setup(o => o.GetOrganizationByIdAsync(orgId))
            .ReturnsAsync(new Organization { Id = orgId, PlanId = Guid.NewGuid() });
        _orgRepoMock.Setup(o => o.GetPlanByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Plan { AllowsCustomRoles = true });

        var result = await _service.CreateRoleAsync(orgId, new CreateRoleRequest
        {
            Name = "Operator",
            Description = "Can operate",
            PermissionKeys = new List<string>()
        });

        result.Should().NotBeNull();
        result.Name.Should().Be("Operator");
    }

    [Fact]
    public async Task CreateRoleAsync_WithEmptyName_ThrowsArgumentException()
    {
        var action = () => _service.CreateRoleAsync(Guid.NewGuid(), new CreateRoleRequest
        {
            Name = "",
            Description = "Test",
            PermissionKeys = new List<string>()
        });

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteRoleAsync_SystemRole_ThrowsInvalidOperationException()
    {
        _repoMock.Setup(r => r.GetRoleByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Role { IsSystemRole = true });

        var action = () => _service.DeleteRoleAsync(Guid.NewGuid(), Guid.NewGuid());
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteRoleAsync_RoleInUse_ThrowsInvalidOperationException()
    {
        var roleId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetRoleByIdAsync(roleId))
            .ReturnsAsync(new Role { Id = roleId, IsSystemRole = false });
        _repoMock.Setup(r => r.IsRoleInUseAsync(roleId))
            .ReturnsAsync(true);

        var action = () => _service.DeleteRoleAsync(roleId, Guid.NewGuid());
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateRoleAsync_SystemRole_ThrowsInvalidOperationException()
    {
        _repoMock.Setup(r => r.GetRoleByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Role { IsSystemRole = true });

        var action = () => _service.UpdateRoleAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateRoleRequest());
        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
