namespace VoucherSystem.Contracts;

public class InviteMemberRequest
{
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public List<Guid>? ProjectIds { get; set; }
    public string? Message { get; set; }
}

public class InviteMemberResponse
{
    public Guid InvitationId { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
}

public class AcceptInvitationRequest
{
    public string Name { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
}

public class AcceptInvitationResponse
{
    public string Status { get; set; } = default!;
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
}

public class MemberResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}

public class UpdateMemberRequest
{
    public Guid? RoleId { get; set; }
    public List<Guid>? ProjectIds { get; set; }
}

public class InvitationResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
