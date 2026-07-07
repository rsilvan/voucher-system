using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IGeoLocationRepository
{
    Task<List<GeoLocation>> GetByProjectAsync(Guid projectId);
    Task<GeoLocation?> GetByIdAsync(Guid id, Guid projectId);
    Task AddAsync(GeoLocation geoLocation);
    Task UpdateAsync(GeoLocation geoLocation);
    Task DeleteAsync(GeoLocation geoLocation);
}
