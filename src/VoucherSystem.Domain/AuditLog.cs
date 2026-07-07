namespace VoucherSystem.Domain;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = default!;
    public string ResourceType { get; set; } = default!;
    public string? ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}
