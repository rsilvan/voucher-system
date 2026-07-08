namespace VoucherSystem.Domain;

public class CampaignCategory
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Color { get; set; }

    public string? Metadata { get; set; } // JSONB

    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
