using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IAreaRepository
{
    Task<Area?> GetByIdAsync(Guid areaId, Guid projectId);
    Task<List<Area>> GetByProjectAsync(Guid projectId);
    Task<List<Area>> GetChildrenAsync(Guid parentId);
    Task<List<Area>> GetAncestorsAsync(Guid areaId, Guid projectId);
    Task<List<Area>> GetDescendantsAsync(Guid areaId, Guid projectId);
    Task<bool> NameExistsAsync(Guid projectId, string name, Guid? excludeId = null);
    Task AddAsync(Area area);
    Task UpdateAsync(Area area);
    Task AddStoreToAreaAsync(Guid areaId, Guid storeId);
    Task RemoveStoreFromAreaAsync(Guid areaId, Guid storeId);
    Task<List<Store>> GetStoresForAreaAsync(Guid areaId);
    Task<bool> StoreInAreaAsync(Guid areaId, Guid storeId);
}
