using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class ProjectRepository : IProjectRepository
{
    private readonly VoucherSystemDbContext _db;

    public ProjectRepository(VoucherSystemDbContext db)
    {
        _db = db;
    }

    public async Task<Project?> GetByIdAsync(Guid projectId, Guid organizationId)
        => await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == organizationId
                && p.Status != nameof(ProjectStatus.PendingDeletion));

    public async Task<List<Project>> GetByOrganizationAsync(Guid organizationId)
        => await _db.Projects
            .Where(p => p.OrganizationId == organizationId && p.Status != nameof(ProjectStatus.PendingDeletion))
            .OrderByDescending(p => p.IsPrimary)
            .ThenBy(p => p.Name)
            .ToListAsync();

    public async Task<List<Project>> GetByMemberAsync(Guid organizationId, Guid memberId)
    {
        var memberProjectIds = await _db.ProjectAccesses
            .Where(pa => pa.OrganizationMemberId == memberId)
            .Select(pa => pa.ProjectId)
            .ToListAsync();

        var orgProjects = await _db.Projects
            .Where(p => p.OrganizationId == organizationId && p.Status != nameof(ProjectStatus.PendingDeletion))
            .ToListAsync();

        return orgProjects
            .Where(p => memberProjectIds.Contains(p.Id))
            .OrderByDescending(p => p.IsPrimary)
            .ThenBy(p => p.Name)
            .ToList();
    }

    public async Task<bool> SlugExistsAsync(Guid organizationId, string slug)
        => await _db.Projects.AnyAsync(p => p.OrganizationId == organizationId && p.Slug == slug);

    public async Task<Project?> GetPrimaryAsync(Guid organizationId)
        => await _db.Projects
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.IsPrimary
                && p.Status == nameof(ProjectStatus.Active));

    public async Task<int> GetActiveCountAsync(Guid organizationId)
        => await _db.Projects
            .CountAsync(p => p.OrganizationId == organizationId && p.Status == nameof(ProjectStatus.Active));

    public async Task AddAsync(Project project)
    {
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Project project)
    {
        _db.Projects.Update(project);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> HasResourcesAsync(Guid projectId)
    {
        // Check if any operational entities reference this project
        var hasAudit = await _db.AuditLogs.AnyAsync(a => a.ProjectId == projectId);
        var hasAccess = await _db.ProjectAccesses.AnyAsync(pa => pa.ProjectId == projectId);
        return hasAudit || hasAccess;
    }
}
