using VoucherSystem.Api.Middleware;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Contracts.GeoLocations;

namespace VoucherSystem.Api.Endpoints;

public static class GeoLocationEndpoints
{
    public static void MapGeoLocationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/locations");

        // List all locations for a project
        group.MapGet("/", async (Guid projectId, IGeoLocationService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("GeoLocations");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.GetByProjectAsync(projectId, userCtx.OrganizationId);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ErrorResponse { Error = "Project not found." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing geo locations for project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("locations.read");

        // Get a specific location
        group.MapGet("/{id:guid}", async (Guid projectId, Guid id, IGeoLocationService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("GeoLocations");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.GetByIdAsync(id, projectId, userCtx.OrganizationId);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ErrorResponse { Error = "Project not found." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting geo location {Id} for project {ProjectId}", id, projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("locations.read");

        // Create a location
        group.MapPost("/", async (Guid projectId, CreateGeoLocationRequest request, IGeoLocationService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("GeoLocations");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.CreateAsync(projectId, userCtx.OrganizationId, request);
                return Results.Created($"/api/projects/{projectId}/locations/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ErrorResponse { Error = "Project not found." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating geo location for project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("locations.create");

        // Update a location
        group.MapPatch("/{id:guid}", async (Guid projectId, Guid id, UpdateGeoLocationRequest request, IGeoLocationService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("GeoLocations");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.UpdateAsync(id, projectId, userCtx.OrganizationId, request);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ErrorResponse { Error = "Project not found." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating geo location {Id} for project {ProjectId}", id, projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("locations.update");

        // Delete a location (soft delete)
        group.MapDelete("/{id:guid}", async (Guid projectId, Guid id, IGeoLocationService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("GeoLocations");
            try
            {
                var userCtx = ctx.GetUserContext();
                var ok = await service.DeleteAsync(id, projectId, userCtx.OrganizationId);
                return ok ? Results.Ok() : Results.NotFound();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ErrorResponse { Error = "Project not found." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting geo location {Id} for project {ProjectId}", id, projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("locations.delete");

        // Validate a location without saving
        group.MapPost("/validate", async (Guid projectId, GeoLocationValidationRequest request, IGeoLocationService service, HttpContext ctx) =>
        {
            try
            {
                var userCtx = ctx.GetUserContext();
                // Even though validation doesn't need project access, we verify the project exists
                var result = service.Validate(request);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new GeoLocationValidationResponse
                {
                    IsValid = false,
                    Errors = new List<string> { $"Unexpected error: {ex.Message}" }
                }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("locations.create");
    }
}
