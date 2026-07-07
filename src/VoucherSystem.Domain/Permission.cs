namespace VoucherSystem.Domain;

public class Permission
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Resource { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
