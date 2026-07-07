using VoucherSystem.Contracts.Projects;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repo;
    private readonly IAuditLogWriter _audit;

    public ProjectService(IProjectRepository repo, IAuditLogWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<ProjectResponse?> GetByIdAsync(Guid projectId, Guid organizationId)
    {
        var project = await _repo.GetByIdAsync(projectId, organizationId);
        return project is null ? null : MapToResponse(project);
    }

    public async Task<ProjectListResponse> GetByOrganizationAsync(Guid organizationId, Guid? memberId)
    {
        List<Project> projects;
        if (memberId.HasValue)
            projects = await _repo.GetByMemberAsync(organizationId, memberId.Value);
        else
            projects = await _repo.GetByOrganizationAsync(organizationId);

        return new ProjectListResponse
        {
            Items = projects.Select(p => new ProjectSummaryResponse
            {
                Id = p.Id,
                Name = p.Name,
                Environment = p.Environment,
                Status = p.Status,
                Currency = p.Currency,
                IsPrimary = p.IsPrimary,
                CreatedAt = p.CreatedAt,
            }).ToList(),
            TotalCount = projects.Count,
        };
    }

    public async Task<ProjectResponse> CreateAsync(Guid organizationId, CreateProjectRequest request)
    {
        ValidateEnvironment(request.Environment);
        ValidateCurrency(request.Currency);
        ValidateTimeZone(request.TimeZone);
        ValidateLocale(request.Locale);
        ValidateCountry(request.Country);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Project name is required.");

        var slug = GenerateSlug(request.Name);

        if (await _repo.SlugExistsAsync(organizationId, slug))
            throw new ArgumentException("A project with this name already exists.");

        var activeCount = await _repo.GetActiveCountAsync(organizationId);
        if (activeCount >= 1) // Quota check — MVP limit
            throw new InvalidOperationException("Project quota exceeded (max 1 active project in MVP).");

        var now = DateTimeOffset.UtcNow;
        var isFirst = activeCount == 0;

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim() ?? string.Empty,
            Environment = request.Environment,
            Currency = request.Currency,
            TimeZone = request.TimeZone,
            Locale = request.Locale,
            Country = request.Country,
            Status = nameof(ProjectStatus.Active),
            IsPrimary = isFirst,
            CreatedAt = now,
        };

        await _repo.AddAsync(project);

        _audit.Write(organizationId, project.Id, null, "project.created", "Project", project.Id.ToString());

        return MapToResponse(project);
    }

    public async Task<ProjectResponse?> UpdateAsync(Guid projectId, Guid organizationId, UpdateProjectRequest request)
    {
        var project = await _repo.GetByIdAsync(projectId, organizationId);
        if (project is null) return null;

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Project name cannot be empty.");
            project.Name = request.Name.Trim();
        }
        if (request.Description is not null)
            project.Description = request.Description.Trim();
        if (request.Currency is not null)
        {
            ValidateCurrency(request.Currency);
            if (!project.CanChangeCurrency())
                throw new InvalidOperationException("PROJECT_CURRENCY_IMMUTABLE");
            project.Currency = request.Currency;
        }
        if (request.TimeZone is not null)
        {
            ValidateTimeZone(request.TimeZone);
            project.TimeZone = request.TimeZone;
        }
        if (request.Locale is not null)
        {
            ValidateLocale(request.Locale);
            project.Locale = request.Locale;
        }
        if (request.Country is not null)
        {
            ValidateCountry(request.Country);
            project.Country = request.Country;
        }
        if (request.Environment is not null)
        {
            ValidateEnvironment(request.Environment);
            if (!project.CanChangeEnvironment())
                throw new InvalidOperationException("PROJECT_ENVIRONMENT_IMMUTABLE");
            project.Environment = request.Environment;
        }

        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(project);

        _audit.Write(organizationId, project.Id, null, "project.updated", "Project", project.Id.ToString());

        return MapToResponse(project);
    }

    public async Task<bool> DisableAsync(Guid projectId, Guid organizationId)
    {
        var project = await _repo.GetByIdAsync(projectId, organizationId);
        if (project is null) return false;

        if (project.Status == nameof(ProjectStatus.Disabled))
            return true;

        if (project.Status != nameof(ProjectStatus.Active))
            throw new InvalidOperationException($"Cannot disable a project with status '{project.Status}'.");

        if (project.IsPrimary)
            throw new InvalidOperationException("Cannot disable the primary project. Set another project as primary first.");

        // Check last active project
        var activeCount = await _repo.GetActiveCountAsync(organizationId);
        if (activeCount <= 1)
            throw new InvalidOperationException("LAST_ACTIVE_PROJECT_REQUIRED");

        project.Status = nameof(ProjectStatus.Disabled);
        project.DisabledAt = DateTimeOffset.UtcNow;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(project);

        _audit.Write(organizationId, project.Id, null, "project.disabled", "Project", project.Id.ToString());
        return true;
    }

    public async Task<bool> EnableAsync(Guid projectId, Guid organizationId)
    {
        var project = await _repo.GetByIdAsync(projectId, organizationId);
        if (project is null) return false;

        if (project.Status != nameof(ProjectStatus.Disabled))
            throw new InvalidOperationException($"Cannot enable a project with status '{project.Status}'.");

        project.Status = nameof(ProjectStatus.Active);
        project.DisabledAt = null;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(project);

        _audit.Write(organizationId, project.Id, null, "project.enabled", "Project", project.Id.ToString());
        return true;
    }

    public async Task<bool> ArchiveAsync(Guid projectId, Guid organizationId)
    {
        var project = await _repo.GetByIdAsync(projectId, organizationId);
        if (project is null) return false;

        if (project.Status == nameof(ProjectStatus.Archived))
            return true;

        if (project.Status != nameof(ProjectStatus.Disabled) && project.Status != nameof(ProjectStatus.Active))
            throw new InvalidOperationException($"Cannot archive a project with status '{project.Status}'.");

        if (project.IsPrimary)
        {
            var others = (await _repo.GetByOrganizationAsync(organizationId))
                .Where(p => p.Id != projectId && p.Status == nameof(ProjectStatus.Active)).ToList();
            if (others.Count == 0)
                throw new InvalidOperationException("LAST_ACTIVE_PROJECT_REQUIRED");
        }

        project.Status = nameof(ProjectStatus.Archived);
        project.ArchivedAt = DateTimeOffset.UtcNow;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(project);

        _audit.Write(organizationId, project.Id, null, "project.archived", "Project", project.Id.ToString());
        return true;
    }

    public async Task<bool> RestoreAsync(Guid projectId, Guid organizationId)
    {
        var project = await _repo.GetByIdAsync(projectId, organizationId);
        if (project is null) return false;

        if (project.Status != nameof(ProjectStatus.Archived))
            throw new InvalidOperationException($"Cannot restore a project with status '{project.Status}'.");

        project.Status = nameof(ProjectStatus.Active);
        project.ArchivedAt = null;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(project);

        _audit.Write(organizationId, project.Id, null, "project.restored", "Project", project.Id.ToString());
        return true;
    }

    public async Task<bool> MakePrimaryAsync(Guid projectId, Guid organizationId)
    {
        var project = await _repo.GetByIdAsync(projectId, organizationId);
        if (project is null) return false;

        if (project.Status != nameof(ProjectStatus.Active))
            throw new InvalidOperationException("Only active projects can be the primary project.");

        var current = await _repo.GetPrimaryAsync(organizationId);
        if (current is not null && current.Id == projectId)
            return true;

        if (current is not null)
        {
            current.IsPrimary = false;
            current.UpdatedAt = DateTimeOffset.UtcNow;
            await _repo.UpdateAsync(current);
        }

        project.IsPrimary = true;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(project);

        _audit.Write(organizationId, project.Id, null, "project.make_primary", "Project", project.Id.ToString());
        return true;
    }

    public async Task<bool> DeleteAsync(Guid projectId, Guid organizationId)
    {
        var project = await _repo.GetByIdAsync(projectId, organizationId);
        if (project is null) return false;

        if (project.IsPrimary)
            throw new InvalidOperationException("Cannot delete the primary project.");

        // Only allow physical deletion of empty projects
        if (await _repo.HasResourcesAsync(projectId))
            throw new InvalidOperationException("Cannot delete a project that has resources. Archive it instead.");

        project.Status = nameof(ProjectStatus.PendingDeletion);
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(project);

        _audit.Write(organizationId, project.Id, null, "project.deleted", "Project", project.Id.ToString());
        return true;
    }

    private static ProjectResponse MapToResponse(Project p) => new()
    {
        Id = p.Id,
        OrganizationId = p.OrganizationId,
        Name = p.Name,
        Slug = p.Slug,
        Description = p.Description,
        Environment = p.Environment,
        Currency = p.Currency,
        TimeZone = p.TimeZone,
        Locale = p.Locale,
        Country = p.Country,
        Status = p.Status,
        IsPrimary = p.IsPrimary,
        IsInUse = p.IsInUse,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        ArchivedAt = p.ArchivedAt,
        DisabledAt = p.DisabledAt,
    };

    internal static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "e")
            .Replace("--", "-")
            .Trim('-');
        var sanitized = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N")[..8] : sanitized;
    }

    internal static void ValidateEnvironment(string env)
    {
        var valid = new[] { "Sandbox", "Development", "Staging", "Production" };
        if (!valid.Contains(env))
            throw new ArgumentException($"Invalid environment '{env}'. Valid values: {string.Join(", ", valid)}.");
    }

    internal static void ValidateCurrency(string currency)
    {
        if (currency.Length != 3 || currency.Any(c => !char.IsUpper(c)))
            throw new ArgumentException($"Invalid currency '{currency}'. Must be ISO 4217 (3 uppercase letters).");
    }

    internal static void ValidateTimeZone(string tz)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(tz);
        }
        catch
        {
            throw new ArgumentException($"Invalid time zone '{tz}'. Must be a valid IANA time zone ID.");
        }
    }

    internal static void ValidateLocale(string locale)
    {
        if (locale.Length < 2 || locale.Length > 10)
            throw new ArgumentException($"Invalid locale '{locale}'. Must follow BCP 47 (e.g. pt-BR, en-US).");
    }

    internal static void ValidateCountry(string country)
    {
        if (country.Length != 2 || country.Any(c => !char.IsUpper(c)))
            throw new ArgumentException($"Invalid country '{country}'. Must be ISO 3166-1 alpha-2 (2 uppercase letters).");
    }
}
