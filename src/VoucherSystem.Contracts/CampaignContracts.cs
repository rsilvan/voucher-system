namespace VoucherSystem.Contracts.Campaigns;

public class CampaignResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int Priority { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? TemplateId { get; set; }
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string? Timezone { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public int? MaxRedemptions { get; set; }
    public int? MaxRedemptionsPerCustomer { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? Currency { get; set; }
    public string? Metadata { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
}

public class CreateCampaignRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = "Coupon";
    public string? Status { get; set; }
    public int Priority { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? TemplateId { get; set; }
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string? Timezone { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public int? MaxRedemptions { get; set; }
    public int? MaxRedemptionsPerCustomer { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? Currency { get; set; }
    public string? Metadata { get; set; }
}

public class UpdateCampaignRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public int? Priority { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? TemplateId { get; set; }
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string? Timezone { get; set; }
    public bool? IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public int? MaxRedemptions { get; set; }
    public int? MaxRedemptionsPerCustomer { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? Currency { get; set; }
    public string? Metadata { get; set; }
    public uint RowVersion { get; set; }
}

public class CampaignListResponse
{
    public List<CampaignSummaryResponse> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class CampaignSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int Priority { get; set; }
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// ─── Campaign Action ───

public class CampaignActionRequest
{
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? EffectiveAt { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Metadata { get; set; }
}

public class CampaignActionResponse
{
    public string OperationId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public List<string> Events { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

// ─── Category ───

public class CampaignCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// ─── Template ───

public class CampaignTemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// ─── Version ───

public class CampaignVersionResponse
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = default!;
    public string? Config { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
}
