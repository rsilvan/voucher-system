namespace VoucherSystem.Domain;

public class Promotion
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? CampaignId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = nameof(PromotionType.Automatic);
    public string Status { get; set; } = nameof(PromotionStatus.Draft);
    public int Priority { get; set; }

    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }

    public string? Metadata { get; set; } // JSONB

    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }

    // RowVersion for concurrency
    public uint RowVersion { get; set; }

    // Navigation
    public List<DiscountDefinition> DiscountDefinitions { get; set; } = new();
}
