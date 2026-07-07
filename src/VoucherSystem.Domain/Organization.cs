namespace VoucherSystem.Domain;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? DocumentNumber { get; set; }
    public string Slug { get; set; } = default!;
    public string Country { get; set; } = "BR";
    public string Status { get; set; } = "Active";
    public Guid PlanId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
