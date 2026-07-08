namespace VoucherSystem.Domain;

public class PromotionPreview
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? PromotionId { get; set; }
    public Guid? CampaignId { get; set; }

    // Order context (JSON)
    public string? OrderContext { get; set; }

    // Calculated result (JSON)
    public string? CalculatedDiscounts { get; set; }
    public decimal? TotalDiscount { get; set; }
    public decimal? FinalTotal { get; set; }
    public string? Currency { get; set; }

    // Breakdown per item
    public string? ItemBreakdown { get; set; } // JSON

    public string? Metadata { get; set; } // JSONB

    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
