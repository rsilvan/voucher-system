using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IBrandRepository
{
    Task<BrandProfile?> GetByProjectAsync(Guid projectId);
    Task AddAsync(BrandProfile brand);
    Task UpdateAsync(BrandProfile brand);
    Task DeleteAsync(BrandProfile brand);
}
