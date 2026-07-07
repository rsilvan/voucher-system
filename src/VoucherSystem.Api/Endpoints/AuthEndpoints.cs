using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Api.Middleware;

namespace VoucherSystem.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (LoginRequest request,
            IAuthService authService,
            HttpContext httpContext,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Auth");
            try
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();
                var result = await authService.LoginAsync(request, ip, userAgent);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning("Login failed: {Message}", ex.Message);
                return Results.Json(new ErrorResponse { Error = ex.Message }, statusCode: 401);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error during login");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        });

        group.MapPost("/logout", async (RefreshTokenRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Auth");
            try
            {
                await authService.LogoutAsync(request.RefreshToken);
                return Results.Ok(new { status = "Logged out" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during logout");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        });

        group.MapPost("/refresh-token", async (RefreshTokenRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Auth");
            try
            {
                var result = await authService.RefreshTokenAsync(request);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new ErrorResponse { Error = ex.Message }, statusCode: 401);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing token");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        });

        group.MapPost("/forgot-password", async (ForgotPasswordRequest request,
            IAuthService authService) =>
        {
            await authService.ForgotPasswordAsync(request);
            return Results.Ok(new { message = "If the email exists, a reset link has been sent." });
        });

        group.MapPost("/reset-password", async (ResetPasswordRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Auth");
            try
            {
                await authService.ResetPasswordAsync(request);
                return Results.Ok(new { status = "Password reset successfully" });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resetting password");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        });

        // Protected: current user info
        group.MapGet("/me", (HttpContext httpContext) =>
        {
            var ctx = httpContext.GetUserContext();
            return Results.Ok(new
            {
                userId = ctx.UserId,
                organizationId = ctx.OrganizationId,
                roleId = ctx.RoleId,
                memberId = ctx.MemberId,
                permissions = ctx.Permissions.ToList()
            });
        }).RequireAuthorization().RequirePermission("organization.read");
    }
}
