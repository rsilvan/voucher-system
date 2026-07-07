using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IOrganizationRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail);
    Task<Plan?> GetTrialPlanAsync();
    Task<Role?> GetSystemRoleByKeyAsync(string key);
    Task<Organization?> GetOrganizationByIdAsync(Guid organizationId);
    Task<Plan?> GetPlanByIdAsync(Guid planId);
    Task SaveOrganizationAsync(Organization organization, User user, Project project, OrganizationMember member);
}
