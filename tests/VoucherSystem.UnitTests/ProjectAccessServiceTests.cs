using FluentAssertions;
using VoucherSystem.Application;

namespace VoucherSystem.UnitTests;

public class ProjectAccessServiceTests
{
    [Fact]
    public async Task SetMemberProjectAccess_CallsRepository()
    {
        var repoMock = new Moq.Mock<IProjectAccessRepository>();
        var service = new ProjectAccessService(repoMock.Object);
        var memberId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var projectIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        repoMock.Setup(r => r.SetMemberProjectAccessAsync(memberId, roleId, projectIds))
            .Returns(Task.CompletedTask);

        await service.SetMemberProjectAccessAsync(memberId, roleId, projectIds);

        repoMock.Verify(r => r.SetMemberProjectAccessAsync(memberId, roleId, projectIds), Moq.Times.Once);
    }

    [Fact]
    public async Task GetMemberProjectIds_ReturnsCorrectList()
    {
        var repoMock = new Moq.Mock<IProjectAccessRepository>();
        var service = new ProjectAccessService(repoMock.Object);
        var memberId = Guid.NewGuid();
        var expected = new List<Guid> { Guid.NewGuid() };

        repoMock.Setup(r => r.GetMemberProjectIdsAsync(memberId))
            .Returns(Task.FromResult(expected));

        var result = await service.GetMemberProjectIdsAsync(memberId);

        result.Should().BeEquivalentTo(expected);
    }
}
