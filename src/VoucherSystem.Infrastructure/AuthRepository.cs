using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class AuthRepository : IAuthRepository
{
    private readonly VoucherSystemDbContext _db;

    public AuthRepository(VoucherSystemDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetUserByEmailAsync(string normalizedEmail)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _db.Users.FindAsync(userId);
    }

    public async Task<OrganizationMember?> GetActiveMemberAsync(Guid userId)
    {
        return await _db.OrganizationMembers
            .Where(m => m.UserId == userId && m.Status == "Active")
            .FirstOrDefaultAsync();
    }

    public async Task<Organization?> GetOrganizationAsync(Guid organizationId)
    {
        return await _db.Organizations.FindAsync(organizationId);
    }

    public async Task<Role?> GetRoleAsync(Guid roleId)
    {
        return await _db.Roles.FindAsync(roleId);
    }

    public async Task<List<Permission>> GetRolePermissionsAsync(Guid roleId)
    {
        return await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p)
            .ToListAsync();
    }

    public async Task SaveRefreshTokenAsync(RefreshToken token)
    {
        _db.Set<RefreshToken>().Add(token);
        await _db.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
    {
        return await _db.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
    }

    public async Task RevokeRefreshTokenAsync(Guid tokenId)
    {
        var token = await _db.Set<RefreshToken>().FindAsync(tokenId);
        if (token != null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task SavePasswordResetTokenAsync(PasswordResetToken token)
    {
        _db.Set<PasswordResetToken>().Add(token);
        await _db.SaveChangesAsync();
    }

    public async Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(string tokenHash)
    {
        return await _db.Set<PasswordResetToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task MarkResetTokenUsedAsync(Guid tokenId)
    {
        var token = await _db.Set<PasswordResetToken>().FindAsync(tokenId);
        if (token != null)
        {
            token.Used = true;
            token.UsedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateUserPasswordAsync(Guid userId, string passwordHash)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.PasswordHash = passwordHash;
            await _db.SaveChangesAsync();
        }
    }
}
