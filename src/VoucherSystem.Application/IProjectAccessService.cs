using VoucherSystem.Contracts;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IProjectAccessService
{
    Task<List<Guid>> GetMemberProjectIdsAsync(Guid memberId);
    Task SetMemberProjectAccessAsync(Guid memberId, Guid roleId, List<Guid> projectIds);
}
