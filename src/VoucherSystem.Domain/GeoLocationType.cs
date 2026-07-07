namespace VoucherSystem.Domain;

public static class GeoLocationType
{
    public const string Circle = "Circle";
    public const string Polygon = "Polygon";
    public const string MultiPolygon = "MultiPolygon";

    public static readonly string[] All = [Circle, Polygon, MultiPolygon];
}
