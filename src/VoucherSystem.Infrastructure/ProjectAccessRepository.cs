using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class ProjectAccessRepository : IProjectAccessRepository
{
    private readonly VoucherSystemDbContext _db;

    public ProjectAccessRepository(VoucherSystemDbContext db) => _db = db;

    public async Task<List<ProjectAccess>> GetMemberProjectAccessAsync(Guid memberId)
    {
        return await _db.ProjectAccesses
            .Where(pa => pa.OrganizationMemberId == memberId)
            .ToListAsync();
    }

    public async Task SetMemberProjectAccessAsync(Guid memberId, Guid roleId, List<Guid> projectIds)
    {
        var existing = await _db.ProjectAccesses
            .Where(pa => pa.OrganizationMemberId == memberId)
            .ToListAsync();
        _db.ProjectAccesses.RemoveRange(existing);

        foreach (var pid in projectIds)
        {
            _db.ProjectAccesses.Add(new ProjectAccess
            {
                Id = Guid.NewGuid(),
                OrganizationMemberId = memberId,
                ProjectId = pid,
                RoleId = roleId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<Guid>> GetMemberProjectIdsAsync(Guid memberId)
    {
        return await _db.ProjectAccesses
            .Where(pa => pa.OrganizationMemberId == memberId)
            .Select(pa => pa.ProjectId)
            .ToListAsync();
    }

    public async Task<bool> HasProjectAccessAsync(Guid memberId, Guid projectId)
    {
        return await _db.ProjectAccesses
            .AnyAsync(pa => pa.OrganizationMemberId == memberId && pa.ProjectId == projectId);
    }
}
