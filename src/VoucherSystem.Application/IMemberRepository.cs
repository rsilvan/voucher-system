using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IMemberRepository
{
    Task<OrganizationMember?> GetMemberByIdAsync(Guid organizationId, Guid memberId);
    Task<List<OrganizationMember>> GetOrganizationMembersAsync(Guid organizationId);
    Task UpdateMemberStatusAsync(Guid memberId, string status);
    Task UpdateMemberRoleAsync(Guid memberId, Guid roleId);
    Task<bool> IsEmailInvitedAsync(Guid organizationId, string normalizedEmail);
    Task SaveInvitationAsync(Invitation invitation);
    Task SaveInvitationProjectAccessAsync(Guid invitationId, List<Guid> projectIds);
    Task<Invitation?> GetInvitationByTokenHashAsync(string tokenHash);
    Task<Invitation?> GetInvitationByIdAsync(Guid invitationId);
    Task UpdateInvitationStatusAsync(Guid invitationId, string status, DateTimeOffset? actionedAt);
    Task UpdateInvitationTokenAsync(Guid invitationId, string tokenHash, DateTimeOffset expiresAt);
    Task<Role?> GetRoleAsync(Guid roleId);
    Task<Plan?> GetPlanAsync(Guid planId);
    Task<int> CountActiveMembersAsync(Guid organizationId);
    Task SaveNewUserMemberAsync(User user, OrganizationMember member);
    Task<List<Guid>> GetInvitationProjectIdsAsync(Guid invitationId);
    Task<string?> GetOrganizationNameAsync(Guid organizationId);
}
