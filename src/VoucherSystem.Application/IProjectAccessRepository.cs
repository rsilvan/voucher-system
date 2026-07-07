using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IProjectAccessRepository
{
    Task<List<ProjectAccess>> GetMemberProjectAccessAsync(Guid memberId);
    Task SetMemberProjectAccessAsync(Guid memberId, Guid roleId, List<Guid> projectIds);
    Task<List<Guid>> GetMemberProjectIdsAsync(Guid memberId);
    Task<bool> HasProjectAccessAsync(Guid memberId, Guid projectId);
}
