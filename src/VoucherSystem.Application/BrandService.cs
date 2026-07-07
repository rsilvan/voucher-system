using System.Globalization;
using System.Text.RegularExpressions;
using VoucherSystem.Contracts.Brands;
using VoucherSystem.Domain;

namespace VoucherSystem.Application;

public partial class BrandService : IBrandService
{
    private readonly IBrandRepository _repo;
    private readonly IProjectRepository _projectRepo;
    private readonly IAuditLogWriter _audit;

    public BrandService(IBrandRepository repo, IProjectRepository projectRepo, IAuditLogWriter audit)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _audit = audit;
    }

    public async Task<BrandResponse?> GetByProjectAsync(Guid projectId, Guid organizationId)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, organizationId);
        if (project is null) return null;

        var brand = await _repo.GetByProjectAsync(projectId);
        return brand is null ? null : MapToResponse(brand);
    }

    public async Task<BrandResponse> CreateAsync(Guid projectId, Guid organizationId, CreateBrandRequest request)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, organizationId)
            ?? throw new ArgumentException("Project not found.");

        ValidateName(request.Name);

        if (request.WebsiteUrl is not null)
            ValidateUrl(request.WebsiteUrl, nameof(request.WebsiteUrl));

        if (request.TermsUrl is not null)
            ValidateUrl(request.TermsUrl, nameof(request.TermsUrl));

        if (request.PrivacyUrl is not null)
            ValidateUrl(request.PrivacyUrl, nameof(request.PrivacyUrl));

        if (request.LogoUrl is not null)
            ValidateUrl(request.LogoUrl, nameof(request.LogoUrl));

        if (request.SupportEmail is not null)
            ValidateEmail(request.SupportEmail);

        if (request.PrimaryColor is not null)
            ValidateColor(request.PrimaryColor, nameof(request.PrimaryColor));

        if (request.SecondaryColor is not null)
            ValidateColor(request.SecondaryColor, nameof(request.SecondaryColor));

        if (request.Description is not null && request.Description.Length > 2000)
            throw new ArgumentException("Description must be at most 2000 characters.");

        // Ensure unique brand per project
        var existing = await _repo.GetByProjectAsync(projectId);
        if (existing is not null)
            throw new InvalidOperationException("A brand profile already exists for this project.");

        var now = DateTimeOffset.UtcNow;
        var brand = new BrandProfile
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            WebsiteUrl = SanitizeUrl(request.WebsiteUrl),
            TermsUrl = SanitizeUrl(request.TermsUrl),
            PrivacyUrl = SanitizeUrl(request.PrivacyUrl),
            SupportEmail = request.SupportEmail?.Trim().ToLowerInvariant(),
            LogoUrl = SanitizeUrl(request.LogoUrl),
            PrimaryColor = request.PrimaryColor,
            SecondaryColor = request.SecondaryColor,
            Address = MapAddress(request.Address),
            CreatedAt = now,
        };

        await _repo.AddAsync(brand);

        _audit.Write(organizationId, projectId, null, "brand.created", "BrandProfile", brand.Id.ToString());

        return MapToResponse(brand);
    }

    public async Task<BrandResponse?> UpdateAsync(Guid projectId, Guid organizationId, UpdateBrandRequest request)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, organizationId);
        if (project is null) return null;

        var brand = await _repo.GetByProjectAsync(projectId);
        if (brand is null) return null;

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Brand name cannot be empty.");
            brand.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            if (request.Description.Length > 2000)
                throw new ArgumentException("Description must be at most 2000 characters.");
            brand.Description = request.Description.Trim();
        }

        if (request.WebsiteUrl is not null)
        {
            ValidateUrl(request.WebsiteUrl, nameof(request.WebsiteUrl));
            brand.WebsiteUrl = SanitizeUrl(request.WebsiteUrl);
        }

        if (request.TermsUrl is not null)
        {
            ValidateUrl(request.TermsUrl, nameof(request.TermsUrl));
            brand.TermsUrl = SanitizeUrl(request.TermsUrl);
        }

        if (request.PrivacyUrl is not null)
        {
            ValidateUrl(request.PrivacyUrl, nameof(request.PrivacyUrl));
            brand.PrivacyUrl = SanitizeUrl(request.PrivacyUrl);
        }

        if (request.LogoUrl is not null)
        {
            ValidateUrl(request.LogoUrl, nameof(request.LogoUrl));
            brand.LogoUrl = SanitizeUrl(request.LogoUrl);
        }

        if (request.SupportEmail is not null)
        {
            ValidateEmail(request.SupportEmail);
            brand.SupportEmail = request.SupportEmail.Trim().ToLowerInvariant();
        }

        if (request.PrimaryColor is not null)
        {
            ValidateColor(request.PrimaryColor, nameof(request.PrimaryColor));
            brand.PrimaryColor = request.PrimaryColor;
        }

        if (request.SecondaryColor is not null)
        {
            ValidateColor(request.SecondaryColor, nameof(request.SecondaryColor));
            brand.SecondaryColor = request.SecondaryColor;
        }

        if (request.Address is not null)
            ApplyAddressUpdate(brand.Address, request.Address);

        brand.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(brand);

        _audit.Write(organizationId, projectId, null, "brand.updated", "BrandProfile", brand.Id.ToString());

        return MapToResponse(brand);
    }

    public async Task<bool> DeleteAsync(Guid projectId, Guid organizationId)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, organizationId);
        if (project is null) return false;

        var brand = await _repo.GetByProjectAsync(projectId);
        if (brand is null) return false;

        await _repo.DeleteAsync(brand);

        _audit.Write(organizationId, projectId, null, "brand.deleted", "BrandProfile", brand.Id.ToString());

        return true;
    }

    // --- Validation helpers ---

    public static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Brand name is required.");
        if (name.Trim().Length > 200)
            throw new ArgumentException("Brand name must be at most 200 characters.");
    }

    public static void ValidateUrl(string url, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            throw new ArgumentException($"'{fieldName}' must be a valid absolute URL.");

        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"'{fieldName}' must use HTTPS.");

        // Reject localhost
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"'{fieldName}' cannot point to localhost.");

        // Reject IP addresses (both IPv4 and IPv6)
        if (uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6)
            throw new ArgumentException($"'{fieldName}' cannot point to an IP address.");
    }

    public static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        email = email.Trim();

        if (email.Length > 254)
            throw new ArgumentException("Support email must be at most 254 characters.");

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address != email)
                throw new ArgumentException("Support email is not valid.");
        }
        catch
        {
            throw new ArgumentException("Support email is not valid.");
        }
    }

    public static void ValidateColor(string color, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(color))
            return;

        if (!HexColorRegex().IsMatch(color.Trim()))
            throw new ArgumentException($"'{fieldName}' must be a valid hex color (e.g. #FF5733).");
    }

    public static string? SanitizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        url = url.Trim();

        // Remove trailing slash for consistency
        if (url.EndsWith('/'))
            url = url.TrimEnd('/');

        return url;
    }

    private static void ApplyAddressUpdate(BrandAddress address, BrandAddressRequest request)
    {
        if (request.Street is not null)
            address.Street = request.Street.Trim();
        if (request.City is not null)
            address.City = request.City.Trim();
        if (request.State is not null)
            address.State = request.State.Trim();
        if (request.ZipCode is not null)
            address.ZipCode = request.ZipCode.Trim();
        if (request.Country is not null)
            address.Country = request.Country.Trim().ToUpperInvariant();
    }

    private static BrandAddress MapAddress(BrandAddressRequest? request)
    {
        if (request is null)
            return new BrandAddress();

        return new BrandAddress
        {
            Street = request.Street?.Trim() ?? string.Empty,
            City = request.City?.Trim() ?? string.Empty,
            State = request.State?.Trim() ?? string.Empty,
            ZipCode = request.ZipCode?.Trim() ?? string.Empty,
            Country = request.Country?.Trim().ToUpperInvariant() ?? string.Empty,
        };
    }

    private static BrandResponse MapToResponse(BrandProfile b) => new()
    {
        Id = b.Id,
        ProjectId = b.ProjectId,
        Name = b.Name,
        Description = b.Description,
        WebsiteUrl = b.WebsiteUrl,
        TermsUrl = b.TermsUrl,
        PrivacyUrl = b.PrivacyUrl,
        SupportEmail = b.SupportEmail,
        LogoUrl = b.LogoUrl,
        PrimaryColor = b.PrimaryColor,
        SecondaryColor = b.SecondaryColor,
        Address = new BrandAddressResponse
        {
            Street = b.Address.Street,
            City = b.Address.City,
            State = b.Address.State,
            ZipCode = b.Address.ZipCode,
            Country = b.Address.Country,
        },
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt,
    };

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();
}
