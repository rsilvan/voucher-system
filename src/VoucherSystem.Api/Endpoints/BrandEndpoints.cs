using VoucherSystem.Api.Middleware;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Contracts.Brands;

namespace VoucherSystem.Api.Endpoints;

public static class BrandEndpoints
{
    public static void MapBrandEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/brand");

        // GET
        group.MapGet("/", async (Guid projectId, IBrandService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Brands");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.GetByProjectAsync(projectId, userCtx.OrganizationId);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting brand for project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("brands.read");

        // CREATE
        group.MapPost("/", async (Guid projectId, CreateBrandRequest request, IBrandService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Brands");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.CreateAsync(projectId, userCtx.OrganizationId, request);
                return Results.Created($"/api/projects/{projectId}/brand", result);
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
                logger.LogError(ex, "Error creating brand for project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("brands.create");

        // UPDATE
        group.MapPut("/", async (Guid projectId, UpdateBrandRequest request, IBrandService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Brands");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.UpdateAsync(projectId, userCtx.OrganizationId, request);
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
                logger.LogError(ex, "Error updating brand for project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("brands.update");

        // DELETE
        group.MapDelete("/", async (Guid projectId, IBrandService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Brands");
            try
            {
                var userCtx = ctx.GetUserContext();
                var ok = await service.DeleteAsync(projectId, userCtx.OrganizationId);
                return ok ? Results.Ok() : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting brand for project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("brands.delete");
    }
}
