using Microsoft.EntityFrameworkCore;
using VoucherSystem.Api.Middleware;
using VoucherSystem.Contracts;
using VoucherSystem.Contracts.Promotions;
using VoucherSystem.Infrastructure;

namespace VoucherSystem.Api.Endpoints;

public static class MetricsEndpoints
{
    public static void MapMetricsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}");

        // GET /api/projects/{projectId}/summary
        group.MapGet("/summary", async (
            Guid projectId,
            VoucherSystemDbContext db,
            HttpContext ctx,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Metrics");
            try
            {
                var userCtx = ctx.GetUserContext();

                // Verify project exists and belongs to organization
                var projectExists = await db.Projects
                    .AnyAsync(p => p.Id == projectId && p.OrganizationId == userCtx.OrganizationId);

                if (!projectExists)
                    return Results.NotFound(new ErrorResponse { Error = "Project not found." });

                // Count active campaigns (BrandProfile = 1 per project, counts as a campaign)
                var activeCampaigns = await db.BrandProfiles
                    .LongCountAsync(b => b.ProjectId == projectId);

                // Total vouchers — future entity, currently always 0
                // Total validations — future entity, currently always 0
                // Total redemptions — future entity, currently always 0
                // Total failed — future entity, currently always 0

                return Results.Ok(new PromotionSummaryResponse
                {
                    TotalCampaigns = (int)activeCampaigns,
                    TotalVouchers = 0,
                    TotalValidations = 0,
                    TotalRedemptions = 0,
                    TotalFailed = 0,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting project summary for {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("usage.read");

        // GET /api/projects/{projectId}/usage
        group.MapGet("/usage", async (
            Guid projectId,
            VoucherSystemDbContext db,
            HttpContext ctx,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Metrics");
            try
            {
                var userCtx = ctx.GetUserContext();

                // Verify project exists and belongs to organization
                var project = await db.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == userCtx.OrganizationId);

                if (project is null)
                    return Results.NotFound(new ErrorResponse { Error = "Project not found." });

                var orgId = userCtx.OrganizationId;

                // Count active projects
                var activeProjects = await db.Projects
                    .LongCountAsync(p => p.OrganizationId == orgId && p.Status == "Active");

                // Count all projects
                var totalProjects = await db.Projects
                    .LongCountAsync(p => p.OrganizationId == orgId);

                // Get max projects from the organization's plan
                var maxProjects = 0;
                var plan = await (
                    from org in db.Organizations
                    join p in db.Plans on org.PlanId equals p.Id
                    where org.Id == orgId
                    select p
                ).FirstOrDefaultAsync();

                if (plan is not null)
                    maxProjects = plan.MaxProjects;

                return Results.Ok(new UsageResponse
                {
                    ActiveProjects = (int)activeProjects,
                    TotalProjects = (int)totalProjects,
                    MaxProjects = maxProjects,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting project usage for {ProjectId}", projectId);
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequireAuthorization().RequirePermission("usage.read");
    }
}
