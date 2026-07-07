namespace VoucherSystem.Contracts.Stores;

public class StoreResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string StoreType { get; set; } = default!;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public Guid? GeoLocationId { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class CreateStoreRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string StoreType { get; set; } = "Physical";
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public Guid? GeoLocationId { get; set; }
}

public class UpdateStoreRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? StoreType { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public Guid? GeoLocationId { get; set; }
    public string? Status { get; set; }
}

public class StoreSummaryResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string StoreType { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}

public class StoreListResponse
{
    public List<StoreSummaryResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
