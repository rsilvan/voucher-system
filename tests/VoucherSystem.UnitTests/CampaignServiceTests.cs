using FluentAssertions;
using Moq;
using VoucherSystem.Application;
using VoucherSystem.Contracts.Campaigns;
using VoucherSystem.Domain;

namespace VoucherSystem.UnitTests;

public class CampaignServiceTests
{
    private readonly Mock<ICampaignRepository> _repoMock = new();
    private readonly Mock<IAuditLogWriter> _auditMock = new();
    private readonly CampaignService _service;

    public CampaignServiceTests()
    {
        _service = new CampaignService(_repoMock.Object, _auditMock.Object);
    }

    // ========================================================================
    // ExecuteActionAsync — publish
    // ========================================================================

    [Fact]
    public async Task Publish_DraftCampaign_TransitionsToScheduled()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var campaign = new Campaign
        {
            Id = campaignId,
            OrganizationId = orgId,
            ProjectId = projectId,
            Name = "Test Campaign",
            Type = "Coupon",
            Status = "Draft",
            CreatedBy = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdAsync(orgId, projectId, campaignId))
            .ReturnsAsync(campaign);

        // Act
        var result = await _service.ExecuteActionAsync(orgId, projectId, campaignId,
            "publish", new CampaignActionRequest(), userId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Scheduled");
        result.Events.Should().Contain("campaign.publish");
        campaign.Status.Should().Be("Scheduled");

        _repoMock.Verify(r => r.UpdateAsync(campaign), Times.Once);
        _repoMock.Verify(r => r.AddVersionAsync(It.Is<CampaignVersion>(
            v => v.CampaignId == campaignId && v.Status == "Published")), Times.Once);
        _auditMock.Verify(a => a.Write(orgId, projectId, userId,
            "campaign.publish", "Campaign", campaignId.ToString()), Times.Once);
    }

    [Fact]
    public async Task Publish_NonDraftCampaign_ThrowsCampaignStateMachineException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var campaign = new Campaign
        {
            Id = campaignId,
            OrganizationId = orgId,
            ProjectId = projectId,
            Name = "Active Campaign",
            Type = "Coupon",
            Status = "Active", // NOT Draft
            CreatedBy = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdAsync(orgId, projectId, campaignId))
            .ReturnsAsync(campaign);

        // Act
        var act = () => _service.ExecuteActionAsync(orgId, projectId, campaignId,
            "publish", new CampaignActionRequest(), userId);

        // Assert
        await act.Should().ThrowAsync<CampaignStateMachineException>()
            .WithMessage("*Cannot transition*");
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Campaign>()), Times.Never);
    }

    [Fact]
    public async Task Publish_ScheduledCampaign_ThrowsCampaignStateMachineException()
    {
        // Arrange
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Scheduled Campaign",
            Type = "Coupon",
            Status = "Scheduled", // Already published
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdAsync(
            campaign.OrganizationId, campaign.ProjectId, campaign.Id))
            .ReturnsAsync(campaign);

        // Act
        var act = () => _service.ExecuteActionAsync(campaign.OrganizationId,
            campaign.ProjectId, campaign.Id,
            "publish", new CampaignActionRequest(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<CampaignStateMachineException>()
            .WithMessage("*Cannot transition*");
    }

    [Fact]
    public async Task Publish_NonexistentCampaign_ThrowsArgumentException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByIdAsync(orgId, projectId, campaignId))
            .ReturnsAsync((Campaign?)null);

        // Act
        var act = () => _service.ExecuteActionAsync(orgId, projectId, campaignId,
            "publish", new CampaignActionRequest(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ExecuteAction_UnknownAction_ThrowsCampaignStateMachineException()
    {
        // Arrange
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Test",
            Type = "Coupon",
            Status = "Draft",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdAsync(
            campaign.OrganizationId, campaign.ProjectId, campaign.Id))
            .ReturnsAsync(campaign);

        // Act
        var act = () => _service.ExecuteActionAsync(campaign.OrganizationId,
            campaign.ProjectId, campaign.Id,
            "invalid_action", new CampaignActionRequest(), Guid.NewGuid());

        // Assert — unknown actions fail the transition check first
        await act.Should().ThrowAsync<CampaignStateMachineException>()
            .WithMessage("*Cannot transition*");
    }

    [Fact]
    public async Task Publish_CreatesVersionWithCorrectSnapshot()
    {
        // Arrange
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Black Friday",
            Description = "Big sale",
            Type = "Coupon",
            Status = "Draft",
            StartAt = new DateTimeOffset(2026, 11, 25, 0, 0, 0, TimeSpan.Zero),
            EndAt = new DateTimeOffset(2026, 11, 28, 0, 0, 0, TimeSpan.Zero),
            MaxRedemptions = 1000,
            BudgetAmount = 5000m,
            Currency = "BRL",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdAsync(
            campaign.OrganizationId, campaign.ProjectId, campaign.Id))
            .ReturnsAsync(campaign);

        CampaignVersion? capturedVersion = null;
        _repoMock.Setup(r => r.AddVersionAsync(It.IsAny<CampaignVersion>()))
            .Callback<CampaignVersion>(v => capturedVersion = v);

        // Act
        await _service.ExecuteActionAsync(campaign.OrganizationId,
            campaign.ProjectId, campaign.Id,
            "publish", new CampaignActionRequest(), Guid.NewGuid());

        // Assert
        capturedVersion.Should().NotBeNull();
        capturedVersion!.Version.Should().Be(1);
        capturedVersion.Status.Should().Be("Published");
        capturedVersion.Config.Should().Contain("Black Friday");
        capturedVersion.Config.Should().Contain("Big sale");
        capturedVersion.CampaignId.Should().Be(campaign.Id);
    }

    // ========================================================================
    // ExecuteActionAsync — other valid transitions
    // ========================================================================

    [Fact]
    public async Task Activate_ScheduledCampaign_TransitionsToActive()
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Scheduled Campaign",
            Type = "Coupon",
            Status = "Scheduled",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdAsync(
            campaign.OrganizationId, campaign.ProjectId, campaign.Id))
            .ReturnsAsync(campaign);

        var result = await _service.ExecuteActionAsync(campaign.OrganizationId,
            campaign.ProjectId, campaign.Id,
            "activate", new CampaignActionRequest(), Guid.NewGuid());

        result.Status.Should().Be("Active");
        campaign.Status.Should().Be("Active");
    }

    [Fact]
    public async Task End_ActiveCampaign_TransitionsToEnded()
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Active Campaign",
            Type = "Coupon",
            Status = "Active",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdAsync(
            campaign.OrganizationId, campaign.ProjectId, campaign.Id))
            .ReturnsAsync(campaign);

        var result = await _service.ExecuteActionAsync(campaign.OrganizationId,
            campaign.ProjectId, campaign.Id,
            "end", new CampaignActionRequest(), Guid.NewGuid());

        result.Status.Should().Be("Ended");
    }

    [Fact]
    public async Task Archive_DraftCampaign_TransitionsToArchived()
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Draft Campaign",
            Type = "Coupon",
            Status = "Draft",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdAsync(
            campaign.OrganizationId, campaign.ProjectId, campaign.Id))
            .ReturnsAsync(campaign);

        var result = await _service.ExecuteActionAsync(campaign.OrganizationId,
            campaign.ProjectId, campaign.Id,
            "archive", new CampaignActionRequest(), Guid.NewGuid());

        result.Status.Should().Be("Archived");
    }

    // ========================================================================
    // CampaignStateMachineException — descriptive message
    // ========================================================================

    [Fact]
    public async Task InvalidTransition_IncludesCurrentAndTargetStatus()
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Archived Campaign",
            Type = "Coupon",
            Status = "Archived",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdAsync(
            campaign.OrganizationId, campaign.ProjectId, campaign.Id))
            .ReturnsAsync(campaign);

        var act = () => _service.ExecuteActionAsync(campaign.OrganizationId,
            campaign.ProjectId, campaign.Id,
            "publish", new CampaignActionRequest(), Guid.NewGuid());

        await act.Should().ThrowAsync<CampaignStateMachineException>()
            .WithMessage("*Archived*").WithMessage("*publish*");
    }
}
