using VoucherSystem.Contracts;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepo;
    private readonly IEmailService _emailService;

    public OrganizationService(IOrganizationRepository organizationRepo, IEmailService emailService)
    {
        _organizationRepo = organizationRepo;
        _emailService = emailService;
    }

    public async Task<CreateOrganizationResponse> CreateOrganizationAsync(CreateOrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationName))
            throw new ArgumentException("Organization name is required.");
        if (string.IsNullOrWhiteSpace(request.ResponsibleName))
            throw new ArgumentException("Responsible name is required.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");
        if (request.Password != request.ConfirmPassword)
            throw new ArgumentException("Passwords do not match.");
        if (!request.AcceptedTerms)
            throw new ArgumentException("You must accept the terms of use.");
        if (!request.AcceptedPrivacyPolicy)
            throw new ArgumentException("You must accept the privacy policy.");
        if (request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _organizationRepo.EmailExistsAsync(normalizedEmail);
        if (emailExists)
            throw new ArgumentException("Email is already registered.");

        var slug = GenerateSlug(request.OrganizationName);

        var plan = await _organizationRepo.GetTrialPlanAsync();
        if (plan == null)
            throw new InvalidOperationException("No trial plan configured.");

        var now = DateTimeOffset.UtcNow;

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.OrganizationName,
            LegalName = request.OrganizationName,
            DocumentNumber = request.DocumentNumber,
            Slug = slug,
            Country = request.Country,
            Status = "Active",
            PlanId = plan.Id,
            CreatedAt = now
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.ResponsibleName,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            EmailVerified = false,
            Status = "Active",
            CreatedAt = now
        };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "Projeto Principal",
            Slug = "projeto-principal",
            Environment = "Production",
            Currency = "BRL",
            TimeZone = "America/Sao_Paulo",
            Status = "Active",
            CreatedAt = now
        };

        var ownerRole = await _organizationRepo.GetSystemRoleByKeyAsync("OrganizationOwner");
        if (ownerRole == null)
            throw new InvalidOperationException("OrganizationOwner role not found.");

        var member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            UserId = user.Id,
            RoleId = ownerRole.Id,
            Status = "Active",
            CreatedAt = now
        };

        await _organizationRepo.SaveOrganizationAsync(organization, user, project, member);

        await _emailService.SendWelcomeEmailAsync(user.Email, user.Name, organization.Name);

        return new CreateOrganizationResponse
        {
            OrganizationId = organization.Id,
            ProjectId = project.Id,
            UserId = user.Id,
            Role = ownerRole.Key,
            Status = "Created"
        };
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "e")
            .Replace("--", "-")
            .Trim('-');

        var sanitized = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N")[..8] : sanitized;
    }
}
