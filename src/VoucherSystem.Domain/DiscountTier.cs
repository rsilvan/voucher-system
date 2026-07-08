namespace VoucherSystem.Domain;

public class DiscountTier
{
    public Guid Id { get; set; }
    public Guid DiscountDefinitionId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }

    public decimal FromValue { get; set; } // e.g. min purchase amount
    public decimal? ToValue { get; set; }  // e.g. max purchase amount (null = unlimited)
    public decimal DiscountValue { get; set; }
    public string DiscountType { get; set; } = "Percentage"; // Percentage / Fixed

    public string? Metadata { get; set; } // JSONB

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
