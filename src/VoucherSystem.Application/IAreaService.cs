using VoucherSystem.Contracts.Areas;

namespace VoucherSystem.Application;

public interface IAreaService
{
    Task<AreaResponse?> GetByIdAsync(Guid areaId, Guid projectId);
    Task<AreaTreeResponse> GetTreeAsync(Guid projectId);
    Task<AreaResponse> CreateAsync(Guid projectId, CreateAreaRequest request);
    Task<AreaResponse?> UpdateAsync(Guid areaId, Guid projectId, UpdateAreaRequest request);
    Task<bool> DeleteAsync(Guid areaId, Guid projectId);
    Task AssignStoresAsync(Guid areaId, Guid projectId, List<Guid> storeIds);
    Task UnassignStoreAsync(Guid areaId, Guid projectId, Guid storeId);
}
