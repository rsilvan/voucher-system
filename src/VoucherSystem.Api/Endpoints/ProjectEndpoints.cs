using VoucherSystem.Api.Middleware;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Contracts.Projects;

namespace VoucherSystem.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects");

        // List projects (by organization)
        group.MapGet("/", async (IProjectService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Projects");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.GetByOrganizationAsync(userCtx.OrganizationId, userCtx.MemberId);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing projects");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization();

        // Get project by ID
        group.MapGet("/{projectId:guid}", async (Guid projectId, IProjectService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Projects");
            try
            {
                var userCtx = ctx.GetUserContext();
                var result = await service.GetByIdAsync(projectId, userCtx.OrganizationId);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.read");

        // Create project
        group.MapPost("/", async (CreateProjectRequest request, IProjectService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Projects");
            try
            {
                var userCtx = ctx.GetUserContext();

                // Production requires special permission
                if (request.Environment == "Production" && !userCtx.HasPermission("projects.manage_production"))
                    return Results.Json(new ErrorResponse { Error = "You need the 'projects.manage_production' permission to create Production projects." }, statusCode: 403);

                var result = await service.CreateAsync(userCtx.OrganizationId, request);
                return Results.Created($"/api/projects/{result.Id}", result);
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
                logger.LogError(ex, "Error creating project");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.create");

        // Update project
        group.MapPatch("/{projectId:guid}", async (Guid projectId, UpdateProjectRequest request, IProjectService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Projects");
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
                logger.LogError(ex, "Error updating project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.update");

        // Disable project
        group.MapPost("/{projectId:guid}/disable", async (Guid projectId, IProjectService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Projects");
            try
            {
                var userCtx = ctx.GetUserContext();
                var ok = await service.DisableAsync(projectId, userCtx.OrganizationId);
                return ok ? Results.Ok() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disabling project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.update");

        // Enable project
        group.MapPost("/{projectId:guid}/enable", async (Guid projectId, IProjectService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Projects");
            try
            {
                var userCtx = ctx.GetUserContext();
                var ok = await service.EnableAsync(projectId, userCtx.OrganizationId);
                return ok ? Results.Ok() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error enabling project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.update");

        // Archive project
        group.MapPost("/{projectId:guid}/archive", async (Guid projectId, IProjectService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Projects");
            try
            {
                var userCtx = ctx.GetUserContext();
                var ok = await service.ArchiveAsync(projectId, userCtx.OrganizationId);
                return ok ? Results.Ok() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error archiving project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.update");

        // Restore project
        group.MapPost("/{projectId:guid}/restore", async (Guid projectId, IProjectService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Projects");
            try
            {
                var userCtx = ctx.GetUserContext();
                var ok = await service.RestoreAsync(projectId, userCtx.OrganizationId);
                return ok ? Results.Ok() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error restoring project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.update");

        // Make primary
        group.MapPost("/{projectId:guid}/make-primary", async (Guid projectId, IProjectService service, HttpContext ctx, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Projects");
            try
            {
                var userCtx = ctx.GetUserContext();
                var ok = await service.MakePrimaryAsync(projectId, userCtx.OrganizationId);
                return ok ? Results.Ok() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting primary project {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("projects.update");
    }
}
