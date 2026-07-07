namespace VoucherSystem.Domain;

public class UsageQuota
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PlanId { get; set; }
    public long MonthlyApiCallsLimit { get; set; }
    public long MonthlyApiCallsUsed { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProjects { get; set; }
    public DateTimeOffset PeriodStartAt { get; set; }
    public DateTimeOffset PeriodEndAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
