using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class MemberRepository : IMemberRepository
{
    private readonly VoucherSystemDbContext _db;

    public MemberRepository(VoucherSystemDbContext db)
    {
        _db = db;
    }

    public async Task<OrganizationMember?> GetMemberByIdAsync(Guid organizationId, Guid memberId)
    {
        return await _db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == organizationId);
    }

    public async Task<List<OrganizationMember>> GetOrganizationMembersAsync(Guid organizationId)
    {
        return await _db.OrganizationMembers
            .Where(m => m.OrganizationId == organizationId)
            .ToListAsync();
    }

    public async Task UpdateMemberStatusAsync(Guid memberId, string status)
    {
        var member = await _db.OrganizationMembers.FindAsync(memberId);
        if (member != null)
        {
            member.Status = status;
            member.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateMemberRoleAsync(Guid memberId, Guid roleId)
    {
        var member = await _db.OrganizationMembers.FindAsync(memberId);
        if (member != null)
        {
            member.RoleId = roleId;
            member.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsEmailInvitedAsync(Guid organizationId, string normalizedEmail)
    {
        return await _db.Set<Invitation>()
            .AnyAsync(i => i.OrganizationId == organizationId
                && i.NormalizedEmail == normalizedEmail
                && i.Status == "Pending");
    }

    public async Task SaveInvitationAsync(Invitation invitation)
    {
        _db.Set<Invitation>().Add(invitation);
        await _db.SaveChangesAsync();
    }

    public async Task SaveInvitationProjectAccessAsync(Guid invitationId, List<Guid> projectIds)
    {
        foreach (var projectId in projectIds)
        {
            _db.Set<InvitationProjectAccess>().Add(new InvitationProjectAccess
            {
                InvitationId = invitationId,
                ProjectId = projectId
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task<Invitation?> GetInvitationByTokenHashAsync(string tokenHash)
    {
        return await _db.Set<Invitation>()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);
    }

    public async Task<Invitation?> GetInvitationByIdAsync(Guid invitationId)
    {
        return await _db.Set<Invitation>().FindAsync(invitationId);
    }

    public async Task UpdateInvitationStatusAsync(Guid invitationId, string status, DateTimeOffset? actionedAt)
    {
        var inv = await _db.Set<Invitation>().FindAsync(invitationId);
        if (inv != null)
        {
            inv.Status = status;
            if (status == "Accepted") inv.AcceptedAt = actionedAt;
            if (status == "Revoked") inv.RevokedAt = actionedAt;
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateInvitationTokenAsync(Guid invitationId, string tokenHash, DateTimeOffset expiresAt)
    {
        var inv = await _db.Set<Invitation>().FindAsync(invitationId);
        if (inv != null)
        {
            inv.TokenHash = tokenHash;
            inv.ExpiresAt = expiresAt;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<Role?> GetRoleAsync(Guid roleId)
    {
        return await _db.Roles.FindAsync(roleId);
    }

    public async Task<Plan?> GetPlanAsync(Guid planId)
    {
        return await _db.Plans.FindAsync(planId);
    }

    public async Task<int> CountActiveMembersAsync(Guid organizationId)
    {
        return await _db.OrganizationMembers
            .CountAsync(m => m.OrganizationId == organizationId && m.Status == "Active");
    }

    public async Task SaveNewUserMemberAsync(User user, OrganizationMember member)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.Users.Add(user);
            _db.OrganizationMembers.Add(member);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Guid>> GetInvitationProjectIdsAsync(Guid invitationId)
    {
        return await _db.Set<InvitationProjectAccess>()
            .Where(ipa => ipa.InvitationId == invitationId)
            .Select(ipa => ipa.ProjectId)
            .ToListAsync();
    }
}
