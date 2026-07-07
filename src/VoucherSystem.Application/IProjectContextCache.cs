namespace VoucherSystem.Application;

/// <summary>
/// Cache for project context data used during request resolution.
/// Follows the same pattern as IPermissionCache but for per-project metadata.
/// </summary>
public interface IProjectContextCache
{
    /// <summary>Retrieves cached project metadata keyed by organization+project.</summary>
    Task<ProjectContextCacheEntry?> GetAsync(Guid organizationId, Guid projectId);

    /// <summary>Stores project metadata with TTL (default 30 min).</summary>
    Task SetAsync(Guid organizationId, Guid projectId, ProjectContextCacheEntry entry);

    /// <summary>Invalidates cached entry for a specific organization+project.</summary>
    Task InvalidateAsync(Guid organizationId, Guid projectId);
}

/// <summary>
/// Lightweight DTO stored in Redis representing the project metadata needed
/// for request processing by ProjectContextMiddleware.
/// </summary>
public class ProjectContextCacheEntry
{
    public Guid ProjectId { get; init; }
    public Guid OrganizationId { get; init; }
    public string Environment { get; init; } = "Production";
    public string Status { get; init; } = "Active";
    public string Currency { get; init; } = "BRL";
    public string TimeZone { get; init; } = "America/Sao_Paulo";
    public string Locale { get; init; } = "pt-BR";
    public string Country { get; init; } = "BR";
}
