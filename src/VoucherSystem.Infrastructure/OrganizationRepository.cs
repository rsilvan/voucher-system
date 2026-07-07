using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly VoucherSystemDbContext _db;

    public OrganizationRepository(VoucherSystemDbContext db)
    {
        _db = db;
    }

    public async Task<bool> EmailExistsAsync(string normalizedEmail)
    {
        return await _db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail);
    }

    public async Task<Plan?> GetTrialPlanAsync()
    {
        return await _db.Plans.FirstOrDefaultAsync(p => p.Key == "Trial");
    }

    public async Task<Role?> GetSystemRoleByKeyAsync(string key)
    {
        return await _db.Roles.FirstOrDefaultAsync(r => r.Key == key && r.IsSystemRole);
    }

    public async Task<Organization?> GetOrganizationByIdAsync(Guid organizationId)
    {
        return await _db.Organizations.FindAsync(organizationId);
    }

    public async Task<Plan?> GetPlanByIdAsync(Guid planId)
    {
        return await _db.Plans.FindAsync(planId);
    }

    public async Task SaveOrganizationAsync(Organization organization, User user, Project project, OrganizationMember member)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.Organizations.Add(organization);
            _db.Users.Add(user);
            _db.Projects.Add(project);
            _db.OrganizationMembers.Add(member);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
