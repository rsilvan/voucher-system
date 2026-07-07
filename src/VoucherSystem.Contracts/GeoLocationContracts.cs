namespace VoucherSystem.Contracts.GeoLocations;

public class GeoLocationResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public string Coordinates { get; set; } = "{}";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Radius { get; set; }
    public string? Unit { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class CreateGeoLocationRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = "Circle";
    public string Coordinates { get; set; } = "{}";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Radius { get; set; }
    public string? Unit { get; set; }
}

public class UpdateGeoLocationRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Coordinates { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Radius { get; set; }
    public string? Unit { get; set; }
}

public class GeoLocationListResponse
{
    public List<GeoLocationResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class GeoLocationValidationRequest
{
    public string Type { get; set; } = "Circle";
    public string Coordinates { get; set; } = "{}";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Radius { get; set; }
    public string? Unit { get; set; }
}

public class GeoLocationValidationResponse
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
