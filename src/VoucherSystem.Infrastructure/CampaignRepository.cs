using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class CampaignRepository : ICampaignRepository
{
    private readonly VoucherSystemDbContext _db;

    public CampaignRepository(VoucherSystemDbContext db) => _db = db;

    public async Task<Campaign?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id)
        => await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.ProjectId == projectId);

    public async Task<List<Campaign>> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status)
    {
        var query = _db.Campaigns.Where(c => c.OrganizationId == organizationId && c.ProjectId == projectId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);
        return await query.OrderByDescending(c => c.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetCountAsync(Guid organizationId, Guid projectId, string? status)
    {
        var query = _db.Campaigns.Where(c => c.OrganizationId == organizationId && c.ProjectId == projectId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);
        return await query.CountAsync();
    }

    public async Task AddAsync(Campaign campaign) { _db.Campaigns.Add(campaign); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(Campaign campaign) { _db.Campaigns.Update(campaign); await _db.SaveChangesAsync(); }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid projectId, Guid id)
    {
        var campaign = await GetByIdAsync(organizationId, projectId, id);
        if (campaign is null) return false;
        campaign.Status = nameof(CampaignStatus.Archived);
        campaign.ArchivedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<CampaignCategory>> GetCategoriesAsync(Guid organizationId, Guid projectId)
        => await _db.Set<CampaignCategory>().Where(c => c.OrganizationId == organizationId && c.ProjectId == projectId).ToListAsync();

    public async Task<CampaignCategory?> GetCategoryByIdAsync(Guid organizationId, Guid projectId, Guid id)
        => await _db.Set<CampaignCategory>().FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.ProjectId == projectId);

    public async Task AddCategoryAsync(CampaignCategory category) { _db.Set<CampaignCategory>().Add(category); await _db.SaveChangesAsync(); }

    public async Task<List<CampaignTemplate>> GetTemplatesAsync(Guid organizationId, Guid projectId)
        => await _db.Set<CampaignTemplate>().Where(t => t.OrganizationId == organizationId && t.ProjectId == projectId).ToListAsync();

    public async Task<CampaignTemplate?> GetTemplateByIdAsync(Guid organizationId, Guid projectId, Guid id)
        => await _db.Set<CampaignTemplate>().FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == organizationId && t.ProjectId == projectId);

    public async Task AddVersionAsync(CampaignVersion version) { _db.Set<CampaignVersion>().Add(version); await _db.SaveChangesAsync(); }

    public async Task<List<CampaignVersion>> GetVersionsAsync(Guid campaignId)
        => await _db.Set<CampaignVersion>().Where(v => v.CampaignId == campaignId).OrderByDescending(v => v.Version).ToListAsync();
}
