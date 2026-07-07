using VoucherSystem.Contracts.Stores;

namespace VoucherSystem.Application;

public interface IStoreService
{
    Task<StoreResponse?> GetByIdAsync(Guid storeId, Guid projectId);
    Task<StoreListResponse> GetByProjectAsync(Guid projectId);
    Task<StoreResponse> CreateAsync(Guid projectId, CreateStoreRequest request);
    Task<StoreResponse?> UpdateAsync(Guid storeId, Guid projectId, UpdateStoreRequest request);
    Task<bool> DeleteAsync(Guid storeId, Guid projectId);
}
