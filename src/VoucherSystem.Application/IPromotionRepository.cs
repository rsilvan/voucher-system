using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id);
    Task<List<Promotion>> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status);
    Task<int> GetCountAsync(Guid organizationId, Guid projectId, string? status);
    Task<List<Promotion>> GetActivePromotionsAsync(Guid organizationId, Guid projectId);
    Task AddAsync(Promotion promotion);
    Task UpdateAsync(Promotion promotion);

    // Discount Definitions
    Task<List<DiscountDefinition>> GetDiscountDefinitionsAsync(Guid promotionId);
    Task AddDiscountDefinitionAsync(DiscountDefinition definition);

    // Preview
    Task AddPreviewAsync(PromotionPreview preview);
}
