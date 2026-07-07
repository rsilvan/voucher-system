using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IAuthRepository
{
    Task<User?> GetUserByEmailAsync(string normalizedEmail);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<OrganizationMember?> GetActiveMemberAsync(Guid userId);
    Task<Organization?> GetOrganizationAsync(Guid organizationId);
    Task<Role?> GetRoleAsync(Guid roleId);
    Task<List<Permission>> GetRolePermissionsAsync(Guid roleId);
    Task SaveRefreshTokenAsync(RefreshToken token);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash);
    Task RevokeRefreshTokenAsync(Guid tokenId);
    Task SavePasswordResetTokenAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(string tokenHash);
    Task MarkResetTokenUsedAsync(Guid tokenId);
    Task UpdateUserPasswordAsync(Guid userId, string passwordHash);
}
