using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class BrandRepository : IBrandRepository
{
    private readonly VoucherSystemDbContext _db;

    public BrandRepository(VoucherSystemDbContext db)
    {
        _db = db;
    }

    public async Task<BrandProfile?> GetByProjectAsync(Guid projectId)
        => await _db.BrandProfiles
            .FirstOrDefaultAsync(b => b.ProjectId == projectId);

    public async Task AddAsync(BrandProfile brand)
    {
        _db.BrandProfiles.Add(brand);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(BrandProfile brand)
    {
        _db.BrandProfiles.Update(brand);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(BrandProfile brand)
    {
        _db.BrandProfiles.Remove(brand);
        await _db.SaveChangesAsync();
    }
}
