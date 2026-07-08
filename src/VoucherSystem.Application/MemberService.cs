using System.Security.Cryptography;
using System.Text;
using VoucherSystem.Contracts;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _repo;
    private readonly IEmailService _emailService;

    public MemberService(IMemberRepository repo, IEmailService emailService)
    {
        _repo = repo;
        _emailService = emailService;
    }

    public async Task<InviteMemberResponse> InviteMemberAsync(Guid organizationId, Guid invitedByUserId, InviteMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check if already invited
        var alreadyInvited = await _repo.IsEmailInvitedAsync(organizationId, normalizedEmail);
        if (alreadyInvited)
            throw new ArgumentException("This email already has a pending invitation.");

        var now = DateTimeOffset.UtcNow;

        var tokenBytes = RandomNumberGenerator.GetBytes(48);
        var tokenString = Convert.ToBase64String(tokenBytes)
            .Replace("/", "_").Replace("+", "-").Replace("=", "");
        var tokenHash = HashToken(tokenString);

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            Name = request.Name,
            RoleId = request.RoleId,
            TokenHash = tokenHash,
            Status = "Pending",
            ExpiresAt = now.AddDays(7),
            InvitedByUserId = invitedByUserId,
            CreatedAt = now
        };

        await _repo.SaveInvitationAsync(invitation);

        if (request.ProjectIds?.Count > 0)
        {
            await _repo.SaveInvitationProjectAccessAsync(invitation.Id, request.ProjectIds);
        }

        await _emailService.SendInvitationEmailAsync(invitation.Email, invitation.Name, tokenString, 
            (await _repo.GetOrganizationNameAsync(organizationId)) ?? "Organization");

        return new InviteMemberResponse
        {
            InvitationId = invitation.Id,
            Status = "Pending",
            ExpiresAt = invitation.ExpiresAt
        };
    }

    public async Task<AcceptInvitationResponse> AcceptInvitationAsync(string token, AcceptInvitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");
        if (request.Password != request.ConfirmPassword)
            throw new ArgumentException("Passwords do not match.");
        if (request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        var tokenHash = HashToken(token);
        var invitation = await _repo.GetInvitationByTokenHashAsync(tokenHash)
            ?? throw new ArgumentException("Invalid or expired invitation token.");

        if (invitation.Status != "Pending")
            throw new ArgumentException($"Invitation is already {invitation.Status.ToLower()}.");
        if (invitation.ExpiresAt < DateTimeOffset.UtcNow)
            throw new ArgumentException("Invitation has expired.");

        var now = DateTimeOffset.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(request.Name) ? invitation.Name : request.Name,
            Email = invitation.Email,
            NormalizedEmail = invitation.NormalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            EmailVerified = false,
            Status = "Active",
            CreatedAt = now
        };

        var member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = invitation.OrganizationId,
            UserId = user.Id,
            RoleId = invitation.RoleId,
            Status = "Active",
            CreatedAt = now
        };

        await _repo.SaveNewUserMemberAsync(user, member);
        await _repo.UpdateInvitationStatusAsync(invitation.Id, "Accepted", now);

        return new AcceptInvitationResponse
        {
            Status = "Accepted",
            UserId = user.Id,
            OrganizationId = invitation.OrganizationId
        };
    }

    public async Task<List<MemberResponse>> GetMembersAsync(Guid organizationId)
    {
        var members = await _repo.GetOrganizationMembersAsync(organizationId);
        var result = new List<MemberResponse>();

        foreach (var m in members)
        {
            var role = await _repo.GetRoleAsync(m.RoleId);
            result.Add(new MemberResponse
            {
                Id = m.Id,
                UserId = m.UserId,
                Name = "", // Would need user info join
                Email = "",
                Role = role?.Name ?? "Unknown",
                Status = m.Status,
                CreatedAt = m.CreatedAt
            });
        }

        return result;
    }

    public async Task<MemberResponse?> GetMemberAsync(Guid organizationId, Guid memberId)
    {
        var member = await _repo.GetMemberByIdAsync(organizationId, memberId);
        if (member == null) return null;

        var role = await _repo.GetRoleAsync(member.RoleId);
        return new MemberResponse
        {
            Id = member.Id,
            UserId = member.UserId,
            Name = "",
            Email = "",
            Role = role?.Name ?? "Unknown",
            Status = member.Status,
            CreatedAt = member.CreatedAt
        };
    }

    public async Task DisableMemberAsync(Guid organizationId, Guid memberId)
    {
        var member = await _repo.GetMemberByIdAsync(organizationId, memberId);
        if (member == null) throw new ArgumentException("Member not found.");

        // Prevent disabling last owner
        var role = await _repo.GetRoleAsync(member.RoleId);
        if (role?.Key == "OrganizationOwner")
        {
            var activeOwners = (await _repo.GetOrganizationMembersAsync(organizationId))
                .Count(m => m.Status == "Active");
            if (activeOwners <= 1)
                throw new InvalidOperationException("Cannot disable the last active owner.");
        }

        await _repo.UpdateMemberStatusAsync(memberId, "Disabled");
    }

    public async Task EnableMemberAsync(Guid organizationId, Guid memberId)
    {
        var member = await _repo.GetMemberByIdAsync(organizationId, memberId);
        if (member == null) throw new ArgumentException("Member not found.");
        await _repo.UpdateMemberStatusAsync(memberId, "Active");
    }

    public async Task<(string Token, DateTimeOffset ExpiresAt)> ResendInvitationAsync(Guid organizationId, Guid invitationId, Guid actorUserId)
    {
        var invitation = await _repo.GetInvitationByIdAsync(invitationId)
            ?? throw new ArgumentException("Invitation not found.");

        if (invitation.OrganizationId != organizationId)
            throw new ArgumentException("Invitation does not belong to this organization.");

        if (invitation.Status != "Pending")
            throw new ArgumentException($"Invitation is already {invitation.Status.ToLower()}.");

        var now = DateTimeOffset.UtcNow;
        var tokenBytes = RandomNumberGenerator.GetBytes(48);
        var tokenString = Convert.ToBase64String(tokenBytes)
            .Replace("/", "_").Replace("+", "-").Replace("=", "");
        var tokenHash = HashToken(tokenString);
        var expiresAt = now.AddDays(7);

        await _repo.UpdateInvitationTokenAsync(invitationId, tokenHash, expiresAt);

        await _emailService.SendInvitationEmailAsync(invitation.Email, invitation.Name, tokenString,
            (await _repo.GetOrganizationNameAsync(invitation.OrganizationId)) ?? "Organization");

        return (tokenString, expiresAt);
    }

    public async Task RevokeInvitationAsync(Guid organizationId, Guid invitationId, Guid actorUserId)
    {
        var invitation = await _repo.GetInvitationByIdAsync(invitationId)
            ?? throw new ArgumentException("Invitation not found.");

        if (invitation.OrganizationId != organizationId)
            throw new ArgumentException("Invitation does not belong to this organization.");

        if (invitation.Status != "Pending")
            throw new ArgumentException($"Invitation is already {invitation.Status.ToLower()}.");

        await _repo.UpdateInvitationStatusAsync(invitationId, "Revoked", DateTimeOffset.UtcNow);
    }

    private static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
