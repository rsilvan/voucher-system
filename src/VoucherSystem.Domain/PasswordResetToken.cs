namespace VoucherSystem.Domain;

public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
