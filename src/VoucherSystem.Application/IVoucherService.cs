using VoucherSystem.Contracts.Vouchers;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public interface IVoucherRepository
{
    Task<Voucher?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id);
    Task<Voucher?> GetByCodeAsync(Guid organizationId, Guid projectId, string code);
    Task<List<Voucher>> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status);
    Task<int> GetCountAsync(Guid organizationId, Guid projectId, string? status);
    Task<bool> CodeExistsAsync(Guid organizationId, Guid projectId, string code);
    Task AddAsync(Voucher voucher);
    Task UpdateAsync(Voucher voucher);
    Task AddBatchAsync(List<Voucher> vouchers);

    // Batch
    Task<VoucherBatch?> GetBatchByIdAsync(Guid organizationId, Guid projectId, Guid id);
    Task<List<VoucherBatch>> GetBatchesAsync(Guid organizationId, Guid projectId);
    Task AddBatchAsync(VoucherBatch batch);
    Task UpdateBatchAsync(VoucherBatch batch);

    // Pattern
    Task<List<VoucherPattern>> GetPatternsAsync(Guid organizationId, Guid projectId);
    Task AddPatternAsync(VoucherPattern pattern);

    // Lifecycle
    Task AddLifecycleEventAsync(VoucherLifecycleEvent evt);

    // Redemption
    Task<VoucherRedemption?> GetRedemptionByIdAsync(Guid organizationId, Guid projectId, Guid id);
    Task AddRedemptionAsync(VoucherRedemption redemption);
    Task UpdateRedemptionAsync(VoucherRedemption redemption);
    Task<VoucherRedemption?> GetByIdempotencyKeyAsync(string idempotencyKey);
}

public interface IVoucherService
{
    Task<VoucherResponse> CreateAsync(Guid organizationId, Guid projectId, CreateVoucherRequest request, Guid userId);
    Task<VoucherResponse?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id);
    Task<VoucherResponse?> GetByCodeAsync(Guid organizationId, Guid projectId, string code);
    Task<VoucherListResponse> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status);
    Task<VoucherResponse?> UpdateAsync(Guid organizationId, Guid projectId, Guid id, UpdateVoucherRequest request, Guid userId);
    Task<bool> DeleteAsync(Guid organizationId, Guid projectId, Guid id);
    Task<VoucherActionResponse> ExecuteActionAsync(Guid organizationId, Guid projectId, Guid id, string action, VoucherActionRequest request, Guid userId);

    // Batch
    Task<VoucherBatchResponse> CreateBatchAsync(Guid organizationId, Guid projectId, CreateVoucherBatchRequest request, Guid userId);
    Task<List<VoucherBatchResponse>> GetBatchesAsync(Guid organizationId, Guid projectId);

    // Patterns
    Task<VoucherPatternResponse> CreatePatternAsync(Guid organizationId, Guid projectId, CreateVoucherPatternRequest request, Guid userId);
    Task<List<VoucherPatternResponse>> GetPatternsAsync(Guid organizationId, Guid projectId);

    // Redemption
    Task<VoucherRedemptionResponse> RedeemAsync(Guid organizationId, Guid projectId, RedeemVoucherRequest request, Guid userId);
}
