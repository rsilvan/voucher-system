namespace VoucherSystem.Contracts.Brands;

public class BrandAddressResponse
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class BrandResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? TermsUrl { get; set; }
    public string? PrivacyUrl { get; set; }
    public string? SupportEmail { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public BrandAddressResponse Address { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class CreateBrandRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? TermsUrl { get; set; }
    public string? PrivacyUrl { get; set; }
    public string? SupportEmail { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public BrandAddressRequest? Address { get; set; }
}

public class UpdateBrandRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? TermsUrl { get; set; }
    public string? PrivacyUrl { get; set; }
    public string? SupportEmail { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public BrandAddressRequest? Address { get; set; }
}

public class BrandAddressRequest
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
}
