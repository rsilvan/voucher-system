namespace VoucherSystem.Domain;

public class Plan
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal MonthlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProjects { get; set; }
    public int MaxActiveCampaigns { get; set; }
    public long MonthlyApiCalls { get; set; }
    public bool AllowsCustomRoles { get; set; }
    public bool AllowsAuditExport { get; set; }
    public bool AllowsSso { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
