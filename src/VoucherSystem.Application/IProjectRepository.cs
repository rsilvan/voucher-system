using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid projectId, Guid organizationId);
    Task<List<Project>> GetByOrganizationAsync(Guid organizationId);
    Task<List<Project>> GetByMemberAsync(Guid organizationId, Guid memberId);
    Task<bool> SlugExistsAsync(Guid organizationId, string slug);
    Task<Project?> GetPrimaryAsync(Guid organizationId);
    Task<int> GetActiveCountAsync(Guid organizationId);
    Task AddAsync(Project project);
    Task UpdateAsync(Project project);
    Task<bool> HasResourcesAsync(Guid projectId);
}
