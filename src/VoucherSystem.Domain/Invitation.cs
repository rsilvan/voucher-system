namespace VoucherSystem.Domain;

public class Invitation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = default!;
    public string NormalizedEmail { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string TokenHash { get; set; } = default!;
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Expired, Revoked
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid InvitedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public class InvitationProjectAccess
{
    public Guid InvitationId { get; set; }
    public Guid ProjectId { get; set; }
}
