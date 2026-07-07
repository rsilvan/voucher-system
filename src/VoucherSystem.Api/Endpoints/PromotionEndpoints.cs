using VoucherSystem.Api.Middleware;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Contracts.Promotions;

namespace VoucherSystem.Api.Endpoints;

public static class PromotionEndpoints
{
    public static void MapPromotionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/promotions");

        // Get promotion plan
        group.MapGet("/plan", async (Guid projectId, [AsParameters] PromotionPlanQuery query, IPromotionService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Promotions");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.GetPlanAsync(userCtx.OrganizationId, projectId, query.TargetProjectId);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting promotion plan");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.promote");

        // List promotion jobs
        group.MapGet("/", async (Guid projectId, IPromotionService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Promotions");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.GetJobsAsync(userCtx.OrganizationId, projectId);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing promotions");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.promote");

        // Create promotion job
        group.MapPost("/", async (Guid projectId, CreatePromotionRequest request, IPromotionService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Promotions");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.CreatePromotionAsync(userCtx.OrganizationId, projectId, request, userCtx.UserId);
                return Results.Created($"/api/projects/{projectId}/promotions/{result.Id}", result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating promotion");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.promote");

        // Get job status
        group.MapGet("/{jobId:guid}", async (Guid projectId, Guid jobId, IPromotionService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Promotions");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.GetJobAsync(userCtx.OrganizationId, jobId);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting promotion job");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.promote");

        // Cancel job
        group.MapPost("/{jobId:guid}/cancel", async (Guid projectId, Guid jobId, IPromotionService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Promotions");
            try
            {
                var userCtx = ctx.GetUserContext();
                var ok = await service.CancelJobAsync(userCtx.OrganizationId, jobId);
                return ok ? Results.Ok() : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cancelling promotion job");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.promote");

        // Get promotion diff (compare plan vs result)
        group.MapGet("/{jobId:guid}/diff", async (Guid projectId, Guid jobId, IPromotionService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Promotions");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.GetPromotionDiffAsync(userCtx.OrganizationId, jobId);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting promotion diff");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.promote");
    }
}

public class PromotionPlanQuery
{
    public Guid TargetProjectId { get; set; }
}
