namespace VoucherSystem.Domain;

public class Role
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public string Scope { get; set; } = "Organization";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
