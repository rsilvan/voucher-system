using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(Guid storeId, Guid projectId);
    Task<List<Store>> GetByProjectAsync(Guid projectId);
    Task<bool> CodeExistsAsync(Guid projectId, string code);
    Task AddAsync(Store store);
    Task UpdateAsync(Store store);
}
