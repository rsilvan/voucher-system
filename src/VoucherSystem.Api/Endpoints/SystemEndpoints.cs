using Microsoft.EntityFrameworkCore;
using VoucherSystem.Api.Middleware;
using VoucherSystem.Contracts;

namespace VoucherSystem.Api.Endpoints;

public static class SystemEndpoints
{
    public static void MapSystemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/permissions").RequireAuthorization();

        // List all available permissions
        group.MapGet("/", async (Infrastructure.VoucherSystemDbContext db, HttpContext httpContext) =>
        {
            var ctx = httpContext.GetUserContext();
            if (!ctx.HasPermission("permissions.read"))
                return Results.Json(new { error = "Forbidden" }, statusCode: 403);

            var permissions = await db.Permissions
                .OrderBy(p => p.Key)
                .Select(p => new PermissionResponse
                {
                    Id = p.Id,
                    Key = p.Key,
                    Resource = p.Resource,
                    Action = p.Action,
                    Description = p.Description
                })
                .ToListAsync();

            return Results.Ok(permissions);
        });
    }
}
