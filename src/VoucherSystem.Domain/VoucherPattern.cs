namespace VoucherSystem.Domain;

public class VoucherPattern
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string Charset { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public int Length { get; set; } = 10;
    public string? Pattern { get; set; } // e.g. "PROMO-####-XXXX"

    public string? Metadata { get; set; } // JSONB

    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
