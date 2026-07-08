namespace VoucherSystem.Domain;

public class DiscountDefinition
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }

    public string DiscountType { get; set; } = nameof(Domain.DiscountType.Percentage);
    public string ValueType { get; set; } = "Percentage"; // Percentage / Fixed
    public decimal Value { get; set; }

    public decimal? MaxDiscount { get; set; }
    public decimal? MinPurchase { get; set; }

    public string ApplyTo { get; set; } = nameof(Domain.DiscountApplyTo.Order);
    public string? ItemScope { get; set; } // JSON — which SKUs/products/collections

    // Buy X Get Y
    public int? BuyXQuantity { get; set; }
    public int? GetYQuantity { get; set; }
    public decimal? GetYDiscountPercent { get; set; }

    public string? Metadata { get; set; } // JSONB

    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public List<DiscountTier> Tiers { get; set; } = new();
}
