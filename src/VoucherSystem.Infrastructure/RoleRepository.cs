using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class RoleRepository : IRoleRepository
{
    private readonly VoucherSystemDbContext _db;

    public RoleRepository(VoucherSystemDbContext db) => _db = db;

    public async Task<List<Role>> GetRolesAsync(Guid? organizationId)
    {
        var query = _db.Roles.Where(r => r.OrganizationId == null);
        if (organizationId.HasValue)
            query = query.Concat(_db.Roles.Where(r => r.OrganizationId == organizationId));
        return await query.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<Role?> GetRoleByIdAsync(Guid roleId) => await _db.Roles.FindAsync(roleId);

    public async Task<Role> CreateRoleAsync(Role role)
    {
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return role;
    }

    public async Task<Role> UpdateRoleAsync(Role role)
    {
        _db.Roles.Update(role);
        await _db.SaveChangesAsync();
        return role;
    }

    public async Task DeleteRoleAsync(Guid roleId)
    {
        var role = await _db.Roles.FindAsync(roleId);
        if (role != null)
        {
            // Remove role permissions
            var rps = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            _db.RolePermissions.RemoveRange(rps);
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsRoleInUseAsync(Guid roleId)
        => await _db.OrganizationMembers.AnyAsync(m => m.RoleId == roleId);

    public async Task<List<Permission>> GetAllPermissionsAsync()
        => await _db.Permissions.OrderBy(p => p.Key).ToListAsync();

    public async Task<List<Permission>> GetRolePermissionsAsync(Guid roleId)
        => await _db.RolePermissions.Where(rp => rp.RoleId == roleId)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p)
            .ToListAsync();

    public async Task SetRolePermissionsAsync(Guid roleId, List<string> permissionKeys)
    {
        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(existing);

        var perms = await _db.Permissions.Where(p => permissionKeys.Contains(p.Key)).ToListAsync();
        foreach (var p in perms)
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = p.Id });

        await _db.SaveChangesAsync();
    }
}
