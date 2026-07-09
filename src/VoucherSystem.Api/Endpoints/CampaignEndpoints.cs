using VoucherSystem.Contracts;
using VoucherSystem.Contracts.Campaigns;
using VoucherSystem.Application;
using VoucherSystem.Api.Middleware;
using System.Diagnostics;

namespace VoucherSystem.Api.Endpoints;

public static class CampaignEndpoints
{
    public static void MapCampaignEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/campaigns");

        // List campaigns
        group.MapGet("/", async (Guid projectId, ICampaignService service, HttpContext ctx,
            int page = 1, int pageSize = 20, string? status = null) =>
        {
            var userCtx = ctx.GetUserContext();
            var result = await service.GetListAsync(userCtx.OrganizationId, projectId, page, pageSize, status);
            return Results.Ok(result);
        }).RequireAuthorization().RequirePermission("campaigns.read");

        // Get campaign by ID
        group.MapGet("/{id:guid}", async (Guid projectId, Guid id, ICampaignService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            var result = await service.GetByIdAsync(userCtx.OrganizationId, projectId, id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization().RequirePermission("campaigns.read");

        // Create campaign
        group.MapPost("/", async (Guid projectId, CreateCampaignRequest request, ICampaignService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            try
            {
                var result = await service.CreateAsync(userCtx.OrganizationId, projectId, request, userCtx.UserId);
                return Results.Created($"/api/projects/{projectId}/campaigns/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
        }).RequireAuthorization().RequirePermission("campaigns.create");

        // Update campaign
        group.MapPatch("/{id:guid}", async (Guid projectId, Guid id, UpdateCampaignRequest request,
            ICampaignService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            try
            {
                var result = await service.UpdateAsync(userCtx.OrganizationId, projectId, id, request, userCtx.UserId);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
        }).RequireAuthorization().RequirePermission("campaigns.update");

        // Campaign actions (activate, pause, end, archive, publish)
        group.MapPost("/{id:guid}/actions/{action}", async (Guid projectId, Guid id, string action,
            CampaignActionRequest request, ICampaignService service, HttpContext ctx) =>
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
            catch (CampaignStateMachineException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message, Detail = "Invalid state transition" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new ErrorResponse { Error = ex.Message }, statusCode: 422);
            }
            catch (Exception ex)
            {
                var requestId = Activity.Current?.Id ?? ctx.TraceIdentifier;
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "An unexpected error occurred",
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    extensions: new Dictionary<string, object?>
                    {
                        ["requestId"] = requestId
                    });
            }
        }).RequireAuthorization().RequirePermission("campaigns.update");

        // Delete (archive) campaign
        group.MapDelete("/{id:guid}", async (Guid projectId, Guid id, ICampaignService service, HttpContext ctx) =>
        {
            var userCtx = ctx.GetUserContext();
            var ok = await service.DeleteAsync(userCtx.OrganizationId, projectId, id);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization().RequirePermission("campaigns.delete");
    }
}
