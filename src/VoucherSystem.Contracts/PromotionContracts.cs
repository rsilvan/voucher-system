using VoucherSystem.Domain;

namespace VoucherSystem.Contracts.Promotions;

public class PromotionPlanResponse
{
    public List<PromotionPlanItemResponse> Items { get; set; } = new();
    public bool HasConflicts { get; set; }
    public bool HasUnsupported { get; set; }
}

public class PromotionPlanItemResponse
{
    public string ResourceType { get; set; } = default!;
    public string SourceName { get; set; } = default!;
    public string? SourceId { get; set; }
    public string Action { get; set; } = default!;
    public string? Detail { get; set; }
}

public class CreatePromotionRequest
{
    public Guid TargetProjectId { get; set; }
    public string? IdempotencyKey { get; set; }
    public List<string> ResourceTypes { get; set; } = new() { "BrandProfile" };
}

public class PromotionJobResponse
{
    public Guid Id { get; set; }
    public Guid SourceProjectId { get; set; }
    public Guid TargetProjectId { get; set; }
    public string Status { get; set; } = default!;
    public string? PlanJson { get; set; }
    public string? ResultJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class PromotionSummaryResponse
{
    public int TotalCampaigns { get; set; }
    public int TotalVouchers { get; set; }
    public int TotalValidations { get; set; }
    public int TotalRedemptions { get; set; }
    public int TotalFailed { get; set; }
}

public class UsageResponse
{
    public int ActiveProjects { get; set; }
    public int TotalProjects { get; set; }
    public int ActiveCampaigns { get; set; }
    public int CurrentApiCalls { get; set; }
    public int MaxProjects { get; set; }
    public int MaxCampaigns { get; set; }
    public int MaxApiCalls { get; set; }
}

public class PromotionDiffResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = default!;
    public List<PromotionDiffItem> Items { get; set; } = new();
}

public class PromotionDiffItem
{
    public string ResourceType { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string? SourceId { get; set; }
    public string? SourceName { get; set; }
    public string? TargetId { get; set; }
    public string? Detail { get; set; }
    public string Status { get; set; } = default!; // "planned", "completed", "pending"
}

public class PromotionProgressResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = default!;
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public int FailedSteps { get; set; }
    public int PendingSteps { get; set; }
}
