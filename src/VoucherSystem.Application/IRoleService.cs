using VoucherSystem.Contracts;

namespace VoucherSystem.Application;

public interface IRoleService
{
    Task<List<RoleResponse>> GetRolesAsync(Guid? organizationId);
    Task<RoleResponse> CreateRoleAsync(Guid? organizationId, CreateRoleRequest request);
    Task<RoleResponse> UpdateRoleAsync(Guid roleId, Guid? organizationId, UpdateRoleRequest request);
    Task DeleteRoleAsync(Guid roleId, Guid? organizationId);
}
