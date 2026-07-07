using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IPromotionRepository
{
    Task<ProjectPromotionJob?> GetJobByIdAsync(Guid jobId, Guid organizationId);
    Task<List<ProjectPromotionJob>> GetJobsByProjectAsync(Guid projectId, Guid organizationId);
    Task<ProjectPromotionJob?> GetByIdempotencyKeyAsync(string idempotencyKey, Guid organizationId);
    Task<List<ProjectPromotionJob>> GetJobsByStatusAsync(params string[] statuses);
    Task AddJobAsync(ProjectPromotionJob job);
    Task UpdateJobAsync(ProjectPromotionJob job);
}
