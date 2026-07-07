using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class GeoLocationRepository : IGeoLocationRepository
{
    private readonly VoucherSystemDbContext _db;

    public GeoLocationRepository(VoucherSystemDbContext db)
    {
        _db = db;
    }

    public async Task<List<GeoLocation>> GetByProjectAsync(Guid projectId)
        => await _db.GeoLocations
            .Where(g => g.ProjectId == projectId)
            .OrderBy(g => g.Name)
            .ToListAsync();

    public async Task<GeoLocation?> GetByIdAsync(Guid id, Guid projectId)
        => await _db.GeoLocations
            .FirstOrDefaultAsync(g => g.Id == id && g.ProjectId == projectId);

    public async Task AddAsync(GeoLocation geoLocation)
    {
        _db.GeoLocations.Add(geoLocation);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(GeoLocation geoLocation)
    {
        _db.GeoLocations.Update(geoLocation);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(GeoLocation geoLocation)
    {
        _db.GeoLocations.Update(geoLocation); // soft delete
        await _db.SaveChangesAsync();
    }
}
