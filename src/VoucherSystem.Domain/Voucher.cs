namespace VoucherSystem.Domain;

public class Voucher
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? BatchId { get; set; }

    public string Code { get; set; } = default!;
    public string CodeMasked { get; set; } = default!;
    public string Type { get; set; } = nameof(VoucherType.Unique);
    public string Status { get; set; } = nameof(VoucherStatus.Active);

    // Discount info
    public string? DiscountType { get; set; } // Percentage / Fixed / FreeShipping / BuyXGetY
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinPurchase { get; set; }
    public string? Currency { get; set; }
    public string? ApplyTo { get; set; } // Order / Item / Sku / Product / Collection

    // Redemption limits
    public int RedemptionCount { get; set; }
    public int? MaxRedemptions { get; set; }
    public int? MaxRedemptionsPerCustomer { get; set; }

    // Holder
    public string? HolderId { get; set; }
    public string? HolderEmail { get; set; }

    // Dates
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public string? Metadata { get; set; } // JSONB

    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }
    public Guid? DisabledBy { get; set; }

    // RowVersion for concurrency
    public uint RowVersion { get; set; }
}
