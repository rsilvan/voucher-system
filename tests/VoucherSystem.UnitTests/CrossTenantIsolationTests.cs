using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoucherSystem.Api.Middleware;
using VoucherSystem.Application;

namespace VoucherSystem.UnitTests;

public class CrossTenantIsolationTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProjectA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static (HttpContext, Mock<IProjectContextCache>, Mock<IProjectRepository>)
        CreateAuthenticatedContext(Guid orgId, Guid? projectId = null, string? projectStatus = null, bool isApiKey = false)
    {
        var ctx = new DefaultHttpContext();
        var identity = new System.Security.Claims.ClaimsIdentity(new System.Security.Claims.Claim[]
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("organization_id", orgId.ToString()),
            new("role_id", Guid.NewGuid().ToString()),
        }, "test");

        if (projectId.HasValue) identity.AddClaim(new("project_id", projectId.Value.ToString()));
        if (projectStatus is not null) identity.AddClaim(new("project_status", projectStatus));

        ctx.User = new System.Security.Claims.ClaimsPrincipal(identity);
        ctx.Items["OrganizationId"] = orgId;
        ctx.Items["UserId"] = Guid.NewGuid();
        ctx.Items["RoleId"] = Guid.NewGuid();
        if (projectId.HasValue) { ctx.Items["ProjectId"] = projectId.Value; ctx.Items["CurrentProjectId"] = projectId.Value; }
        if (projectStatus is not null) ctx.Items["ProjectStatus"] = projectStatus;
        if (isApiKey) ctx.Items["AuthMethod"] = "ApiKey";

        return (ctx, new Mock<IProjectContextCache>(), new Mock<IProjectRepository>());
    }

    [Fact]
    public async Task UserFromOrgA_RequestingProjectOfOrgB_ShouldNotReturnProjectData()
    {
        var (ctx, cacheMock, repoMock) = CreateAuthenticatedContext(OrgA, projectId: ProjectB);
        cacheMock.Setup(c => c.GetAsync(OrgA, ProjectB)).ReturnsAsync((ProjectContextCacheEntry?)null);
        repoMock.Setup(r => r.GetByIdAsync(ProjectB, OrgA)).ReturnsAsync((Domain.Project?)null);

        var middleware = new ProjectContextMiddleware(_ => Task.CompletedTask);
        ctx.RequestServices = CreateServices(cacheMock.Object, repoMock.Object);
        await middleware.InvokeAsync(ctx, cacheMock.Object, repoMock.Object);

        var projCtx = ctx.Items["CurrentProjectContext"] as CurrentProjectContext;
        projCtx.Should().NotBeNull();
        projCtx!.OrganizationId.Should().Be(OrgA);
        projCtx.ProjectId.Should().Be(ProjectB);
        projCtx.Status.Should().Be("Active");
        cacheMock.Verify(c => c.SetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ProjectContextCacheEntry>()), Times.Never);
    }

    [Fact]
    public async Task UserFromOrgA_WithOrgBProjectIdInHeader_ShouldResolveWithDefaults()
    {
        var (ctx, cacheMock, repoMock) = CreateAuthenticatedContext(OrgA, projectId: null);
        ctx.Request.Headers["X-Project-Id"] = ProjectB.ToString();
        cacheMock.Setup(c => c.GetAsync(OrgA, ProjectB)).ReturnsAsync((ProjectContextCacheEntry?)null);
        repoMock.Setup(r => r.GetByIdAsync(ProjectB, OrgA)).ReturnsAsync((Domain.Project?)null);

        var middleware = new ProjectContextMiddleware(_ => Task.CompletedTask);
        ctx.RequestServices = CreateServices(cacheMock.Object, repoMock.Object);
        await middleware.InvokeAsync(ctx, cacheMock.Object, repoMock.Object);

        var projCtx = ctx.Items["CurrentProjectContext"] as CurrentProjectContext;
        projCtx.Should().NotBeNull();
        projCtx!.ProjectId.Should().Be(ProjectB);
        projCtx.Status.Should().Be("Active");
        cacheMock.Verify(c => c.SetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ProjectContextCacheEntry>()), Times.Never);
    }

    [Theory]
    [InlineData("Disabled")]
    [InlineData("Archived")]
    public async Task WriteOperation_OnNonActiveProject_Returns423(string status)
    {
        var (ctx, cacheMock, repoMock) = CreateAuthenticatedContext(OrgA, projectId: ProjectA, projectStatus: status);
        ctx.Request.Method = "POST";
        ctx.Request.Path = "/api/vouchers";
        cacheMock.Setup(c => c.GetAsync(OrgA, ProjectA)).ReturnsAsync((ProjectContextCacheEntry?)null);

        var middleware = new ProjectContextMiddleware(_ => Task.CompletedTask);
        ctx.RequestServices = CreateServices(cacheMock.Object, repoMock.Object);
        await middleware.InvokeAsync(ctx, cacheMock.Object, repoMock.Object);

        ctx.Response.StatusCode.Should().Be(423);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task ReadOperation_OnNonActiveProject_AllowsThrough(string method)
    {
        var (ctx, cacheMock, repoMock) = CreateAuthenticatedContext(OrgA, projectId: ProjectA, projectStatus: "Disabled");
        ctx.Request.Method = method;
        ctx.Request.Path = "/api/vouchers";
        cacheMock.Setup(c => c.GetAsync(OrgA, ProjectA)).ReturnsAsync((ProjectContextCacheEntry?)null);

        var middleware = new ProjectContextMiddleware(_ => Task.CompletedTask);
        ctx.RequestServices = CreateServices(cacheMock.Object, repoMock.Object);
        await middleware.InvokeAsync(ctx, cacheMock.Object, repoMock.Object);

        ctx.Response.StatusCode.Should().Be(200);
    }

    [Theory]
    [InlineData("/api/projects/enable")]
    [InlineData("/api/projects/restore")]
    [InlineData("/api/projects/archive")]
    public async Task WriteOperation_OnManagementEndpoint_AllowsThrough(string path)
    {
        var (ctx, cacheMock, repoMock) = CreateAuthenticatedContext(OrgA, projectId: ProjectA, projectStatus: "Disabled");
        ctx.Request.Method = "POST";
        ctx.Request.Path = path;
        cacheMock.Setup(c => c.GetAsync(OrgA, ProjectA)).ReturnsAsync((ProjectContextCacheEntry?)null);

        var middleware = new ProjectContextMiddleware(_ => Task.CompletedTask);
        ctx.RequestServices = CreateServices(cacheMock.Object, repoMock.Object);
        await middleware.InvokeAsync(ctx, cacheMock.Object, repoMock.Object);

        ctx.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ProjectContext_WhenCacheHit_UsesCachedData()
    {
        var (ctx, cacheMock, repoMock) = CreateAuthenticatedContext(OrgA, projectId: ProjectA);
        cacheMock.Setup(c => c.GetAsync(OrgA, ProjectA)).ReturnsAsync(new ProjectContextCacheEntry
        {
            OrganizationId = OrgA, ProjectId = ProjectA, Environment = "Staging", Status = "Active",
            Currency = "USD", TimeZone = "America/New_York", Locale = "en-US", Country = "US",
        });

        var middleware = new ProjectContextMiddleware(_ => Task.CompletedTask);
        ctx.RequestServices = CreateServices(cacheMock.Object, repoMock.Object);
        await middleware.InvokeAsync(ctx, cacheMock.Object, repoMock.Object);

        var projCtx = ctx.Items["CurrentProjectContext"] as CurrentProjectContext;
        projCtx.Should().NotBeNull();
        projCtx!.Environment.Should().Be("Staging");
        projCtx.Currency.Should().Be("USD");
        repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ProjectContext_WhenCacheMiss_FallsBackToDbAndPopulatesCache()
    {
        var (ctx, cacheMock, repoMock) = CreateAuthenticatedContext(OrgA, projectId: ProjectA);
        cacheMock.Setup(c => c.GetAsync(OrgA, ProjectA)).ReturnsAsync((ProjectContextCacheEntry?)null);
        repoMock.Setup(r => r.GetByIdAsync(ProjectA, OrgA)).ReturnsAsync(new Domain.Project
        {
            Id = ProjectA, OrganizationId = OrgA, Name = "Test", Slug = "test",
            Environment = "Staging", Status = "Active", Currency = "EUR",
            TimeZone = "Europe/Berlin", Locale = "de-DE", Country = "DE",
        });

        var middleware = new ProjectContextMiddleware(_ => Task.CompletedTask);
        ctx.RequestServices = CreateServices(cacheMock.Object, repoMock.Object);
        await middleware.InvokeAsync(ctx, cacheMock.Object, repoMock.Object);

        var projCtx = ctx.Items["CurrentProjectContext"] as CurrentProjectContext;
        projCtx.Should().NotBeNull();
        projCtx!.Environment.Should().Be("Staging");
        projCtx.Currency.Should().Be("EUR");
        cacheMock.Verify(c => c.SetAsync(OrgA, ProjectA, It.Is<ProjectContextCacheEntry>(e => e.Environment == "Staging")), Times.Once);
    }

    [Fact]
    public async Task ApiKeyAuth_CannotOverrideProjectIdViaHeader()
    {
        var (ctx, cacheMock, repoMock) = CreateAuthenticatedContext(OrgA, projectId: ProjectA, isApiKey: true);
        ctx.Request.Headers["X-Project-Id"] = ProjectB.ToString();
        cacheMock.Setup(c => c.GetAsync(OrgA, ProjectA)).ReturnsAsync((ProjectContextCacheEntry?)null);

        var middleware = new ProjectContextMiddleware(_ => Task.CompletedTask);
        ctx.RequestServices = CreateServices(cacheMock.Object, repoMock.Object);
        await middleware.InvokeAsync(ctx, cacheMock.Object, repoMock.Object);

        var projCtx = ctx.Items["CurrentProjectContext"] as CurrentProjectContext;
        projCtx.Should().NotBeNull();
        projCtx!.ProjectId.Should().Be(ProjectA);
    }

    private static IServiceProvider CreateServices(IProjectContextCache c, IProjectRepository r)
    {
        var svc = new ServiceCollection();
        svc.AddSingleton(c);
        svc.AddSingleton(r);
        return svc.BuildServiceProvider();
    }
}
