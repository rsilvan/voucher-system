using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class VoucherRepository : IVoucherRepository
{
    private readonly VoucherSystemDbContext _db;

    public VoucherRepository(VoucherSystemDbContext db) => _db = db;

    public async Task<Voucher?> GetByIdAsync(Guid organizationId, Guid projectId, Guid id)
        => await _db.Vouchers.FirstOrDefaultAsync(v => v.Id == id && v.OrganizationId == organizationId && v.ProjectId == projectId);

    public async Task<Voucher?> GetByCodeAsync(Guid organizationId, Guid projectId, string code)
        => await _db.Vouchers.FirstOrDefaultAsync(v => v.OrganizationId == organizationId && v.ProjectId == projectId && v.Code == code);

    public async Task<List<Voucher>> GetListAsync(Guid organizationId, Guid projectId, int page, int pageSize, string? status)
    {
        var query = _db.Vouchers.Where(v => v.OrganizationId == organizationId && v.ProjectId == projectId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(v => v.Status == status);
        return await query.OrderByDescending(v => v.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetCountAsync(Guid organizationId, Guid projectId, string? status)
    {
        var query = _db.Vouchers.Where(v => v.OrganizationId == organizationId && v.ProjectId == projectId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(v => v.Status == status);
        return await query.CountAsync();
    }

    public async Task<bool> CodeExistsAsync(Guid organizationId, Guid projectId, string code)
        => await _db.Vouchers.AnyAsync(v => v.OrganizationId == organizationId && v.ProjectId == projectId && v.Code == code);

    public async Task AddAsync(Voucher voucher) { _db.Vouchers.Add(voucher); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(Voucher voucher) { _db.Vouchers.Update(voucher); await _db.SaveChangesAsync(); }
    public async Task AddBatchAsync(List<Voucher> vouchers) { _db.Vouchers.AddRange(vouchers); await _db.SaveChangesAsync(); }

    // Batch
    public async Task<VoucherBatch?> GetBatchByIdAsync(Guid organizationId, Guid projectId, Guid id)
        => await _db.Set<VoucherBatch>().FirstOrDefaultAsync(b => b.Id == id && b.OrganizationId == organizationId && b.ProjectId == projectId);

    public async Task<List<VoucherBatch>> GetBatchesAsync(Guid organizationId, Guid projectId)
        => await _db.Set<VoucherBatch>().Where(b => b.OrganizationId == organizationId && b.ProjectId == projectId).OrderByDescending(b => b.CreatedAt).ToListAsync();

    public async Task AddBatchAsync(VoucherBatch batch) { _db.Set<VoucherBatch>().Add(batch); await _db.SaveChangesAsync(); }
    public async Task UpdateBatchAsync(VoucherBatch batch) { _db.Set<VoucherBatch>().Update(batch); await _db.SaveChangesAsync(); }

    // Pattern
    public async Task<List<VoucherPattern>> GetPatternsAsync(Guid organizationId, Guid projectId)
        => await _db.Set<VoucherPattern>().Where(p => p.OrganizationId == organizationId && p.ProjectId == projectId).ToListAsync();

    public async Task AddPatternAsync(VoucherPattern pattern) { _db.Set<VoucherPattern>().Add(pattern); await _db.SaveChangesAsync(); }

    // Lifecycle
    public async Task AddLifecycleEventAsync(VoucherLifecycleEvent evt) { _db.Set<VoucherLifecycleEvent>().Add(evt); await _db.SaveChangesAsync(); }

    // Redemption
    public async Task<VoucherRedemption?> GetRedemptionByIdAsync(Guid organizationId, Guid projectId, Guid id)
        => await _db.Set<VoucherRedemption>().FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == organizationId && r.ProjectId == projectId);

    public async Task AddRedemptionAsync(VoucherRedemption redemption) { _db.Set<VoucherRedemption>().Add(redemption); await _db.SaveChangesAsync(); }
    public async Task UpdateRedemptionAsync(VoucherRedemption redemption) { _db.Set<VoucherRedemption>().Update(redemption); await _db.SaveChangesAsync(); }

    public async Task<VoucherRedemption?> GetByIdempotencyKeyAsync(string idempotencyKey)
        => await _db.Set<VoucherRedemption>().FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey);
}
