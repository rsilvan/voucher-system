using VoucherSystem.Contracts;

namespace VoucherSystem.Application;

public interface IOrganizationService
{
    Task<CreateOrganizationResponse> CreateOrganizationAsync(CreateOrganizationRequest request);
}
