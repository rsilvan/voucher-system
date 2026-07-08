using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class PromotionRepository : IPromotionRepository
{
    private readonly VoucherSystemDbContext _db;

    public PromotionRepository(VoucherSystemDbContext db) => _db = db;

    public async Task<Promotion?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id)
        => await _db.Promotions
            .Include(p => p.DiscountDefinitions)
            .ThenInclude(d => d.Tiers)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId && p.ProjectId == projectId);

    public async Task<List<Promotion>> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status)
    {
        var query = _db.Promotions.Where(p => p.OrganizationId == organizationId && p.ProjectId == projectId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        return await query.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetCountAsync(Guid organizationId, Guid projectId, string? status)
    {
        var query = _db.Promotions.Where(p => p.OrganizationId == organizationId && p.ProjectId == projectId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        return await query.CountAsync();
    }

    public async Task<List<Promotion>> GetActivePromotionsAsync(Guid organizationId, Guid projectId)
        => await _db.Promotions
            .Where(p => p.OrganizationId == organizationId && p.ProjectId == projectId && p.Status == nameof(PromotionStatus.Active))
            .OrderBy(p => p.Priority)
            .ToListAsync();

    public async Task AddAsync(Promotion promotion) { _db.Promotions.Add(promotion); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(Promotion promotion) { _db.Promotions.Update(promotion); await _db.SaveChangesAsync(); }

    public async Task<List<DiscountDefinition>> GetDiscountDefinitionsAsync(Guid promotionId)
        => await _db.Set<DiscountDefinition>()
            .Include(d => d.Tiers)
            .Where(d => d.PromotionId == promotionId).ToListAsync();

    public async Task AddDiscountDefinitionAsync(DiscountDefinition definition)
    {
        _db.Set<DiscountDefinition>().Add(definition);
        await _db.SaveChangesAsync();
    }

    public async Task AddPreviewAsync(PromotionPreview preview) { _db.Set<PromotionPreview>().Add(preview); await _db.SaveChangesAsync(); }
}
