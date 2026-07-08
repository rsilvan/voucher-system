namespace VoucherSystem.Domain;

public class VoucherLifecycleEvent
{
    public Guid Id { get; set; }
    public Guid VoucherId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }

    public string EventType { get; set; } = default!; // generated / activated / disabled / expired / redeemed / imported
    public string? Description { get; set; }
    public Guid? ActorId { get; set; }

    public string? Metadata { get; set; } // JSONB

    public DateTimeOffset CreatedAt { get; set; }
}
