using Microsoft.EntityFrameworkCore;
using VoucherSystem.Api.Middleware;
using VoucherSystem.Contracts;
using VoucherSystem.Domain;
using VoucherSystem.Infrastructure;

namespace VoucherSystem.Api.Endpoints;

public class ProjectSummaryResponse
{
    public int TotalCampaigns { get; set; }
    public int TotalVouchers { get; set; }
    public int TotalRedemptions { get; set; }
    public int TotalActivePromotions { get; set; }
}

public class UsageResponse
{
    public int ActiveProjects { get; set; }
    public int TotalProjects { get; set; }
    public int MaxProjects { get; set; }
}

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

                var projectExists = await db.Projects
                    .AnyAsync(p => p.Id == projectId && p.OrganizationId == userCtx.OrganizationId);

                if (!projectExists)
                    return Results.NotFound(new ErrorResponse { Error = "Project not found." });

                var activeCampaigns = await db.Campaigns
                    .LongCountAsync(c => c.ProjectId == projectId && c.Status == "Active");

                var totalVouchers = await db.Vouchers
                    .LongCountAsync(v => v.ProjectId == projectId && v.Status != "PendingDeletion");

                var totalRedemptions = await db.Set<VoucherRedemption>()
                    .LongCountAsync(r => r.ProjectId == projectId);

                var activePromotions = await db.Promotions
                    .LongCountAsync(p => p.ProjectId == projectId && p.Status == "Active");

                return Results.Ok(new ProjectSummaryResponse
                {
                    TotalCampaigns = (int)activeCampaigns,
                    TotalVouchers = (int)totalVouchers,
                    TotalRedemptions = (int)totalRedemptions,
                    TotalActivePromotions = (int)activePromotions,
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

                var project = await db.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == userCtx.OrganizationId);

                if (project is null)
                    return Results.NotFound(new ErrorResponse { Error = "Project not found." });

                var orgId = userCtx.OrganizationId;

                var activeProjects = await db.Projects
                    .LongCountAsync(p => p.OrganizationId == orgId && p.Status == "Active");

                var totalProjects = await db.Projects
                    .LongCountAsync(p => p.OrganizationId == orgId);

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
