namespace VoucherSystem.Domain;

public class ProjectAccess
{
    public Guid Id { get; set; }
    public Guid OrganizationMemberId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
