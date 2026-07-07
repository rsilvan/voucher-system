using VoucherSystem.Contracts;

namespace VoucherSystem.Application;

public interface IMemberService
{
    Task<InviteMemberResponse> InviteMemberAsync(Guid organizationId, Guid invitedByUserId, InviteMemberRequest request);
    Task<AcceptInvitationResponse> AcceptInvitationAsync(string token, AcceptInvitationRequest request);
    Task<(string Token, DateTimeOffset ExpiresAt)> ResendInvitationAsync(Guid organizationId, Guid invitationId, Guid actorUserId);
    Task RevokeInvitationAsync(Guid organizationId, Guid invitationId, Guid actorUserId);
    Task<List<MemberResponse>> GetMembersAsync(Guid organizationId);
    Task<MemberResponse?> GetMemberAsync(Guid organizationId, Guid memberId);
    Task DisableMemberAsync(Guid organizationId, Guid memberId);
    Task EnableMemberAsync(Guid organizationId, Guid memberId);
}
