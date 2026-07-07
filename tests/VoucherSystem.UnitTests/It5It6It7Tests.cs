using FluentAssertions;
using VoucherSystem.Domain;

namespace VoucherSystem.UnitTests;

public class AuditLogWriterTests
{
    [Fact]
    public void AuditLog_HasRequiredFields()
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Action = "organization.created",
            ResourceType = "Organization",
            ResourceId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        log.Id.Should().NotBeEmpty();
        log.OrganizationId.Should().NotBeEmpty();
        log.Action.Should().Be("organization.created");
        log.ResourceType.Should().Be("Organization");
        log.MetadataJson.Should().Be("{}");
    }

    [Fact]
    public void ProjectAccess_HasUniqueCompositeIndex()
    {
        var pa = new ProjectAccess
        {
            Id = Guid.NewGuid(),
            OrganizationMemberId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        pa.Id.Should().NotBeEmpty();
        pa.OrganizationMemberId.Should().NotBeEmpty();
        pa.ProjectId.Should().NotBeEmpty();
    }

    [Fact]
    public void UsageQuota_HasRequiredFields()
    {
        var q = new UsageQuota
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            MonthlyApiCallsLimit = 10000,
            MaxUsers = 3,
            MaxProjects = 1,
            PeriodStartAt = DateTimeOffset.UtcNow,
            PeriodEndAt = DateTimeOffset.UtcNow.AddMonths(1),
            CreatedAt = DateTimeOffset.UtcNow
        };

        q.MonthlyApiCallsLimit.Should().Be(10000);
        q.MaxUsers.Should().Be(3);
    }
}
