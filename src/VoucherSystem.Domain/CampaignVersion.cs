namespace VoucherSystem.Domain;

public class CampaignVersion
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }

    public int Version { get; set; }
    public string Status { get; set; } = "Draft"; // Draft / Published / Superseded
    public string? Config { get; set; } // JSON — snapshot of campaign config at publication

    public Guid PublishedBy { get; set; }
    public DateTimeOffset PublishedAt { get; set; }

    public string? Metadata { get; set; } // JSONB
    public DateTimeOffset CreatedAt { get; set; }
}
