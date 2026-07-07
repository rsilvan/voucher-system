using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Api.Middleware;

/// <summary>
/// Current project context resolved per-request for authenticated users.
/// Provides organization, project, environment, status, and regionalization info.
/// </summary>
public class CurrentProjectContext
{
    public Guid OrganizationId { get; init; }
    public Guid ProjectId { get; init; }
    public string Environment { get; init; } = "Production";
    public string Status { get; init; } = "Active";
    public string Currency { get; init; } = "BRL";
    public string TimeZone { get; init; } = "America/Sao_Paulo";
    public string Locale { get; init; } = "pt-BR";
    public string Country { get; init; } = "BR";
    public bool IsReadOnly => Status != "Active";
    public bool IsProduction => Environment == "Production";
}

/// <summary>
/// Middleware that resolves and validates the current project context.
/// Supports X-Project-Id header for human users; API keys use their fixed project.
/// Uses Redis cache for project metadata with fallback to database.
/// Blocks write operations on non-active projects (Disabled, Archived).
/// </summary>
public class ProjectContextMiddleware
{
    private readonly RequestDelegate _next;

    public ProjectContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context,
        IProjectContextCache projectCache,
        IProjectRepository projectRepo)
    {
        // Only process authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var orgId = (Guid)(context.Items["OrganizationId"] ?? Guid.Empty);
            var isApiKeyAuth = context.Items["AuthMethod"]?.ToString() == "ApiKey";

            if (orgId != Guid.Empty)
            {
                // For API keys, project is fixed — cannot override via header
                Guid projectId;
                if (isApiKeyAuth)
                {
                    projectId = (Guid)(context.Items["ProjectId"] ?? Guid.Empty);
                }
                else
                {
                    // Human users can set X-Project-Id header
                    var headerProjectId = context.Request.Headers["X-Project-Id"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(headerProjectId) && Guid.TryParse(headerProjectId, out var parsed))
                    {
                        projectId = parsed;
                    }
                    else
                    {
                        projectId = (Guid)(context.Items["ProjectId"] ?? Guid.Empty);
                    }
                }

                // Store resolved project context
                context.Items["CurrentProjectId"] = projectId;

                // ─── Resolve project metadata (cache-first, DB fallback) ───
                string projectEnvironment;
                string projectStatus;
                string projectCurrency;
                string projectTimeZone;
                string projectLocale;
                string projectCountry;

                // 1) Try Redis cache first
                var cached = await projectCache.GetAsync(orgId, projectId);
                if (cached is not null)
                {
                    projectEnvironment = cached.Environment;
                    projectStatus = cached.Status;
                    projectCurrency = cached.Currency;
                    projectTimeZone = cached.TimeZone;
                    projectLocale = cached.Locale;
                    projectCountry = cached.Country;
                }
                else
                {
                    // 2) Fallback to claims/headers (may already be set by UserContextMiddleware)
                    projectEnvironment = context.Items["ProjectEnvironment"]?.ToString() ?? "Production";
                    projectStatus = context.Items["ProjectStatus"]?.ToString() ?? "Active";
                    projectCurrency = context.Items["ProjectCurrency"]?.ToString() ?? "BRL";
                    projectTimeZone = context.Items["ProjectTimeZone"]?.ToString() ?? "America/Sao_Paulo";
                    projectLocale = context.Items["ProjectLocale"]?.ToString() ?? "pt-BR";
                    projectCountry = context.Items["ProjectCountry"]?.ToString() ?? "BR";

                    // 4) Populate cache for next request — only if we loaded from DB
                    var project = projectId != Guid.Empty ? await projectRepo.GetByIdAsync(projectId, orgId) : null;
                    if (project is not null)
                    {
                        projectEnvironment = project.Environment;
                        projectStatus = project.Status;
                        projectCurrency = project.Currency;
                        projectTimeZone = project.TimeZone;
                        projectLocale = project.Locale;
                        projectCountry = project.Country;

                        await projectCache.SetAsync(orgId, projectId, new ProjectContextCacheEntry
                        {
                            OrganizationId = orgId,
                            ProjectId = projectId,
                            Environment = project.Environment,
                            Status = project.Status,
                            Currency = project.Currency,
                            TimeZone = project.TimeZone,
                            Locale = project.Locale,
                            Country = project.Country,
                        });
                    }
                }

                // ─── Write-blocking for non-active projects ───
                if (projectStatus != "Active")
                {
                    var method = context.Request.Method;
                    if (method is "POST" or "PUT" or "PATCH" or "DELETE")
                    {
                        // Allow admin operations for management endpoints
                        var path = context.Request.Path.Value ?? "";
                        if (!path.Contains("/api/projects/") || (!path.EndsWith("/enable") && !path.EndsWith("/restore") && !path.EndsWith("/archive")))
                        {
                            // Only block if not a project management operation
                            if (projectStatus == "Disabled")
                            {
                                context.Response.StatusCode = 423; // Locked
                                await context.Response.WriteAsJsonAsync(new { error = "Project is disabled. Write operations are blocked." });
                                return;
                            }
                            if (projectStatus == "Archived")
                            {
                                context.Response.StatusCode = 423;
                                await context.Response.WriteAsJsonAsync(new { error = "Project is archived. Write operations are blocked." });
                                return;
                            }
                        }
                    }
                }

                context.Items["CurrentProjectContext"] = new CurrentProjectContext
                {
                    OrganizationId = orgId,
                    ProjectId = projectId,
                    Environment = projectEnvironment,
                    Status = projectStatus,
                    Currency = projectCurrency,
                    TimeZone = projectTimeZone,
                    Locale = projectLocale,
                    Country = projectCountry,
                };
            }
        }

        await _next(context);
    }
}

public static class ProjectContextExtensions
{
    public static CurrentProjectContext GetCurrentProjectContext(this HttpContext context)
        => (CurrentProjectContext)(context.Items["CurrentProjectContext"] ?? throw new UnauthorizedAccessException("No project context available."));

    public static Guid GetCurrentProjectId(this HttpContext context)
        => (Guid)(context.Items["CurrentProjectId"] ?? Guid.Empty);
}
