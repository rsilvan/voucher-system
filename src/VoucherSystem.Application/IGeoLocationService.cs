using VoucherSystem.Contracts.GeoLocations;

namespace VoucherSystem.Application;

public interface IGeoLocationService
{
    Task<GeoLocationListResponse> GetByProjectAsync(Guid projectId, Guid organizationId);
    Task<GeoLocationResponse?> GetByIdAsync(Guid id, Guid projectId, Guid organizationId);
    Task<GeoLocationResponse> CreateAsync(Guid projectId, Guid organizationId, CreateGeoLocationRequest request);
    Task<GeoLocationResponse?> UpdateAsync(Guid id, Guid projectId, Guid organizationId, UpdateGeoLocationRequest request);
    Task<bool> DeleteAsync(Guid id, Guid projectId, Guid organizationId);
    GeoLocationValidationResponse Validate(GeoLocationValidationRequest request);
}
