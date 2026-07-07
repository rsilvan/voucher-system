using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Api.Middleware;

namespace VoucherSystem.Api.Endpoints;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/roles").RequireAuthorization();

        group.MapGet("/", async (IRoleService service, HttpContext httpContext) =>
        {
            var ctx = httpContext.GetUserContext();
            var roles = await service.GetRolesAsync(ctx.OrganizationId);
            return Results.Ok(roles);
        }).RequirePermission("roles.read");

        group.MapGet("/{roleId:guid}", async (Guid roleId, IRoleService service, HttpContext httpContext) =>
        {
            var ctx = httpContext.GetUserContext();
            var roles = await service.GetRolesAsync(ctx.OrganizationId);
            var role = roles.FirstOrDefault(r => r.Id == roleId);
            return role is not null ? Results.Ok(role) : Results.NotFound();
        }).RequirePermission("roles.read");

        group.MapPost("/", async (CreateRoleRequest request, IRoleService service, HttpContext httpContext, ILoggerFactory logger) =>
        {
            try
            {
                var ctx = httpContext.GetUserContext();
                var role = await service.CreateRoleAsync(ctx.OrganizationId, request);
                return Results.Created($"/api/roles/{role.Id}", role);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new ErrorResponse { Error = ex.Message }); }
            catch (Exception ex) { logger.CreateLogger("Roles").LogError(ex, "Error creating role"); return Results.StatusCode(500); }
        }).RequirePermission("roles.create");

        group.MapPatch("/{roleId:guid}", async (Guid roleId, UpdateRoleRequest request, IRoleService service, HttpContext httpContext, ILoggerFactory logger) =>
        {
            try
            {
                var ctx = httpContext.GetUserContext();
                var role = await service.UpdateRoleAsync(roleId, ctx.OrganizationId, request);
                return Results.Ok(role);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new ErrorResponse { Error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new ErrorResponse { Error = ex.Message }); }
            catch (Exception ex) { logger.CreateLogger("Roles").LogError(ex, "Error updating role"); return Results.StatusCode(500); }
        }).RequirePermission("roles.update");

        group.MapDelete("/{roleId:guid}", async (Guid roleId, IRoleService service, HttpContext httpContext, ILoggerFactory logger) =>
        {
            try
            {
                var ctx = httpContext.GetUserContext();
                await service.DeleteRoleAsync(roleId, ctx.OrganizationId);
                return Results.NoContent();
            }
            catch (ArgumentException ex) { return Results.NotFound(new ErrorResponse { Error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new ErrorResponse { Error = ex.Message }); }
            catch (Exception ex) { logger.CreateLogger("Roles").LogError(ex, "Error deleting role"); return Results.StatusCode(500); }
        }).RequirePermission("roles.delete");
    }
}
