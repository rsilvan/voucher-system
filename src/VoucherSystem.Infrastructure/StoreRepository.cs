using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class StoreRepository : IStoreRepository
{
    private readonly VoucherSystemDbContext _db;

    public StoreRepository(VoucherSystemDbContext db)
    {
        _db = db;
    }

    public async Task<Store?> GetByIdAsync(Guid storeId, Guid projectId)
        => await _db.Stores
            .FirstOrDefaultAsync(s => s.Id == storeId && s.ProjectId == projectId && !s.IsDeleted);

    public async Task<List<Store>> GetByProjectAsync(Guid projectId)
        => await _db.Stores
            .Where(s => s.ProjectId == projectId && !s.IsDeleted)
            .OrderBy(s => s.Code)
            .ToListAsync();

    public async Task<bool> CodeExistsAsync(Guid projectId, string code)
        => await _db.Stores.AnyAsync(s => s.ProjectId == projectId && s.Code == code && !s.IsDeleted);

    public async Task AddAsync(Store store)
    {
        _db.Stores.Add(store);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Store store)
    {
        _db.Stores.Update(store);
        await _db.SaveChangesAsync();
    }
}
