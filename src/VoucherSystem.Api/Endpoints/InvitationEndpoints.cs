using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Api.Middleware;

namespace VoucherSystem.Api.Endpoints;

public static class InvitationEndpoints
{
    public static void MapInvitationEndpoints(this WebApplication app)
    {
        // Public: accept invitation (no auth required - user doesn't have account yet)
        var publicGroup = app.MapGroup("/api/invitations");

        publicGroup.MapGet("/{token}", async (string token, IMemberService service, ILoggerFactory loggerFactory) =>
        {
            // Just validates the token exists - accept POST does the actual acceptance
            return Results.Ok(new { message = "Token received. Use POST to accept." });
        });

        publicGroup.MapPost("/{token}/accept", async (string token, AcceptInvitationRequest request, IMemberService service, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Invitations");
            try
            {
                var result = await service.AcceptInvitationAsync(token, request);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error accepting invitation");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        });

        // Protected: manage invitations
        var adminGroup = app.MapGroup("/api/organizations/current/invitations")
            .RequireAuthorization();

        adminGroup.MapGet("/", async (HttpContext httpContext) =>
        {
            // List pending invitations for the organization
            return Results.Ok(new { message = "List invitations - to be implemented" });
        }).RequirePermission("users.read");

        // Resend invitation
        adminGroup.MapPost("/{id:guid}/resend", async (Guid id, HttpContext httpContext, IMemberService service, IAuditLogWriter auditLog, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Invitations");
            try
            {
                var ctx = httpContext.GetUserContext();
                var (token, expiresAt) = await service.ResendInvitationAsync(ctx.OrganizationId, id, ctx.UserId);
                auditLog.Write(ctx.OrganizationId, null, ctx.UserId, "invitation.resend", "Invitation", id.ToString());
                await auditLog.SaveAsync();
                return Results.Ok(new { token, expiresAt, message = "Invitation resent. New token generated." });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resending invitation {Id}", id);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequirePermission("users.invite");

        // Revoke invitation
        adminGroup.MapPost("/{id:guid}/revoke", async (Guid id, HttpContext httpContext, IMemberService service, IAuditLogWriter auditLog, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Invitations");
            try
            {
                var ctx = httpContext.GetUserContext();
                await service.RevokeInvitationAsync(ctx.OrganizationId, id, ctx.UserId);
                auditLog.Write(ctx.OrganizationId, null, ctx.UserId, "invitation.revoked", "Invitation", id.ToString());
                await auditLog.SaveAsync();
                return Results.Ok(new { message = "Invitation revoked." });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error revoking invitation {Id}", id);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequirePermission("users.invite");
    }
}
