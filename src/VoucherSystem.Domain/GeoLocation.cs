namespace VoucherSystem.Domain;

public class GeoLocation
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Circle, Polygon, or MultiPolygon.</summary>
    public string Type { get; set; } = GeoLocationType.Circle;

    /// <summary>
    /// GeoJSON string stored as text in PostgreSQL.
    /// For Circle: stores the full circle geometry GeoJSON (e.g. {"type":"Circle","coordinates":[lng,lat],"radius":R,"unit":"km"}).
    /// For Polygon/MultiPolygon: stores the standard GeoJSON geometry object.
    /// </summary>
    public string Coordinates { get; set; } = "{}";

    /// <summary>Latitude in decimal degrees (-90 to 90). Used primarily for Circle type.</summary>
    public double? Latitude { get; set; }

    /// <summary>Longitude in decimal degrees (-180 to 180). Used primarily for Circle type.</summary>
    public double? Longitude { get; set; }

    /// <summary>Radius value. Used for Circle type.</summary>
    public double? Radius { get; set; }

    /// <summary>Unit of measurement for radius: "km" or "mi".</summary>
    public string? Unit { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
