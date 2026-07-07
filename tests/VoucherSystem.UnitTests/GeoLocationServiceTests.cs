using FluentAssertions;
using VoucherSystem.Application;
using VoucherSystem.Contracts.GeoLocations;
using VoucherSystem.Domain;

namespace VoucherSystem.UnitTests;

public class GeoLocationServiceValidationTests
{
    private static GeoLocationService CreateService()
    {
        return new GeoLocationService(
            new MockGeoLocationRepository(),
            new MockProjectRepository());
    }

    [Fact]
    public void Validate_Circle_Valid_ReturnsValid()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Circle",
            Latitude = -23.5505,
            Longitude = -46.6333,
            Radius = 10.0,
            Unit = "km",
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Circle_InvalidLatitude_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Circle",
            Latitude = 100.0,
            Longitude = -46.6333,
            Radius = 10.0,
            Unit = "km",
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Latitude must be between -90 and 90"));
    }

    [Fact]
    public void Validate_Circle_InvalidLongitude_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Circle",
            Latitude = -23.5505,
            Longitude = 200.0,
            Radius = 10.0,
            Unit = "km",
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Longitude must be between -180 and 180"));
    }

    [Fact]
    public void Validate_Circle_NegativeRadius_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Circle",
            Latitude = -23.5505,
            Longitude = -46.6333,
            Radius = -5.0,
            Unit = "km",
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Radius must be positive"));
    }

    [Fact]
    public void Validate_Circle_InvalidUnit_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Circle",
            Latitude = -23.5505,
            Longitude = -46.6333,
            Radius = 10.0,
            Unit = "miles",
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Unit must be 'km' or 'mi'"));
    }

    [Fact]
    public void Validate_Circle_MissingLatitude_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Circle",
            Longitude = -46.6333,
            Radius = 10.0,
            Unit = "km",
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Latitude is required"));
    }

    [Fact]
    public void Validate_Circle_MissingRadius_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Circle",
            Latitude = -23.5505,
            Longitude = -46.6333,
            Unit = "km",
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Radius is required"));
    }

    [Fact]
    public void Validate_Polygon_Closed_ReturnsValid()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Polygon",
            Coordinates = """
                {
                    "type": "Polygon",
                    "coordinates": [[
                        [-46.6, -23.5],
                        [-46.5, -23.5],
                        [-46.5, -23.6],
                        [-46.6, -23.6],
                        [-46.6, -23.5]
                    ]]
                }
                """,
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Polygon_NotClosed_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Polygon",
            Coordinates = """
                {
                    "type": "Polygon",
                    "coordinates": [[
                        [-46.6, -23.5],
                        [-46.5, -23.5],
                        [-46.5, -23.6],
                        [-46.6, -23.6]
                    ]]
                }
                """,
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not closed"));
    }

    [Fact]
    public void Validate_Polygon_Empty_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Polygon",
            Coordinates = """
                {
                    "type": "Polygon",
                    "coordinates": []
                }
                """,
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("empty"));
    }

    [Fact]
    public void Validate_Polygon_TooFewPoints_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Polygon",
            Coordinates = """
                {
                    "type": "Polygon",
                    "coordinates": [[
                        [-46.6, -23.5],
                        [-46.5, -23.5],
                        [-46.6, -23.5]
                    ]]
                }
                """,
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("at least 4 points"));
    }

    [Fact]
    public void Validate_MultiPolygon_Valid_ReturnsValid()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "MultiPolygon",
            Coordinates = """
                {
                    "type": "MultiPolygon",
                    "coordinates": [[[
                        [-46.6, -23.5],
                        [-46.5, -23.5],
                        [-46.5, -23.6],
                        [-46.6, -23.6],
                        [-46.6, -23.5]
                    ]]]
                }
                """,
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_MultiPolygon_NotClosed_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "MultiPolygon",
            Coordinates = """
                {
                    "type": "MultiPolygon",
                    "coordinates": [[[
                        [-46.6, -23.5],
                        [-46.5, -23.5],
                        [-46.5, -23.6],
                        [-46.6, -23.6]
                    ]]]
                }
                """,
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not closed"));
    }

    [Fact]
    public void Validate_InvalidType_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "LineString",
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid type"));
    }

    [Fact]
    public void Validate_EmptyCoordinatesForPolygon_ReturnsErrors()
    {
        var service = CreateService();
        var request = new GeoLocationValidationRequest
        {
            Type = "Polygon",
            Coordinates = "{}",
        };

        var result = service.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Coordinates (GeoJSON) is required"));
    }

    [Fact]
    public void GeoLocationEntity_HasRequiredFields()
    {
        var geo = new GeoLocation
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Test Location",
            Type = "Circle",
            Coordinates = "{}",
            Latitude = -23.5505,
            Longitude = -46.6333,
            Radius = 10.0,
            Unit = "km",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        geo.Id.Should().NotBeEmpty();
        geo.ProjectId.Should().NotBeEmpty();
        geo.Name.Should().Be("Test Location");
        geo.Type.Should().Be("Circle");
        geo.Latitude.Should().Be(-23.5505);
        geo.Longitude.Should().Be(-46.6333);
        geo.Radius.Should().Be(10.0);
        geo.Unit.Should().Be("km");
        geo.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void GeoLocationType_Constants_AreCorrect()
    {
        GeoLocationType.Circle.Should().Be("Circle");
        GeoLocationType.Polygon.Should().Be("Polygon");
        GeoLocationType.MultiPolygon.Should().Be("MultiPolygon");
        GeoLocationType.All.Should().Contain(["Circle", "Polygon", "MultiPolygon"]);
        GeoLocationType.All.Should().HaveCount(3);
    }
}

/// <summary>
/// Mock repository for testing GeoLocationService validation (no actual DB needed).
/// </summary>
public class MockGeoLocationRepository : IGeoLocationRepository
{
    public Task<List<GeoLocation>> GetByProjectAsync(Guid projectId)
        => Task.FromResult(new List<GeoLocation>());

    public Task<GeoLocation?> GetByIdAsync(Guid id, Guid projectId)
        => Task.FromResult<GeoLocation?>(null);

    public Task AddAsync(GeoLocation geoLocation)
        => Task.CompletedTask;

    public Task UpdateAsync(GeoLocation geoLocation)
        => Task.CompletedTask;

    public Task DeleteAsync(GeoLocation geoLocation)
        => Task.CompletedTask;
}

/// <summary>
/// Mock project repository that always returns a project, for testing service methods.
/// </summary>
public class MockProjectRepository : IProjectRepository
{
    public Task<Project?> GetByIdAsync(Guid projectId, Guid organizationId)
        => Task.FromResult<Project?>(new Project
        {
            Id = projectId,
            OrganizationId = organizationId,
            Name = "Mock Project",
            Slug = "mock-project",
            Status = nameof(ProjectStatus.Active),
            CreatedAt = DateTimeOffset.UtcNow,
        });

    public Task<List<Project>> GetByOrganizationAsync(Guid organizationId)
        => Task.FromResult(new List<Project>());

    public Task<List<Project>> GetByMemberAsync(Guid organizationId, Guid memberId)
        => Task.FromResult(new List<Project>());

    public Task<bool> SlugExistsAsync(Guid organizationId, string slug)
        => Task.FromResult(false);

    public Task<Project?> GetPrimaryAsync(Guid organizationId)
        => Task.FromResult<Project?>(null);

    public Task<int> GetActiveCountAsync(Guid organizationId)
        => Task.FromResult(0);

    public Task AddAsync(Project project)
        => Task.CompletedTask;

    public Task UpdateAsync(Project project)
        => Task.CompletedTask;

    public Task<bool> HasResourcesAsync(Guid projectId)
        => Task.FromResult(false);
}
