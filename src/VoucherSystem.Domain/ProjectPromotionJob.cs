namespace VoucherSystem.Domain;

public class ProjectPromotionJob
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SourceProjectId { get; set; }
    public Guid TargetProjectId { get; set; }
    public string Status { get; set; } = nameof(PromotionJobStatus.Pending);
    public string? IdempotencyKey { get; set; }
    public string? PlanJson { get; set; } // JSON serialized plan
    public string? ResultJson { get; set; } // JSON serialized result/mappings
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
