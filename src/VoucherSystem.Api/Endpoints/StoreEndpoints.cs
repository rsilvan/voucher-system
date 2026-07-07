using VoucherSystem.Api.Middleware;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Contracts.Stores;

namespace VoucherSystem.Api.Endpoints;

public static class StoreEndpoints
{
    public static void MapStoreEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/stores");

        // List stores
        group.MapGet("/", async (Guid projectId, IStoreService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Stores");
            try
            {
                var result = await service.GetByProjectAsync(projectId);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing stores for project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("stores.read");

        // Get store by ID
        group.MapGet("/{storeId:guid}", async (Guid projectId, Guid storeId, IStoreService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Stores");
            try
            {
                var result = await service.GetByIdAsync(storeId, projectId);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting store {StoreId}", storeId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("stores.read");

        // Create store
        group.MapPost("/", async (Guid projectId, CreateStoreRequest request, IStoreService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Stores");
            try
            {
                var result = await service.CreateAsync(projectId, request);
                return Results.Created($"/api/projects/{projectId}/stores/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating store for project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("stores.create");

        // Update store
        group.MapPatch("/{storeId:guid}", async (Guid projectId, Guid storeId, UpdateStoreRequest request, IStoreService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Stores");
            try
            {
                var result = await service.UpdateAsync(storeId, projectId, request);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating store {StoreId}", storeId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("stores.update");

        // Delete store (soft delete)
        group.MapDelete("/{storeId:guid}", async (Guid projectId, Guid storeId, IStoreService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Stores");
            try
            {
                var ok = await service.DeleteAsync(storeId, projectId);
                return ok ? Results.NoContent() : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting store {StoreId}", storeId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("stores.delete");
    }
}
