using System.Text;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Api.Middleware;

namespace VoucherSystem.Api.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/audit-logs").RequireAuthorization();

        group.MapGet("/", async (HttpContext httpContext,
            IAuditLogReader reader,
            int? page, int? pageSize,
            Guid? projectId, string? action, string? resourceType) =>
        {
            var ctx = httpContext.GetUserContext();
            var skip = ((page ?? 1) - 1) * (pageSize ?? 20);
            var take = Math.Min(pageSize ?? 20, 100);

            var (items, total) = await reader.GetLogsAsync(
                ctx.OrganizationId, projectId, action, resourceType, skip, take);

            return Results.Ok(new PagedResponse<AuditLogResponse>
            {
                Items = items.Select(i => new AuditLogResponse
                {
                    Id = i.Id,
                    ActorUserId = i.ActorUserId,
                    Action = i.Action,
                    ResourceType = i.ResourceType,
                    ResourceId = i.ResourceId,
                    IpAddress = i.IpAddress,
                    UserAgent = i.UserAgent,
                    MetadataJson = i.MetadataJson,
                    CreatedAt = i.CreatedAt
                }).ToList(),
                TotalCount = total,
                Page = page ?? 1,
                PageSize = take
            });
        }).RequirePermission("audit.read");

        // CSV export endpoint
        group.MapGet("/export", async (HttpContext httpContext,
            IAuditLogReader reader,
            Guid? projectId, string? action, string? resourceType,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Audit");
            try
            {
                var ctx = httpContext.GetUserContext();

                // Export all matching logs (up to 10k rows)
                var (items, _) = await reader.GetLogsAsync(
                    ctx.OrganizationId, projectId, action, resourceType, 0, 10000);

                var sb = new StringBuilder();
                sb.AppendLine("timestamp,action,entityType,entityId,actorId");

                foreach (var item in items)
                {
                    var timestamp = item.CreatedAt.ToString("O");
                    var act = EscapeCsv(item.Action);
                    var entityType = EscapeCsv(item.ResourceType);
                    var entityId = EscapeCsv(item.ResourceId ?? "");
                    var actorId = item.ActorUserId?.ToString() ?? "";
                    sb.AppendLine($"{timestamp},{act},{entityType},{entityId},{actorId}");
                }

                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                return Results.File(bytes, "text/csv", $"audit-logs-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error exporting audit logs");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        }).RequirePermission("audit.export");
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
