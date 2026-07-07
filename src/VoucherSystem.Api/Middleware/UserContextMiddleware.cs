using System.Security.Claims;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Api.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthRepository authRepo, IPermissionCache permCache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var orgIdClaim = context.User.FindFirst("organization_id")?.Value;
            var roleIdClaim = context.User.FindFirst("role_id")?.Value;

            if (Guid.TryParse(userIdClaim, out var userId) &&
                Guid.TryParse(orgIdClaim, out var orgId) &&
                Guid.TryParse(roleIdClaim, out var roleId))
            {
                context.Items["UserId"] = userId;
                context.Items["OrganizationId"] = orgId;
                context.Items["RoleId"] = roleId;

                // Load permissions from cache or DB
                var permissions = await permCache.GetPermissionsAsync(roleId);
                if (permissions.Count == 0)
                {
                    var dbPerms = await authRepo.GetRolePermissionsAsync(roleId);
                    if (dbPerms.Count > 0)
                    {
                        permissions = dbPerms.Select(p => p.Key).ToList();
                        await permCache.SetPermissionsAsync(roleId, dbPerms);
                    }
                }

                context.Items["Permissions"] = new HashSet<string>(permissions);

                // Extract project context from claims
                var projectIdClaim = context.User.FindFirst("project_id")?.Value;
                if (Guid.TryParse(projectIdClaim, out var projectId))
                {
                    context.Items["CurrentProjectId"] = projectId;
                    context.Items["ProjectId"] = projectId;
                }
                var projEnvClaim = context.User.FindFirst("project_environment")?.Value;
                if (!string.IsNullOrEmpty(projEnvClaim))
                    context.Items["ProjectEnvironment"] = projEnvClaim;
                var projStatusClaim = context.User.FindFirst("project_status")?.Value;
                if (!string.IsNullOrEmpty(projStatusClaim))
                    context.Items["ProjectStatus"] = projStatusClaim;

                // Set current member info
                var memberIdClaim = context.User.FindFirst("member_id")?.Value;
                if (Guid.TryParse(memberIdClaim, out var memberId))
                    context.Items["MemberId"] = memberId;
            }
        }

        await _next(context);
    }
}

public static class PermissionExtensions
{
    /// <summary>Requires that the authenticated user has the specified permission.</summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            var permissions = httpContext.Items["Permissions"] as HashSet<string>;

            if (permissions == null || !permissions.Contains(permission))
            {
                return Results.Json(new { error = "Forbidden: insufficient permissions" }, statusCode: 403);
            }

            return await next(context);
        });
    }

    /// <summary>Helper to get current user context from HttpContext.Items.</summary>
    public static UserContext GetUserContext(this HttpContext context)
    {
        return new UserContext
        {
            UserId = (Guid)(context.Items["UserId"] ?? Guid.Empty),
            OrganizationId = (Guid)(context.Items["OrganizationId"] ?? Guid.Empty),
            RoleId = (Guid)(context.Items["RoleId"] ?? Guid.Empty),
            MemberId = (Guid?)(context.Items["MemberId"]),
            Permissions = (HashSet<string>?)(context.Items["Permissions"]) ?? new()
        };
    }
}

public class UserContext
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? MemberId { get; set; }
    public HashSet<string> Permissions { get; set; } = new();

    public bool HasPermission(string permission) => Permissions.Contains(permission);
}
