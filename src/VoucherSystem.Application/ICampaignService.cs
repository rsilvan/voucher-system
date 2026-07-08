using VoucherSystem.Contracts.Campaigns;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface ICampaignRepository
{
    // Campaign CRUD
    Task<Campaign?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id);
    Task<List<Campaign>> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status);
    Task<int> GetCountAsync(Guid organizationId, Guid projectId, string? status);
    Task AddAsync(Campaign campaign);
    Task UpdateAsync(Campaign campaign);
    Task<bool> DeleteAsync(Guid organizationId, Guid projectId, Guid id);

    // Category
    Task<List<CampaignCategory>> GetCategoriesAsync(Guid organizationId, Guid projectId);
    Task<CampaignCategory?> GetCategoryByIdAsync(Guid organizationId, Guid projectId, Guid id);
    Task AddCategoryAsync(CampaignCategory category);

    // Template
    Task<List<CampaignTemplate>> GetTemplatesAsync(Guid organizationId, Guid projectId);
    Task<CampaignTemplate?> GetTemplateByIdAsync(Guid organizationId, Guid projectId, Guid id);

    // Version
    Task AddVersionAsync(CampaignVersion version);
    Task<List<CampaignVersion>> GetVersionsAsync(Guid campaignId);
}

public interface ICampaignService
{
    Task<CampaignResponse> CreateAsync(Guid organizationId, Guid projectId, CreateCampaignRequest request, Guid userId);
    Task<CampaignResponse?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id);
    Task<CampaignListResponse> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status);
    Task<CampaignResponse?> UpdateAsync(Guid organizationId, Guid projectId, Guid id, UpdateCampaignRequest request, Guid userId);
    Task<bool> DeleteAsync(Guid organizationId, Guid projectId, Guid id);
    Task<CampaignActionResponse> ExecuteActionAsync(Guid organizationId, Guid projectId, Guid id, string action, CampaignActionRequest request, Guid userId);

    // Categories
    Task<List<CampaignCategoryResponse>> GetCategoriesAsync(Guid organizationId, Guid projectId);

    // Templates
    Task<List<CampaignTemplateResponse>> GetTemplatesAsync(Guid organizationId, Guid projectId);
}
