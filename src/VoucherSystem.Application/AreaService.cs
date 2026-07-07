using VoucherSystem.Contracts.Areas;
using VoucherSystem.Contracts.Stores;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class AreaService : IAreaService
{
    private readonly IAreaRepository _repo;
    private readonly IStoreRepository _storeRepo;
    private readonly IAuditLogWriter _audit;

    private const int MaxDepth = 5;

    public AreaService(IAreaRepository repo, IStoreRepository storeRepo, IAuditLogWriter audit)
    {
        _repo = repo;
        _storeRepo = storeRepo;
        _audit = audit;
    }

    public async Task<AreaResponse?> GetByIdAsync(Guid areaId, Guid projectId)
    {
        var area = await _repo.GetByIdAsync(areaId, projectId);
        if (area is null) return null;

        return await MapToResponseAsync(area, projectId);
    }

    public async Task<AreaTreeResponse> GetTreeAsync(Guid projectId)
    {
        var allAreas = await _repo.GetByProjectAsync(projectId);
        var roots = allAreas.Where(a => a.ParentAreaId is null).OrderBy(a => a.Name).ToList();

        var rootResponses = new List<AreaResponse>();
        foreach (var root in roots)
        {
            rootResponses.Add(await BuildTreeResponseAsync(root, allAreas, projectId));
        }

        return new AreaTreeResponse
        {
            Roots = rootResponses,
            TotalCount = allAreas.Count,
        };
    }

    public async Task<AreaResponse> CreateAsync(Guid projectId, CreateAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Area name is required.");

        if (await _repo.NameExistsAsync(projectId, request.Name.Trim()))
            throw new ArgumentException("An area with this name already exists in this project.");

        int depth = 0;
        if (request.ParentAreaId.HasValue)
        {
            var parent = await _repo.GetByIdAsync(request.ParentAreaId.Value, projectId);
            if (parent is null)
                throw new ArgumentException("Parent area not found.");

            // Prevent cycles
            if (parent.ParentAreaId == request.ParentAreaId)
                throw new InvalidOperationException("Cannot set parent as itself.");

            // Check max depth
            var ancestors = await _repo.GetAncestorsAsync(parent.Id, projectId);
            depth = ancestors.Count + 1;

            if (depth > MaxDepth)
                throw new InvalidOperationException($"Maximum area depth of {MaxDepth} exceeded.");
        }

        var now = DateTimeOffset.UtcNow;
        var area = new Area
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ParentAreaId = request.ParentAreaId,
            Depth = depth,
            CreatedAt = now,
        };

        await _repo.AddAsync(area);
        _audit.Write(projectId, projectId, null, "area.created", "Area", area.Id.ToString());

        return await MapToResponseAsync(area, projectId);
    }

    public async Task<AreaResponse?> UpdateAsync(Guid areaId, Guid projectId, UpdateAreaRequest request)
    {
        var area = await _repo.GetByIdAsync(areaId, projectId);
        if (area is null) return null;

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Area name cannot be empty.");
            if (await _repo.NameExistsAsync(projectId, request.Name.Trim(), areaId))
                throw new ArgumentException("An area with this name already exists in this project.");
            area.Name = request.Name.Trim();
        }
        if (request.Description is not null)
            area.Description = request.Description.Trim();

        if (request.ParentAreaId.HasValue)
        {
            var newParentId = request.ParentAreaId.Value;

            // Can't set parent to itself
            if (newParentId == areaId)
                throw new InvalidOperationException("An area cannot be its own parent.");

            // Can't set parent to a descendant (would create cycle)
            var descendants = await _repo.GetDescendantsAsync(areaId, projectId);
            if (descendants.Any(d => d.Id == newParentId))
                throw new InvalidOperationException("Cannot set parent to a descendant area (would create a cycle).");

            Area? parent = null;
            if (newParentId != Guid.Empty)
            {
                parent = await _repo.GetByIdAsync(newParentId, projectId);
                if (parent is null)
                    throw new ArgumentException("Parent area not found.");
            }

            area.ParentAreaId = newParentId == Guid.Empty ? null : newParentId;

            // Recalculate depth
            if (newParentId != Guid.Empty && parent is not null)
            {
                var ancestors = await _repo.GetAncestorsAsync(parent.Id, projectId);
                area.Depth = ancestors.Count + 1;
                if (area.Depth > MaxDepth)
                    throw new InvalidOperationException($"Maximum area depth of {MaxDepth} exceeded.");
            }
            else
            {
                area.Depth = 0;
            }

            // Update depths of descendants
            await UpdateDescendantDepthsAsync(area, descendants, projectId);
        }
        else if (request.ParentAreaId is null)
        {
            // Explicit null means no change (null in request == don't update)
            // The HasValue check above only runs when ParentAreaId is set in the request
        }

        area.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(area);
        _audit.Write(projectId, projectId, null, "area.updated", "Area", area.Id.ToString());

        return await MapToResponseAsync(area, projectId);
    }

    public async Task<bool> DeleteAsync(Guid areaId, Guid projectId)
    {
        var area = await _repo.GetByIdAsync(areaId, projectId);
        if (area is null) return false;

        // Soft delete - mark area and all descendants as deleted
        var descendants = await _repo.GetDescendantsAsync(areaId, projectId);
        area.IsDeleted = true;
        area.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(area);

        foreach (var desc in descendants)
        {
            desc.IsDeleted = true;
            desc.UpdatedAt = DateTimeOffset.UtcNow;
            await _repo.UpdateAsync(desc);
        }

        _audit.Write(projectId, projectId, null, "area.deleted", "Area", area.Id.ToString());
        return true;
    }

    public async Task AssignStoresAsync(Guid areaId, Guid projectId, List<Guid> storeIds)
    {
        var area = await _repo.GetByIdAsync(areaId, projectId);
        if (area is null)
            throw new ArgumentException("Area not found.");

        foreach (var storeId in storeIds)
        {
            var store = await _storeRepo.GetByIdAsync(storeId, projectId);
            if (store is null)
                throw new ArgumentException($"Store with ID '{storeId}' not found in this project.");

            var alreadyAssigned = await _repo.StoreInAreaAsync(areaId, storeId);
            if (!alreadyAssigned)
            {
                await _repo.AddStoreToAreaAsync(areaId, storeId);
            }
        }

        _audit.Write(projectId, projectId, null, "area.stores_assigned", "Area", areaId.ToString(),
            new { storeIds });
    }

    public async Task UnassignStoreAsync(Guid areaId, Guid projectId, Guid storeId)
    {
        var area = await _repo.GetByIdAsync(areaId, projectId);
        if (area is null)
            throw new ArgumentException("Area not found.");

        var store = await _storeRepo.GetByIdAsync(storeId, projectId);
        if (store is null)
            throw new ArgumentException("Store not found.");

        await _repo.RemoveStoreFromAreaAsync(areaId, storeId);
        _audit.Write(projectId, projectId, null, "area.store_unassigned", "Area", areaId.ToString(),
            new { storeId });
    }

    private async Task UpdateDescendantDepthsAsync(Area area, List<Area> descendants, Guid projectId)
    {
        // Recalculate depth for each descendant
        foreach (var desc in descendants.OrderBy(d => d.Depth))
        {
            var ancestors = await _repo.GetAncestorsAsync(desc.Id, projectId);
            desc.Depth = ancestors.Count;
            if (desc.Depth > MaxDepth)
                throw new InvalidOperationException($"Maximum area depth of {MaxDepth} exceeded.");
            await _repo.UpdateAsync(desc);
        }
    }

    private async Task<AreaResponse> MapToResponseAsync(Area area, Guid projectId)
    {
        var allAreas = await _repo.GetByProjectAsync(projectId);
        return await BuildTreeResponseAsync(area, allAreas, projectId);
    }

    private async Task<AreaResponse> BuildTreeResponseAsync(Area area, List<Area> allAreas, Guid projectId)
    {
        var children = allAreas.Where(a => a.ParentAreaId == area.Id).OrderBy(a => a.Name).ToList();
        var stores = await _repo.GetStoresForAreaAsync(area.Id);

        return new AreaResponse
        {
            Id = area.Id,
            ProjectId = area.ProjectId,
            Name = area.Name,
            Description = area.Description,
            ParentAreaId = area.ParentAreaId,
            Depth = area.Depth,
            CreatedAt = area.CreatedAt,
            UpdatedAt = area.UpdatedAt,
            Children = children.Select(c => BuildTreeResponseAsync(c, allAreas, projectId).GetAwaiter().GetResult()).ToList(),
            Stores = stores.Where(s => !s.IsDeleted).Select(s => new StoreSummaryResponse
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                StoreType = s.StoreType,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
            }).ToList(),
        };
    }
}
