using VoucherSystem.Contracts.Promotions;

namespace VoucherSystem.Application;

public interface IPromotionService
{
    Task<PromotionPlanResponse> GetPlanAsync(Guid organizationId, Guid sourceProjectId, Guid targetProjectId);
    Task<PromotionJobResponse> CreatePromotionAsync(Guid organizationId, Guid sourceProjectId, CreatePromotionRequest request, Guid actorId);
    Task<PromotionJobResponse?> GetJobAsync(Guid organizationId, Guid jobId);
    Task<List<PromotionJobResponse>> GetJobsAsync(Guid organizationId, Guid projectId);
    Task<PromotionDiffResponse> GetPromotionDiffAsync(Guid organizationId, Guid jobId);
    Task<bool> CancelJobAsync(Guid organizationId, Guid jobId);
}
