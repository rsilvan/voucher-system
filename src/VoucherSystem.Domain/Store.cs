namespace VoucherSystem.Domain;

public enum StoreType
{
    Physical,
    Digital
}

public enum StoreStatus
{
    Active,
    Inactive,
    Archived
}

public class Store
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
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
    public string Status { get; set; } = nameof(StoreStatus.Active);
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Project Project { get; set; } = default!;
}
