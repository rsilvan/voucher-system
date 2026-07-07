using Moq;
using FluentAssertions;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Domain;

namespace VoucherSystem.UnitTests;

public class OrganizationServiceTests
{
    private readonly Mock<IOrganizationRepository> _repoMock;
    private readonly OrganizationService _service;

    public OrganizationServiceTests()
    {
        _repoMock = new Mock<IOrganizationRepository>();
        _service = new OrganizationService(_repoMock.Object);
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Empresa Teste",
            ResponsibleName = "João",
            Email = "joao@teste.com",
            Password = "SenhaForte123!",
            ConfirmPassword = "SenhaForte123!",
            Country = "BR",
            AcceptedTerms = true,
            AcceptedPrivacyPolicy = true
        };

        _repoMock.Setup(r => r.EmailExistsAsync("joao@teste.com")).ReturnsAsync(false);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            Key = "Trial",
            Name = "Trial",
            MaxUsers = 3,
            MaxProjects = 1,
            MaxActiveCampaigns = 5
        };
        _repoMock.Setup(r => r.GetTrialPlanAsync()).ReturnsAsync(plan);

        var ownerRole = new Role
        {
            Id = Guid.NewGuid(),
            Key = "OrganizationOwner",
            Name = "OrganizationOwner",
            IsSystemRole = true
        };
        _repoMock.Setup(r => r.GetSystemRoleByKeyAsync("OrganizationOwner")).ReturnsAsync(ownerRole);

        _repoMock.Setup(r => r.SaveOrganizationAsync(
            It.IsAny<Organization>(),
            It.IsAny<User>(),
            It.IsAny<Project>(),
            It.IsAny<OrganizationMember>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _service.CreateOrganizationAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Status.Should().Be("Created");
        response.Role.Should().Be("OrganizationOwner");
        response.OrganizationId.Should().NotBeEmpty();
        response.ProjectId.Should().NotBeEmpty();
        response.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithDuplicatedEmail_ThrowsArgumentException()
    {
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Empresa",
            ResponsibleName = "João",
            Email = "joao@duplicado.com",
            Password = "SenhaForte123!",
            ConfirmPassword = "SenhaForte123!",
            Country = "BR",
            AcceptedTerms = true,
            AcceptedPrivacyPolicy = true
        };

        _repoMock.Setup(r => r.EmailExistsAsync("joao@duplicado.com")).ReturnsAsync(true);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateOrganizationAsync(request));
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithMismatchedPasswords_ThrowsArgumentException()
    {
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Empresa",
            ResponsibleName = "João",
            Email = "joao@teste.com",
            Password = "Senha123!",
            ConfirmPassword = "OutraSenha456!",
            Country = "BR",
            AcceptedTerms = true,
            AcceptedPrivacyPolicy = true
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateOrganizationAsync(request));
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithoutAcceptedTerms_ThrowsArgumentException()
    {
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Empresa",
            ResponsibleName = "João",
            Email = "joao@teste.com",
            Password = "SenhaForte123!",
            ConfirmPassword = "SenhaForte123!",
            Country = "BR",
            AcceptedTerms = false,
            AcceptedPrivacyPolicy = true
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateOrganizationAsync(request));
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithShortPassword_ThrowsArgumentException()
    {
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Empresa",
            ResponsibleName = "João",
            Email = "joao@teste.com",
            Password = "1234567",
            ConfirmPassword = "1234567",
            Country = "BR",
            AcceptedTerms = true,
            AcceptedPrivacyPolicy = true
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateOrganizationAsync(request));
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithMissingName_ThrowsArgumentException()
    {
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "",
            ResponsibleName = "João",
            Email = "joao@teste.com",
            Password = "SenhaForte123!",
            ConfirmPassword = "SenhaForte123!",
            Country = "BR",
            AcceptedTerms = true,
            AcceptedPrivacyPolicy = true
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateOrganizationAsync(request));
    }

    [Fact]
    public async Task CreateOrganizationAsync_WhenNoTrialPlan_ThrowsInvalidOperationException()
    {
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Empresa",
            ResponsibleName = "João",
            Email = "joao@teste.com",
            Password = "SenhaForte123!",
            ConfirmPassword = "SenhaForte123!",
            Country = "BR",
            AcceptedTerms = true,
            AcceptedPrivacyPolicy = true
        };

        _repoMock.Setup(r => r.EmailExistsAsync("joao@teste.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetTrialPlanAsync()).ReturnsAsync((Plan?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateOrganizationAsync(request));
    }

    [Fact]
    public async Task CreateOrganizationAsync_WhenNoOwnerRole_ThrowsInvalidOperationException()
    {
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Empresa",
            ResponsibleName = "João",
            Email = "joao@teste.com",
            Password = "SenhaForte123!",
            ConfirmPassword = "SenhaForte123!",
            Country = "BR",
            AcceptedTerms = true,
            AcceptedPrivacyPolicy = true
        };

        _repoMock.Setup(r => r.EmailExistsAsync("joao@teste.com")).ReturnsAsync(false);

        var plan = new Plan { Id = Guid.NewGuid(), Key = "Trial", Name = "Trial" };
        _repoMock.Setup(r => r.GetTrialPlanAsync()).ReturnsAsync(plan);
        _repoMock.Setup(r => r.GetSystemRoleByKeyAsync("OrganizationOwner")).ReturnsAsync((Role?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateOrganizationAsync(request));
    }
}
