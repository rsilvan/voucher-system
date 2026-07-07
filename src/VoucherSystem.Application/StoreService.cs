using VoucherSystem.Contracts.Stores;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class StoreService : IStoreService
{
    private readonly IStoreRepository _repo;
    private readonly IAuditLogWriter _audit;

    public StoreService(IStoreRepository repo, IAuditLogWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<StoreResponse?> GetByIdAsync(Guid storeId, Guid projectId)
    {
        var store = await _repo.GetByIdAsync(storeId, projectId);
        return store is null ? null : MapToResponse(store);
    }

    public async Task<StoreListResponse> GetByProjectAsync(Guid projectId)
    {
        var stores = await _repo.GetByProjectAsync(projectId);
        return new StoreListResponse
        {
            Items = stores.Select(s => new StoreSummaryResponse
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                StoreType = s.StoreType,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
            }).ToList(),
            TotalCount = stores.Count,
        };
    }

    public async Task<StoreResponse> CreateAsync(Guid projectId, CreateStoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Store code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Store name is required.");

        ValidateStoreType(request.StoreType);

        if (await _repo.CodeExistsAsync(projectId, request.Code.Trim()))
            throw new ArgumentException("A store with this code already exists in this project.");

        var now = DateTimeOffset.UtcNow;
        var store = new Store
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            StoreType = request.StoreType,
            AddressLine1 = request.AddressLine1?.Trim(),
            AddressLine2 = request.AddressLine2?.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            Country = request.Country?.Trim(),
            ContactEmail = request.ContactEmail?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            GeoLocationId = request.GeoLocationId,
            Status = nameof(StoreStatus.Active),
            CreatedAt = now,
        };

        await _repo.AddAsync(store);
        _audit.Write(projectId, projectId, null, "store.created", "Store", store.Id.ToString());

        return MapToResponse(store);
    }

    public async Task<StoreResponse?> UpdateAsync(Guid storeId, Guid projectId, UpdateStoreRequest request)
    {
        var store = await _repo.GetByIdAsync(storeId, projectId);
        if (store is null) return null;

        if (request.Code is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new ArgumentException("Store code cannot be empty.");
            var trimmed = request.Code.Trim().ToUpperInvariant();
            if (trimmed != store.Code && await _repo.CodeExistsAsync(projectId, trimmed))
                throw new ArgumentException("A store with this code already exists in this project.");
            store.Code = trimmed;
        }
        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Store name cannot be empty.");
            store.Name = request.Name.Trim();
        }
        if (request.Description is not null)
            store.Description = request.Description.Trim();
        if (request.StoreType is not null)
        {
            ValidateStoreType(request.StoreType);
            store.StoreType = request.StoreType;
        }
        if (request.AddressLine1 is not null)
            store.AddressLine1 = request.AddressLine1.Trim();
        if (request.AddressLine2 is not null)
            store.AddressLine2 = request.AddressLine2.Trim();
        if (request.City is not null)
            store.City = request.City.Trim();
        if (request.State is not null)
            store.State = request.State.Trim();
        if (request.PostalCode is not null)
            store.PostalCode = request.PostalCode.Trim();
        if (request.Country is not null)
            store.Country = request.Country.Trim();
        if (request.ContactEmail is not null)
            store.ContactEmail = request.ContactEmail.Trim();
        if (request.ContactPhone is not null)
            store.ContactPhone = request.ContactPhone.Trim();
        if (request.GeoLocationId is not null)
            store.GeoLocationId = request.GeoLocationId;
        if (request.Status is not null)
        {
            ValidateStoreStatus(request.Status);
            store.Status = request.Status;
        }

        store.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(store);
        _audit.Write(projectId, projectId, null, "store.updated", "Store", store.Id.ToString());

        return MapToResponse(store);
    }

    public async Task<bool> DeleteAsync(Guid storeId, Guid projectId)
    {
        var store = await _repo.GetByIdAsync(storeId, projectId);
        if (store is null) return false;

        store.IsDeleted = true;
        store.Status = nameof(StoreStatus.Archived);
        store.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(store);

        _audit.Write(projectId, projectId, null, "store.deleted", "Store", store.Id.ToString());
        return true;
    }

    private static StoreResponse MapToResponse(Store s) => new()
    {
        Id = s.Id,
        ProjectId = s.ProjectId,
        Code = s.Code,
        Name = s.Name,
        Description = s.Description,
        StoreType = s.StoreType,
        AddressLine1 = s.AddressLine1,
        AddressLine2 = s.AddressLine2,
        City = s.City,
        State = s.State,
        PostalCode = s.PostalCode,
        Country = s.Country,
        ContactEmail = s.ContactEmail,
        ContactPhone = s.ContactPhone,
        GeoLocationId = s.GeoLocationId,
        Status = s.Status,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
    };

    private static void ValidateStoreType(string type)
    {
        var valid = new[] { nameof(StoreType.Physical), nameof(StoreType.Digital) };
        if (!valid.Contains(type))
            throw new ArgumentException($"Invalid store type '{type}'. Valid values: {string.Join(", ", valid)}.");
    }

    private static void ValidateStoreStatus(string status)
    {
        var valid = new[] { nameof(StoreStatus.Active), nameof(StoreStatus.Inactive), nameof(StoreStatus.Archived) };
        if (!valid.Contains(status))
            throw new ArgumentException($"Invalid store status '{status}'. Valid values: {string.Join(", ", valid)}.");
    }
}
