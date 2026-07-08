namespace VoucherSystem.Domain;

public class VoucherBatch
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? CampaignId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Status { get; set; } = nameof(VoucherBatchStatus.Pending);

    public int Count { get; set; }
    public int GeneratedCount { get; set; }

    // Pattern config stored as JSON
    public string? PatternConfig { get; set; }

    // Error info
    public string? ErrorMessage { get; set; }

    public string? Metadata { get; set; } // JSONB

    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
