namespace VoucherSystem.Contracts;

public class CreateOrganizationResponse
{
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = default!;
    public string Status { get; set; } = default!;
}
