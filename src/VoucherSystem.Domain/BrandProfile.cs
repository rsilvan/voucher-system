namespace VoucherSystem.Domain;

public class BrandProfile
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
    public BrandAddress Address { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
