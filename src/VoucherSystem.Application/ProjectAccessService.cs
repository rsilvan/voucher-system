using VoucherSystem.Application;

namespace VoucherSystem.Application;

public class ProjectAccessService : IProjectAccessService
{
    private readonly IProjectAccessRepository _repo;

    public ProjectAccessService(IProjectAccessRepository repo) => _repo = repo;

    public async Task<List<Guid>> GetMemberProjectIdsAsync(Guid memberId)
        => await _repo.GetMemberProjectIdsAsync(memberId);

    public async Task SetMemberProjectAccessAsync(Guid memberId, Guid roleId, List<Guid> projectIds)
        => await _repo.SetMemberProjectAccessAsync(memberId, roleId, projectIds);
}
