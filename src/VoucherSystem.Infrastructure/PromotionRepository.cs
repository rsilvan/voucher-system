using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class PromotionRepository : IPromotionRepository
{
    private readonly VoucherSystemDbContext _db;

    public PromotionRepository(VoucherSystemDbContext db)
    {
        _db = db;
    }

    public async Task<ProjectPromotionJob?> GetJobByIdAsync(Guid jobId, Guid organizationId)
        => await _db.Set<ProjectPromotionJob>()
            .FirstOrDefaultAsync(j => j.Id == jobId && j.OrganizationId == organizationId);

    public async Task<List<ProjectPromotionJob>> GetJobsByProjectAsync(Guid projectId, Guid organizationId)
        => await _db.Set<ProjectPromotionJob>()
            .Where(j => j.OrganizationId == organizationId && (j.SourceProjectId == projectId || j.TargetProjectId == projectId))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

    public async Task<ProjectPromotionJob?> GetByIdempotencyKeyAsync(string idempotencyKey, Guid organizationId)
        => await _db.Set<ProjectPromotionJob>()
            .FirstOrDefaultAsync(j => j.IdempotencyKey == idempotencyKey && j.OrganizationId == organizationId);

    public async Task<List<ProjectPromotionJob>> GetJobsByStatusAsync(params string[] statuses)
        => await _db.Set<ProjectPromotionJob>()
            .Where(j => statuses.Contains(j.Status))
            .OrderBy(j => j.CreatedAt)
            .ToListAsync();

    public async Task AddJobAsync(ProjectPromotionJob job)
    {
        _db.Set<ProjectPromotionJob>().Add(job);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateJobAsync(ProjectPromotionJob job)
    {
        _db.Set<ProjectPromotionJob>().Update(job);
        await _db.SaveChangesAsync();
    }
}
