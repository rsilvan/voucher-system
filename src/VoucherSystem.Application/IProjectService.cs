using VoucherSystem.Contracts.Projects;

namespace VoucherSystem.Application;

public interface IProjectService
{
    Task<ProjectResponse?> GetByIdAsync(Guid projectId, Guid organizationId);
    Task<ProjectListResponse> GetByOrganizationAsync(Guid organizationId, Guid? memberId);
    Task<ProjectResponse> CreateAsync(Guid organizationId, CreateProjectRequest request);
    Task<ProjectResponse?> UpdateAsync(Guid projectId, Guid organizationId, UpdateProjectRequest request);
    Task<bool> DisableAsync(Guid projectId, Guid organizationId);
    Task<bool> EnableAsync(Guid projectId, Guid organizationId);
    Task<bool> ArchiveAsync(Guid projectId, Guid organizationId);
    Task<bool> RestoreAsync(Guid projectId, Guid organizationId);
    Task<bool> MakePrimaryAsync(Guid projectId, Guid organizationId);
    Task<bool> DeleteAsync(Guid projectId, Guid organizationId);
}
