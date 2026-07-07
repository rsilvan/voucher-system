using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IPermissionCache
{
    Task<List<string>> GetPermissionsAsync(Guid roleId);
    Task SetPermissionsAsync(Guid roleId, List<Permission> permissions);
    Task InvalidateRoleAsync(Guid roleId);
}
