using VoucherSystem.Contracts.Brands;

namespace VoucherSystem.Application;

public interface IBrandService
{
    Task<BrandResponse?> GetByProjectAsync(Guid projectId, Guid organizationId);
    Task<BrandResponse> CreateAsync(Guid projectId, Guid organizationId, CreateBrandRequest request);
    Task<BrandResponse?> UpdateAsync(Guid projectId, Guid organizationId, UpdateBrandRequest request);
    Task<bool> DeleteAsync(Guid projectId, Guid organizationId);
}
