using VoucherSystem.Contracts;
using VoucherSystem.Contracts.Vouchers;
using VoucherSystem.Application;
using VoucherSystem.Api.Middleware;

namespace VoucherSystem.Api.Endpoints;

public static class VoucherEndpoints
{
    public static void MapVoucherEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/vouchers");

        // List vouchers
        group.MapGet("/", async (Guid projectId, IVoucherService service, HttpContext ctx,
            int page = 1, int pageSize = 20, string? status = null) =>
        {
            var userCtx = ctx.GetUserContext();
            var result = await service.GetListAsync(userCtx.OrganizationId, projectId, page, pageSize, status);
            return Results.Ok(result);
        }).RequireAuthorization().RequirePermission("vouchers.read");

        // Get voucher by ID
        group.MapGet("/{id:guid}", async (Guid projectId, Guid id, IVoucherService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            var result = await service.GetByIdAsync(userCtx.OrganizationId, projectId, id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization().RequirePermission("vouchers.read");

        // Get voucher by code
        group.MapGet("/by-code/{code}", async (Guid projectId, string code, IVoucherService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            var result = await service.GetByCodeAsync(userCtx.OrganizationId, projectId, code);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization().RequirePermission("vouchers.read");

        // Create voucher
        group.MapPost("/", async (Guid projectId, CreateVoucherRequest request, IVoucherService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            try
            {
                var result = await service.CreateAsync(userCtx.OrganizationId, projectId, request, userCtx.UserId);
                return Results.Created($"/api/projects/{projectId}/vouchers/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
        }).RequireAuthorization().RequirePermission("vouchers.create");

        // Update voucher
        group.MapPatch("/{id:guid}", async (Guid projectId, Guid id, UpdateVoucherRequest request,
            IVoucherService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            var result = await service.UpdateAsync(userCtx.OrganizationId, projectId, id, request, userCtx.UserId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization().RequirePermission("vouchers.update");

        // Voucher actions (disable/enable)
        group.MapPost("/{id:guid}/actions/{action}", async (Guid projectId, Guid id, string action,
            VoucherActionRequest request, IVoucherService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            try
            {
                var result = await service.ExecuteActionAsync(userCtx.OrganizationId, projectId, id, action, request, userCtx.UserId);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new ErrorResponse { Error = ex.Message }, statusCode: 422);
            }
        }).RequireAuthorization().RequirePermission("vouchers.update");

        // Delete voucher
        group.MapDelete("/{id:guid}", async (Guid projectId, Guid id, IVoucherService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            var ok = await service.DeleteAsync(userCtx.OrganizationId, projectId, id);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization().RequirePermission("vouchers.delete");
    }

    // ─── Batch endpoints ───
    public static void MapVoucherBatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/voucher-batches");

        group.MapGet("/", async (Guid projectId, IVoucherService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            var result = await service.GetBatchesAsync(userCtx.OrganizationId, projectId);
            return Results.Ok(result);
        }).RequireAuthorization().RequirePermission("vouchers.read");

        group.MapPost("/", async (Guid projectId, CreateVoucherBatchRequest request, IVoucherService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            try
            {
                var result = await service.CreateBatchAsync(userCtx.OrganizationId, projectId, request, userCtx.UserId);
                return Results.Created($"/api/projects/{projectId}/voucher-batches/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
        }).RequireAuthorization().RequirePermission("vouchers.create");
    }

    // ─── Redemption endpoints ───
    public static void MapVoucherRedemptionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/redeem");

        group.MapPost("/", async (Guid projectId, RedeemVoucherRequest request, IVoucherService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            try
            {
                var result = await service.RedeemAsync(userCtx.OrganizationId, projectId, request, userCtx.UserId);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new ErrorResponse { Error = ex.Message }, statusCode: 422);
            }
        }).RequireAuthorization().RequirePermission("vouchers.update");
    }
}
