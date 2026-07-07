using VoucherSystem.Api.Middleware;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Contracts.Areas;
using VoucherSystem.Contracts.Stores;

namespace VoucherSystem.Api.Endpoints;

public static class AreaEndpoints
{
    public static void MapAreaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/areas");

        // Get area tree
        group.MapGet("/", async (Guid projectId, IAreaService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Areas");
            try
            {
                var result = await service.GetTreeAsync(projectId);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing areas for project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("areas.read");

        // Get area by ID (with children and stores)
        group.MapGet("/{areaId:guid}", async (Guid projectId, Guid areaId, IAreaService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Areas");
            try
            {
                var result = await service.GetByIdAsync(areaId, projectId);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting area {AreaId}", areaId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("areas.read");

        // Create area
        group.MapPost("/", async (Guid projectId, CreateAreaRequest request, IAreaService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Areas");
            try
            {
                var result = await service.CreateAsync(projectId, request);
                return Results.Created($"/api/projects/{projectId}/areas/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating area for project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("areas.create");

        // Update area
        group.MapPatch("/{areaId:guid}", async (Guid projectId, Guid areaId, UpdateAreaRequest request, IAreaService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Areas");
            try
            {
                var result = await service.UpdateAsync(areaId, projectId, request);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating area {AreaId}", areaId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("areas.update");

        // Delete area (soft delete with cascade to children)
        group.MapDelete("/{areaId:guid}", async (Guid projectId, Guid areaId, IAreaService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Areas");
            try
            {
                var ok = await service.DeleteAsync(areaId, projectId);
                return ok ? Results.NoContent() : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting area {AreaId}", areaId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("areas.delete");

        // Assign stores to area
        group.MapPost("/{areaId:guid}/stores", async (Guid projectId, Guid areaId, AssignStoresToAreaRequest request, IAreaService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Areas");
            try
            {
                await service.AssignStoresAsync(areaId, projectId, request.StoreIds);
                return Results.Ok();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning stores to area {AreaId}", areaId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("areas.update");

        // Unassign store from area
        group.MapDelete("/{areaId:guid}/stores/{storeId:guid}", async (Guid projectId, Guid areaId, Guid storeId, IAreaService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Areas");
            try
            {
                await service.UnassignStoreAsync(areaId, projectId, storeId);
                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error unassigning store {StoreId} from area {AreaId}", storeId, areaId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("areas.update");
    }
}
