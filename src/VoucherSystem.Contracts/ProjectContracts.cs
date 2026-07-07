namespace VoucherSystem.Contracts.Projects;

public class ProjectResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public string Environment { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public string TimeZone { get; set; } = default!;
    public string Locale { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string Status { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public bool IsInUse { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Environment { get; set; } = "Sandbox";
    public string Currency { get; set; } = "BRL";
    public string TimeZone { get; set; } = "America/Sao_Paulo";
    public string Locale { get; set; } = "pt-BR";
    public string Country { get; set; } = "BR";
}

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Currency { get; set; }
    public string? TimeZone { get; set; }
    public string? Locale { get; set; }
    public string? Country { get; set; }
    public string? Environment { get; set; }
}

public class ProjectSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Environment { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ProjectListResponse
{
    public List<ProjectSummaryResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
