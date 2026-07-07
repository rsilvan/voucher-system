using Microsoft.EntityFrameworkCore;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class AreaRepository : IAreaRepository
{
    private readonly VoucherSystemDbContext _db;

    public AreaRepository(VoucherSystemDbContext db)
    {
        _db = db;
    }

    public async Task<Area?> GetByIdAsync(Guid areaId, Guid projectId)
        => await _db.Areas
            .FirstOrDefaultAsync(a => a.Id == areaId && a.ProjectId == projectId && !a.IsDeleted);

    public async Task<List<Area>> GetByProjectAsync(Guid projectId)
        => await _db.Areas
            .Where(a => a.ProjectId == projectId && !a.IsDeleted)
            .OrderBy(a => a.Depth)
            .ThenBy(a => a.Name)
            .ToListAsync();

    public async Task<List<Area>> GetChildrenAsync(Guid parentId)
        => await _db.Areas
            .Where(a => a.ParentAreaId == parentId && !a.IsDeleted)
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<List<Area>> GetAncestorsAsync(Guid areaId, Guid projectId)
    {
        var ancestors = new List<Area>();
        var current = await _db.Areas
            .FirstOrDefaultAsync(a => a.Id == areaId && a.ProjectId == projectId);

        while (current?.ParentAreaId is not null)
        {
            var parent = await _db.Areas
                .FirstOrDefaultAsync(a => a.Id == current.ParentAreaId.Value && a.ProjectId == projectId);
            if (parent is null) break;
            ancestors.Add(parent);
            current = parent;
        }

        return ancestors;
    }

    public async Task<List<Area>> GetDescendantsAsync(Guid areaId, Guid projectId)
    {
        var descendants = new List<Area>();
        var toProcess = new Queue<Guid>();
        toProcess.Enqueue(areaId);

        while (toProcess.Count > 0)
        {
            var currentId = toProcess.Dequeue();
            var children = await _db.Areas
                .Where(a => a.ParentAreaId == currentId && a.ProjectId == projectId && !a.IsDeleted)
                .ToListAsync();

            foreach (var child in children)
            {
                descendants.Add(child);
                toProcess.Enqueue(child.Id);
            }
        }

        return descendants;
    }

    public async Task<bool> NameExistsAsync(Guid projectId, string name, Guid? excludeId = null)
    {
        var query = _db.Areas.Where(a => a.ProjectId == projectId && a.Name == name && !a.IsDeleted);
        if (excludeId.HasValue)
            query = query.Where(a => a.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task AddAsync(Area area)
    {
        _db.Areas.Add(area);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Area area)
    {
        _db.Areas.Update(area);
        await _db.SaveChangesAsync();
    }

    public async Task AddStoreToAreaAsync(Guid areaId, Guid storeId)
    {
        var areaStore = new AreaStore { AreaId = areaId, StoreId = storeId };
        _db.AreaStores.Add(areaStore);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveStoreFromAreaAsync(Guid areaId, Guid storeId)
    {
        var asm = await _db.AreaStores
            .FirstOrDefaultAsync(x => x.AreaId == areaId && x.StoreId == storeId);
        if (asm is not null)
        {
            _db.AreaStores.Remove(asm);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<Store>> GetStoresForAreaAsync(Guid areaId)
    {
        var storeIds = await _db.AreaStores
            .Where(x => x.AreaId == areaId)
            .Select(x => x.StoreId)
            .ToListAsync();

        return await _db.Stores
            .Where(s => storeIds.Contains(s.Id))
            .ToListAsync();
    }

    public async Task<bool> StoreInAreaAsync(Guid areaId, Guid storeId)
        => await _db.AreaStores.AnyAsync(x => x.AreaId == areaId && x.StoreId == storeId);
}
