namespace VoucherSystem.Contracts;

public class CreateOrganizationRequest
{
    public string OrganizationName { get; set; } = default!;
    public string ResponsibleName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
    public string Country { get; set; } = "BR";
    public string? DocumentNumber { get; set; }
    public bool AcceptedTerms { get; set; }
    public bool AcceptedPrivacyPolicy { get; set; }
}
