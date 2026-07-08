namespace VoucherSystem.Domain;

public class Campaign
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = nameof(CampaignType.Coupon);
    public string Status { get; set; } = nameof(CampaignStatus.Draft);
    public int Priority { get; set; }

    public Guid? CategoryId { get; set; }
    public Guid? TemplateId { get; set; }

    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string? Timezone { get; set; }

    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; } // RFC 5545 RRULE

    public int? MaxRedemptions { get; set; }
    public int? MaxRedemptionsPerCustomer { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? Currency { get; set; }

    public string? Metadata { get; set; } // JSONB

    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid? ArchivedBy { get; set; }

    // RowVersion for concurrency
    public uint RowVersion { get; set; }

    // Navigation
    public List<CampaignVersion> Versions { get; set; } = new();
    public List<CampaignApproval> Approvals { get; set; } = new();
}
