using System.Text.Json;
using VoucherSystem.Contracts.GeoLocations;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class GeoLocationService : IGeoLocationService
{
    private readonly IGeoLocationRepository _repo;
    private readonly IProjectRepository _projectRepo;

    public GeoLocationService(IGeoLocationRepository repo, IProjectRepository projectRepo)
    {
        _repo = repo;
        _projectRepo = projectRepo;
    }

    public async Task<GeoLocationListResponse> GetByProjectAsync(Guid projectId, Guid organizationId)
    {
        await EnsureProjectAccessAsync(projectId, organizationId);
        var items = await _repo.GetByProjectAsync(projectId);
        return new GeoLocationListResponse
        {
            Items = items.Where(g => !g.IsDeleted).Select(MapToResponse).ToList(),
            TotalCount = items.Count(g => !g.IsDeleted),
        };
    }

    public async Task<GeoLocationResponse?> GetByIdAsync(Guid id, Guid projectId, Guid organizationId)
    {
        await EnsureProjectAccessAsync(projectId, organizationId);
        var geo = await _repo.GetByIdAsync(id, projectId);
        return geo is null || geo.IsDeleted ? null : MapToResponse(geo);
    }

    public async Task<GeoLocationResponse> CreateAsync(Guid projectId, Guid organizationId, CreateGeoLocationRequest request)
    {
        await EnsureProjectAccessAsync(projectId, organizationId);

        var validation = Validate(new GeoLocationValidationRequest
        {
            Type = request.Type,
            Coordinates = request.Coordinates,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Radius = request.Radius,
            Unit = request.Unit,
        });

        if (!validation.IsValid)
            throw new ArgumentException($"Validation failed: {string.Join("; ", validation.Errors)}");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("GeoLocation name is required.");

        var now = DateTimeOffset.UtcNow;
        var geo = new GeoLocation
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Type = request.Type,
            Coordinates = request.Coordinates,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Radius = request.Radius,
            Unit = request.Unit,
            CreatedAt = now,
        };

        await _repo.AddAsync(geo);
        return MapToResponse(geo);
    }

    public async Task<GeoLocationResponse?> UpdateAsync(Guid id, Guid projectId, Guid organizationId, UpdateGeoLocationRequest request)
    {
        await EnsureProjectAccessAsync(projectId, organizationId);
        var geo = await _repo.GetByIdAsync(id, projectId);
        if (geo is null || geo.IsDeleted) return null;

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("GeoLocation name cannot be empty.");
            geo.Name = request.Name.Trim();
        }

        if (request.Description is not null)
            geo.Description = request.Description.Trim();

        if (request.Type is not null)
            geo.Type = request.Type;

        if (request.Coordinates is not null)
            geo.Coordinates = request.Coordinates;

        if (request.Latitude.HasValue)
            geo.Latitude = request.Latitude;

        if (request.Longitude.HasValue)
            geo.Longitude = request.Longitude;

        if (request.Radius.HasValue)
            geo.Radius = request.Radius;

        if (request.Unit is not null)
            geo.Unit = request.Unit;

        // If type/coords/lat/lon/radius/unit changed, re-validate
        var validation = Validate(new GeoLocationValidationRequest
        {
            Type = geo.Type,
            Coordinates = geo.Coordinates,
            Latitude = geo.Latitude,
            Longitude = geo.Longitude,
            Radius = geo.Radius,
            Unit = geo.Unit,
        });

        if (!validation.IsValid)
            throw new ArgumentException($"Validation failed: {string.Join("; ", validation.Errors)}");

        geo.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(geo);
        return MapToResponse(geo);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid projectId, Guid organizationId)
    {
        await EnsureProjectAccessAsync(projectId, organizationId);
        var geo = await _repo.GetByIdAsync(id, projectId);
        if (geo is null || geo.IsDeleted) return false;

        geo.IsDeleted = true;
        geo.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.DeleteAsync(geo);
        return true;
    }

    public GeoLocationValidationResponse Validate(GeoLocationValidationRequest request)
    {
        var errors = new List<string>();

        if (!GeoLocationType.All.Contains(request.Type))
        {
            errors.Add($"Invalid type '{request.Type}'. Must be one of: {string.Join(", ", GeoLocationType.All)}.");
            return new GeoLocationValidationResponse { IsValid = false, Errors = errors };
        }

        switch (request.Type)
        {
            case GeoLocationType.Circle:
                ValidateCircle(request, errors);
                break;
            case GeoLocationType.Polygon:
                ValidatePolygon(request, errors);
                break;
            case GeoLocationType.MultiPolygon:
                ValidateMultiPolygon(request, errors);
                break;
        }

        return new GeoLocationValidationResponse
        {
            IsValid = errors.Count == 0,
            Errors = errors,
        };
    }

    private static void ValidateCircle(GeoLocationValidationRequest request, List<string> errors)
    {
        if (request.Latitude is null)
            errors.Add("Latitude is required for Circle type.");
        else if (request.Latitude < -90 || request.Latitude > 90)
            errors.Add($"Latitude must be between -90 and 90. Got {request.Latitude}.");

        if (request.Longitude is null)
            errors.Add("Longitude is required for Circle type.");
        else if (request.Longitude < -180 || request.Longitude > 180)
            errors.Add($"Longitude must be between -180 and 180. Got {request.Longitude}.");

        if (request.Radius is null)
            errors.Add("Radius is required for Circle type.");
        else if (request.Radius <= 0)
            errors.Add($"Radius must be positive. Got {request.Radius}.");

        if (request.Unit is not null && request.Unit != "km" && request.Unit != "mi")
            errors.Add($"Unit must be 'km' or 'mi'. Got '{request.Unit}'.");
    }

    private static void ValidatePolygon(GeoLocationValidationRequest request, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(request.Coordinates) || request.Coordinates == "{}")
        {
            errors.Add("Coordinates (GeoJSON) is required for Polygon type.");
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(request.Coordinates);
            var root = doc.RootElement;

            // Check type
            if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "Polygon")
            {
                errors.Add("GeoJSON type must be 'Polygon'.");
                return;
            }

            // Check coordinates
            if (!root.TryGetProperty("coordinates", out var coords) || coords.ValueKind != JsonValueKind.Array)
            {
                errors.Add("GeoJSON must have a 'coordinates' array.");
                return;
            }

            var rings = coords.EnumerateArray().ToList();
            if (rings.Count == 0)
            {
                errors.Add("Polygon must have at least one ring (coordinates array is empty).");
                return;
            }

            foreach (var (ring, idx) in rings.Select((r, i) => (r, i)))
            {
                if (ring.ValueKind != JsonValueKind.Array)
                {
                    errors.Add($"Ring {idx}: must be an array of coordinates.");
                    continue;
                }

                var points = ring.EnumerateArray().ToList();
                if (points.Count < 4)
                {
                    errors.Add($"Ring {idx}: must have at least 4 points (3 vertices + closing point). Got {points.Count}.");
                    continue;
                }

                // Check that ring is closed (first point == last point)
                var first = points[0];
                var last = points[^1];
                if (!PointsEqual(first, last))
                    errors.Add($"Ring {idx}: is not closed. First point must equal last point.");
            }
        }
        catch (JsonException)
        {
            errors.Add("Coordinates is not valid GeoJSON.");
        }
    }

    private static void ValidateMultiPolygon(GeoLocationValidationRequest request, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(request.Coordinates) || request.Coordinates == "{}")
        {
            errors.Add("Coordinates (GeoJSON) is required for MultiPolygon type.");
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(request.Coordinates);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "MultiPolygon")
            {
                errors.Add("GeoJSON type must be 'MultiPolygon'.");
                return;
            }

            if (!root.TryGetProperty("coordinates", out var coords) || coords.ValueKind != JsonValueKind.Array)
            {
                errors.Add("GeoJSON must have a 'coordinates' array.");
                return;
            }

            var polygons = coords.EnumerateArray().ToList();
            if (polygons.Count == 0)
            {
                errors.Add("MultiPolygon must have at least one polygon.");
                return;
            }

            foreach (var (polygon, polyIdx) in polygons.Select((p, i) => (p, i)))
            {
                if (polygon.ValueKind != JsonValueKind.Array)
                {
                    errors.Add($"Polygon {polyIdx}: must be an array of rings.");
                    continue;
                }

                var rings = polygon.EnumerateArray().ToList();
                if (rings.Count == 0)
                {
                    errors.Add($"Polygon {polyIdx}: must have at least one ring.");
                    continue;
                }

                foreach (var (ring, ringIdx) in rings.Select((r, i) => (r, i)))
                {
                    if (ring.ValueKind != JsonValueKind.Array)
                    {
                        errors.Add($"Polygon {polyIdx}, ring {ringIdx}: must be an array of coordinates.");
                        continue;
                    }

                    var points = ring.EnumerateArray().ToList();
                    if (points.Count < 4)
                    {
                        errors.Add($"Polygon {polyIdx}, ring {ringIdx}: must have at least 4 points.");
                        continue;
                    }

                    var first = points[0];
                    var last = points[^1];
                    if (!PointsEqual(first, last))
                        errors.Add($"Polygon {polyIdx}, ring {ringIdx}: is not closed.");
                }
            }
        }
        catch (JsonException)
        {
            errors.Add("Coordinates is not valid GeoJSON.");
        }
    }

    private static bool PointsEqual(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != JsonValueKind.Array || b.ValueKind != JsonValueKind.Array)
            return false;
        var aArr = a.EnumerateArray().Select(x => x.GetDouble()).ToList();
        var bArr = b.EnumerateArray().Select(x => x.GetDouble()).ToList();
        if (aArr.Count != bArr.Count) return false;
        return aArr.SequenceEqual(bArr);
    }

    private async Task EnsureProjectAccessAsync(Guid projectId, Guid organizationId)
    {
        // Since we just need to verify the project exists under this org
        var project = await _projectRepo.GetByIdAsync(projectId, organizationId);
        if (project is null)
            throw new KeyNotFoundException($"Project {projectId} not found in organization {organizationId}.");
    }

    private static GeoLocationResponse MapToResponse(GeoLocation g) => new()
    {
        Id = g.Id,
        ProjectId = g.ProjectId,
        Name = g.Name,
        Description = g.Description,
        Type = g.Type,
        Coordinates = g.Coordinates,
        Latitude = g.Latitude,
        Longitude = g.Longitude,
        Radius = g.Radius,
        Unit = g.Unit,
        CreatedAt = g.CreatedAt,
        UpdatedAt = g.UpdatedAt,
    };
}
