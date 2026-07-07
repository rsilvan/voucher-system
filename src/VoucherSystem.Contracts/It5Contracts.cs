namespace VoucherSystem.Contracts;

// Roles
public class CreateRoleRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public List<string> PermissionKeys { get; set; } = new();
}

public class UpdateRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? PermissionKeys { get; set; }
}

public class RoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public string Scope { get; set; } = default!;
    public List<string> Permissions { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}

public class PermissionResponse
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Resource { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string? Description { get; set; }
}

// Project Access
public class UpdateProjectAccessRequest
{
    public List<Guid> ProjectIds { get; set; } = new();
    public Guid RoleId { get; set; }
}

// Audit
public class AuditLogResponse
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = default!;
    public string ResourceType { get; set; } = default!;
    public string? ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}

public class AuditLogFilterRequest
{
    public string? ActorUserId { get; set; }
    public string? Action { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public Guid? ProjectId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
