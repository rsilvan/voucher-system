using VoucherSystem.Api.Middleware;
using VoucherSystem.Application;
using VoucherSystem.Contracts;

namespace VoucherSystem.Api.Endpoints;

public static class MemberEndpoints
{
    public static void MapMemberEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/organizations/current/members")
            .RequireAuthorization();

        // List members
        group.MapGet("/", async (IMemberService service, HttpContext httpContext) =>
        {
            var ctx = httpContext.GetUserContext();
            var members = await service.GetMembersAsync(ctx.OrganizationId);
            return Results.Ok(members);
        }).RequirePermission("users.read");

        // Get single member
        group.MapGet("/{memberId:guid}", async (Guid memberId, IMemberService service, HttpContext httpContext) =>
        {
            var ctx = httpContext.GetUserContext();
            var member = await service.GetMemberAsync(ctx.OrganizationId, memberId);
            return member is not null ? Results.Ok(member) : Results.NotFound();
        }).RequirePermission("users.read");

        // Invite member
        group.MapPost("/invite", async (InviteMemberRequest request, IMemberService service, HttpContext httpContext, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Members");
            try
            {
                var ctx = httpContext.GetUserContext();
                var result = await service.InviteMemberAsync(ctx.OrganizationId, ctx.UserId, request);
                return Results.Created($"/api/organizations/current/members", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inviting member");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequirePermission("users.invite");

        // Disable member
        group.MapPost("/{memberId:guid}/disable", async (Guid memberId, IMemberService service, HttpContext httpContext, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Members");
            try
            {
                var ctx = httpContext.GetUserContext();
                await service.DisableMemberAsync(ctx.OrganizationId, memberId);
                return Results.Ok(new { status = "Disabled" });
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(new ErrorResponse { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disabling member");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequirePermission("users.disable");

        // Enable member
        group.MapPost("/{memberId:guid}/enable", async (Guid memberId, IMemberService service, HttpContext httpContext, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Members");
            try
            {
                var ctx = httpContext.GetUserContext();
                await service.EnableMemberAsync(ctx.OrganizationId, memberId);
                return Results.Ok(new { status = "Enabled" });
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error enabling member");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequirePermission("users.enable");
    }
}
