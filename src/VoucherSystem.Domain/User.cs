namespace VoucherSystem.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string NormalizedEmail { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public bool EmailVerified { get; set; }
    public string Status { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LockoutEndAt { get; set; }
}
