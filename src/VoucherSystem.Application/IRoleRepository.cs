using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IRoleRepository
{
    Task<List<Role>> GetRolesAsync(Guid? organizationId);
    Task<Role?> GetRoleByIdAsync(Guid roleId);
    Task<Role> CreateRoleAsync(Role role);
    Task<Role> UpdateRoleAsync(Role role);
    Task DeleteRoleAsync(Guid roleId);
    Task<bool> IsRoleInUseAsync(Guid roleId);
    Task<List<Permission>> GetAllPermissionsAsync();
    Task<List<Permission>> GetRolePermissionsAsync(Guid roleId);
    Task SetRolePermissionsAsync(Guid roleId, List<string> permissionKeys);
}
