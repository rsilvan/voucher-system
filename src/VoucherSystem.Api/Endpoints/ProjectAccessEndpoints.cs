using VoucherSystem.Api.Middleware;
using VoucherSystem.Application;
using VoucherSystem.Contracts;

namespace VoucherSystem.Api.Endpoints;

public static class ProjectAccessEndpoints
{
    public static void MapProjectAccessEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/organizations/current/members/{memberId:guid}/projects")
            .RequireAuthorization();

        // GET: list project IDs for a member
        group.MapGet("/", async (Guid memberId, IProjectAccessService service, HttpContext httpContext) =>
        {
            var ctx = httpContext.GetUserContext();
            if (!ctx.HasPermission("users.read"))
                return Results.Json(new { error = "Forbidden" }, statusCode: 403);

            var projectIds = await service.GetMemberProjectIdsAsync(memberId);
            return Results.Ok(new { projectIds });
        }).RequirePermission("users.read");

        // PUT: set project access for a member
        group.MapPut("/", async (Guid memberId, UpdateProjectAccessRequest request,
            IProjectAccessService service, HttpContext httpContext, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ProjectAccess");
            try
            {
                var ctx = httpContext.GetUserContext();
                if (!ctx.HasPermission("users.update"))
                    return Results.Json(new { error = "Forbidden" }, statusCode: 403);

                await service.SetMemberProjectAccessAsync(memberId, request.RoleId, request.ProjectIds);
                return Results.Ok(new { status = "Updated", projectIds = request.ProjectIds });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating project access");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequirePermission("users.update");
    }
}
