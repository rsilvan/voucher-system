namespace VoucherSystem.Domain;

public class CampaignTemplate
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = nameof(CampaignType.Coupon);
    public string? Config { get; set; } // JSON — template configuration

    public bool IsSystem { get; set; }

    public string? Metadata { get; set; } // JSONB

    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
