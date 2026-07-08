namespace VoucherSystem.Domain;

public class CampaignApproval
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }

    public string Status { get; set; } = "Pending"; // Pending / Approved / Rejected
    public Guid? ApprovedBy { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? Reason { get; set; }

    public string? Metadata { get; set; } // JSONB

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ActionedAt { get; set; }
}
