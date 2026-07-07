using VoucherSystem.Contracts;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _repo;
    private readonly IOrganizationRepository _orgRepo;

    public RoleService(IRoleRepository repo, IOrganizationRepository orgRepo)
    {
        _repo = repo;
        _orgRepo = orgRepo;
    }

    public async Task<List<RoleResponse>> GetRolesAsync(Guid? organizationId)
    {
        var roles = await _repo.GetRolesAsync(organizationId);
        var result = new List<RoleResponse>();
        foreach (var r in roles)
        {
            var perms = await _repo.GetRolePermissionsAsync(r.Id);
            result.Add(MapRole(r, perms));
        }
        return result;
    }

    public async Task<RoleResponse> CreateRoleAsync(Guid? organizationId, CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Role name is required.");

        // Validate plan allows custom roles
        if (organizationId.HasValue)
        {
            var org = await _orgRepo.GetOrganizationByIdAsync(organizationId.Value);
            if (org != null)
            {
                var plan = await _orgRepo.GetPlanByIdAsync(org.PlanId);
                if (plan != null && !plan.AllowsCustomRoles)
                    throw new InvalidOperationException("Your plan does not allow custom roles.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var key = request.Name.ToUpper().Replace(" ", "_").Replace("-", "_")[..Math.Min(100, request.Name.Length)];

        var role = new Role
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name,
            Key = key,
            Description = request.Description,
            IsSystemRole = false,
            Scope = "Organization",
            CreatedAt = now
        };

        await _repo.CreateRoleAsync(role);

        if (request.PermissionKeys.Count > 0)
            await _repo.SetRolePermissionsAsync(role.Id, request.PermissionKeys);

        var perms = await _repo.GetRolePermissionsAsync(role.Id);
        return MapRole(role, perms);
    }

    public async Task<RoleResponse> UpdateRoleAsync(Guid roleId, Guid? organizationId, UpdateRoleRequest request)
    {
        var role = await _repo.GetRoleByIdAsync(roleId)
            ?? throw new ArgumentException("Role not found.");

        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot modify system roles.");

        if (request.Name != null) role.Name = request.Name;
        if (request.Description != null) role.Description = request.Description;
        role.UpdatedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateRoleAsync(role);

        if (request.PermissionKeys != null)
            await _repo.SetRolePermissionsAsync(roleId, request.PermissionKeys);

        var perms = await _repo.GetRolePermissionsAsync(roleId);
        return MapRole(role, perms);
    }

    public async Task DeleteRoleAsync(Guid roleId, Guid? organizationId)
    {
        var role = await _repo.GetRoleByIdAsync(roleId)
            ?? throw new ArgumentException("Role not found.");

        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot delete system roles.");

        if (await _repo.IsRoleInUseAsync(roleId))
            throw new InvalidOperationException("Cannot delete a role that is assigned to members.");

        await _repo.DeleteRoleAsync(roleId);
    }

    private static RoleResponse MapRole(Role r, List<Permission> perms) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Key = r.Key,
        Description = r.Description,
        IsSystemRole = r.IsSystemRole,
        Scope = r.Scope,
        Permissions = perms.Select(p => p.Key).ToList(),
        CreatedAt = r.CreatedAt
    };
}
