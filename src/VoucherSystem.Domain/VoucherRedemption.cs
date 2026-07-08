namespace VoucherSystem.Domain;

public class VoucherRedemption
{
    public Guid Id { get; set; }
    public Guid VoucherId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }

    public string? CustomerId { get; set; }
    public string? OrderId { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string Status { get; set; } = "Completed"; // Completed / Reversed
    public string? IdempotencyKey { get; set; }

    public string? Metadata { get; set; } // JSONB

    public Guid RedeemedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public Guid? ReversedBy { get; set; }
}
