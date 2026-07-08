using VoucherSystem.Contracts.Promotions;

namespace VoucherSystem.Application;

public interface IPromotionService
{
    // Promotion CRUD
    Task<PromotionResponse> CreateAsync(Guid organizationId, Guid projectId, CreatePromotionRequest request, Guid userId);
    Task<PromotionResponse?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id);
    Task<PromotionListResponse> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status);
    Task<PromotionResponse?> UpdateAsync(Guid organizationId, Guid projectId, Guid id, UpdatePromotionRequest request, Guid userId);
    Task<bool> DeleteAsync(Guid organizationId, Guid projectId, Guid id);

    // Actions
    Task<PromotionActionResponse> ExecuteActionAsync(Guid organizationId, Guid projectId, Guid id, string action, PromotionActionRequest request, Guid userId);

    // Preview
    Task<PromotionPreviewResponse> PreviewAsync(Guid organizationId, Guid projectId, PromotionPreviewRequest request, Guid userId);
}
