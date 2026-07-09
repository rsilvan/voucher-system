using System.Text.Json;
using VoucherSystem.Contracts.Campaigns;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class CampaignService : ICampaignService
{
    private readonly ICampaignRepository _repo;
    private readonly IAuditLogWriter _audit;

    public CampaignService(ICampaignRepository repo, IAuditLogWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<CampaignResponse> CreateAsync(Guid organizationId, Guid projectId, CreateCampaignRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Campaign name is required.");

        ValidateCampaignType(request.Type);
        if (!string.IsNullOrWhiteSpace(request.Timezone))
            ValidateTimeZone(request.Timezone);

        var now = DateTimeOffset.UtcNow;
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Type = request.Type,
            Status = request.Status ?? nameof(CampaignStatus.Draft),
            Priority = request.Priority,
            CategoryId = request.CategoryId,
            TemplateId = request.TemplateId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Timezone = request.Timezone,
            IsRecurring = request.IsRecurring,
            RecurrenceRule = request.RecurrenceRule,
            MaxRedemptions = request.MaxRedemptions,
            MaxRedemptionsPerCustomer = request.MaxRedemptionsPerCustomer,
            BudgetAmount = request.BudgetAmount,
            Currency = request.Currency,
            Metadata = request.Metadata,
            CreatedBy = userId,
            CreatedAt = now
        };

        await _repo.AddAsync(campaign);
        _audit.Write(organizationId, projectId, userId, "campaign.created", "Campaign", campaign.Id.ToString());

        return MapToResponse(campaign);
    }

    public async Task<CampaignResponse?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id)
    {
        var campaign = await _repo.GetByIdAsync(organizationId, projectId, id);
        return campaign is null ? null : MapToResponse(campaign);
    }

    public async Task<CampaignListResponse> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status)
    {
        var items = await _repo.GetListAsync(organizationId, projectId, page, pageSize, status);
        var total = await _repo.GetCountAsync(organizationId, projectId, status);

        return new CampaignListResponse
        {
            Items = items.Select(c => new CampaignSummaryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                Status = c.Status,
                Priority = c.Priority,
                StartAt = c.StartAt,
                EndAt = c.EndAt,
                CreatedAt = c.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<CampaignResponse?> UpdateAsync(Guid organizationId, Guid projectId, Guid id, UpdateCampaignRequest request, Guid userId)
    {
        var campaign = await _repo.GetByIdAsync(organizationId, projectId, id);
        if (campaign is null) return null;

        if (request.Name is not null) campaign.Name = request.Name.Trim();
        if (request.Description is not null) campaign.Description = request.Description.Trim();
        if (request.Priority.HasValue) campaign.Priority = request.Priority.Value;
        if (request.CategoryId is not null) campaign.CategoryId = request.CategoryId;
        if (request.StartAt is not null) campaign.StartAt = request.StartAt;
        if (request.EndAt is not null) campaign.EndAt = request.EndAt;
        if (request.Timezone is not null) campaign.Timezone = request.Timezone;
        if (request.MaxRedemptions is not null) campaign.MaxRedemptions = request.MaxRedemptions;
        if (request.MaxRedemptionsPerCustomer is not null) campaign.MaxRedemptionsPerCustomer = request.MaxRedemptionsPerCustomer;
        if (request.BudgetAmount is not null) campaign.BudgetAmount = request.BudgetAmount;
        if (request.Currency is not null) campaign.Currency = request.Currency;
        if (request.Metadata is not null) campaign.Metadata = request.Metadata;

        campaign.UpdatedAt = DateTimeOffset.UtcNow;
        campaign.UpdatedBy = userId;
        await _repo.UpdateAsync(campaign);

        _audit.Write(organizationId, projectId, userId, "campaign.updated", "Campaign", campaign.Id.ToString());
        return MapToResponse(campaign);
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid projectId, Guid id)
    {
        return await _repo.DeleteAsync(organizationId, projectId, id);
    }

    public async Task<CampaignActionResponse> ExecuteActionAsync(Guid organizationId, Guid projectId, Guid id, string action, CampaignActionRequest request, Guid userId)
    {
        var campaign = await _repo.GetByIdAsync(organizationId, projectId, id)
            ?? throw new ArgumentException("Campaign not found.");

        var validTransitions = GetValidTransitions(campaign.Status);
        if (!validTransitions.Contains(action))
            throw new CampaignStateMachineException($"Cannot transition campaign from '{campaign.Status}' to '{action}'.");

        campaign.Status = action switch
        {
            "activate" => nameof(CampaignStatus.Active),
            "pause" => nameof(CampaignStatus.Paused),
            "end" => nameof(CampaignStatus.Ended),
            "archive" => nameof(CampaignStatus.Archived),
            "publish" => campaign.Status == nameof(CampaignStatus.Draft) ? nameof(CampaignStatus.Scheduled) : campaign.Status,
            _ => throw new ArgumentException($"Unknown action '{action}'.")
        };

        campaign.UpdatedAt = DateTimeOffset.UtcNow;
        campaign.UpdatedBy = userId;
        await _repo.UpdateAsync(campaign);

        // Create a published version on publish
        if (action == "publish")
        {
            var version = new CampaignVersion
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                OrganizationId = organizationId,
                ProjectId = projectId,
                Version = 1,
                Status = "Published",
                Config = JsonSerializer.Serialize(new
                {
                    campaign.Name, campaign.Description, campaign.Type,
                    campaign.StartAt, campaign.EndAt, campaign.MaxRedemptions,
                    campaign.BudgetAmount, campaign.Currency
                }),
                PublishedBy = userId,
                PublishedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _repo.AddVersionAsync(version);
        }

        _audit.Write(organizationId, projectId, userId, $"campaign.{action}", "Campaign", campaign.Id.ToString());

        return new CampaignActionResponse
        {
            OperationId = campaign.Id.ToString(),
            Status = campaign.Status,
            Events = new List<string> { $"campaign.{action}" },
            Warnings = new List<string>()
        };
    }

    public async Task<List<CampaignCategoryResponse>> GetCategoriesAsync(Guid organizationId, Guid projectId)
    {
        var categories = await _repo.GetCategoriesAsync(organizationId, projectId);
        return categories.Select(c => new CampaignCategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Color = c.Color,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<List<CampaignTemplateResponse>> GetTemplatesAsync(Guid organizationId, Guid projectId)
    {
        var templates = await _repo.GetTemplatesAsync(organizationId, projectId);
        return templates.Select(t => new CampaignTemplateResponse
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Type = t.Type,
            IsSystem = t.IsSystem,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    private static CampaignResponse MapToResponse(Campaign c) => new()
    {
        Id = c.Id,
        OrganizationId = c.OrganizationId,
        ProjectId = c.ProjectId,
        Name = c.Name,
        Description = c.Description,
        Type = c.Type,
        Status = c.Status,
        Priority = c.Priority,
        CategoryId = c.CategoryId,
        TemplateId = c.TemplateId,
        StartAt = c.StartAt,
        EndAt = c.EndAt,
        Timezone = c.Timezone,
        IsRecurring = c.IsRecurring,
        RecurrenceRule = c.RecurrenceRule,
        MaxRedemptions = c.MaxRedemptions,
        MaxRedemptionsPerCustomer = c.MaxRedemptionsPerCustomer,
        BudgetAmount = c.BudgetAmount,
        Currency = c.Currency,
        Metadata = c.Metadata,
        CreatedBy = c.CreatedBy,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        RowVersion = c.RowVersion
    };

    private static HashSet<string> GetValidTransitions(string status) => status switch
    {
        "Draft" => new() { "publish", "archive" },
        "Scheduled" => new() { "activate", "pause", "end", "archive" },
        "Active" => new() { "pause", "end" },
        "Paused" => new() { "activate", "end" },
        "Ended" => new() { "archive" },
        "Archived" => new(),
        _ => new()
    };

    internal static void ValidateCampaignType(string type)
    {
        var valid = new[] { "Coupon", "Promotion", "GiftCard", "Loyalty", "Referral" };
        if (!valid.Contains(type))
            throw new ArgumentException($"Invalid campaign type '{type}'.");
    }

    internal static void ValidateTimeZone(string tz)
    {
        try { TimeZoneInfo.FindSystemTimeZoneById(tz); }
        catch { throw new ArgumentException($"Invalid time zone '{tz}'."); }
    }
}
