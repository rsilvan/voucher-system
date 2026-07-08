using FluentAssertions;
using Moq;
using VoucherSystem.Application;
using VoucherSystem.Contracts;
using VoucherSystem.Contracts.Brands;
using VoucherSystem.Contracts.Stores;
using VoucherSystem.Contracts.Areas;
using VoucherSystem.Domain;

namespace VoucherSystem.UnitTests;

/// <summary>
/// R002 E2E tests covering onboarding, isolation, brand profiles, stores, and areas.
/// </summary>
public class R002E2ETests
{
    // ========================================================================
    // 1. Onboarding: criar organização → verificar projeto principal criado
    // ========================================================================

    [Fact]
    public async Task Onboarding_CreateOrganization_CreatesPrimaryProject()
    {
        // Arrange
        var orgRepoMock = new Mock<IOrganizationRepository>();
        var service = new OrganizationService(orgRepoMock.Object, new Mock<IEmailService>().Object);

        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Minha Empresa",
            ResponsibleName = "João Silva",
            Email = "joao@empresa.com",
            Password = "SenhaForte123!",
            ConfirmPassword = "SenhaForte123!",
            Country = "BR",
            AcceptedTerms = true,
            AcceptedPrivacyPolicy = true,
        };

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            Key = "Trial",
            Name = "Trial",
            MaxUsers = 3,
            MaxProjects = 1,
            MaxActiveCampaigns = 5,
        };
        orgRepoMock.Setup(r => r.GetTrialPlanAsync()).ReturnsAsync(plan);

        var ownerRole = new Role
        {
            Id = Guid.NewGuid(),
            Key = "OrganizationOwner",
            Name = "OrganizationOwner",
            IsSystemRole = true,
        };
        orgRepoMock.Setup(r => r.GetSystemRoleByKeyAsync("OrganizationOwner")).ReturnsAsync(ownerRole);

        orgRepoMock.Setup(r => r.EmailExistsAsync("joao@empresa.com")).ReturnsAsync(false);

        Organization? savedOrg = null;
        Project? savedProject = null;
        orgRepoMock.Setup(r => r.SaveOrganizationAsync(
                It.IsAny<Organization>(),
                It.IsAny<User>(),
                It.IsAny<Project>(),
                It.IsAny<OrganizationMember>()))
            .Callback<Organization, User, Project, OrganizationMember>((o, u, p, m) =>
            {
                savedOrg = o;
                savedProject = p;
            })
            .Returns(Task.CompletedTask);

        // Act
        var response = await service.CreateOrganizationAsync(request);

        // Assert — verificar projeto principal criado
        response.Should().NotBeNull();
        response.Status.Should().Be("Created");
        response.OrganizationId.Should().NotBeEmpty();
        response.ProjectId.Should().NotBeEmpty();
        response.UserId.Should().NotBeEmpty();
        response.Role.Should().Be("OrganizationOwner");

        savedOrg.Should().NotBeNull();
        savedOrg!.Name.Should().Be("Minha Empresa");
        savedOrg.Status.Should().Be("Active");

        savedProject.Should().NotBeNull();
        savedProject!.OrganizationId.Should().Be(savedOrg.Id);
        savedProject.Name.Should().Be("Projeto Principal");
        savedProject.Status.Should().Be("Active");
        savedProject.Environment.Should().Be("Production");
        savedProject.Slug.Should().Be("projeto-principal");
    }

    // ========================================================================
    // 2. Isolamento: verificar que dados de projeto X não vazam para Y
    // ========================================================================

    [Fact]
    public async Task ProjectIsolation_DataOfProjectX_DoesNotLeakToProjectY()
    {
        // Arrange — two separate organizations with their own projects
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();

        var brandRepo = new Mock<IBrandRepository>();
        var projectRepo = new Mock<IProjectRepository>();
        var auditMock = new Mock<IAuditLogWriter>();
        var brandService = new BrandService(brandRepo.Object, projectRepo.Object, auditMock.Object);

        // Project A belongs to Org A
        projectRepo.Setup(r => r.GetByIdAsync(projectA, orgA))
            .ReturnsAsync(new Project { Id = projectA, OrganizationId = orgA });

        // Project B belongs to Org B
        projectRepo.Setup(r => r.GetByIdAsync(projectB, orgB))
            .ReturnsAsync(new Project { Id = projectB, OrganizationId = orgB });

        // Capture created brand and use closure for GetByProjectAsync
        BrandProfile? capturedBrand = null;
        brandRepo.Setup(r => r.GetByProjectAsync(projectA))
            .ReturnsAsync(() => capturedBrand);
        brandRepo.Setup(r => r.GetByProjectAsync(projectB))
            .ReturnsAsync((BrandProfile?)null);
        brandRepo.Setup(r => r.AddAsync(It.IsAny<BrandProfile>()))
            .Callback<BrandProfile>(b =>
            {
                b.Id = Guid.NewGuid();
                capturedBrand = b;
            })
            .Returns(Task.CompletedTask);

        // Act — create brand in Project A
        var createRequest = new CreateBrandRequest
        {
            Name = "Brand A",
            Description = "Only for Project A",
        };
        var brandA = await brandService.CreateAsync(projectA, orgA, createRequest);

        // Assert — brand was created for Project A
        brandA.Should().NotBeNull();
        brandA.Name.Should().Be("Brand A");
        brandA.ProjectId.Should().Be(projectA);

        // Verify: Project B has no brand (even after Project A created one)
        var brandOfProjectB = await brandService.GetByProjectAsync(projectB, orgB);
        brandOfProjectB.Should().BeNull("Project B has no brand");

        // Verify: Project A accessed from Org B context returns null (org isolation)
        var brandFromWrongOrg = await brandService.GetByProjectAsync(projectA, orgB);
        brandFromWrongOrg.Should().BeNull("Project A does not belong to Org B — project repo returns null for wrong org");

        // Verify: correct context returns the brand
        var brandFromCorrectContext = await brandService.GetByProjectAsync(projectA, orgA);
        brandFromCorrectContext.Should().NotBeNull();
        brandFromCorrectContext!.Name.Should().Be("Brand A");
    }

    // ========================================================================
    // 3. Marca: criar BrandProfile via service e verificar persistência
    // ========================================================================

    [Fact]
    public async Task BrandProfile_CreateAndVerifyPersistence()
    {
        // Arrange
        var brandRepo = new Mock<IBrandRepository>();
        var projectRepo = new Mock<IProjectRepository>();
        var auditMock = new Mock<IAuditLogWriter>();
        var brandService = new BrandService(brandRepo.Object, projectRepo.Object, auditMock.Object);

        var orgId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        projectRepo.Setup(r => r.GetByIdAsync(projectId, orgId))
            .ReturnsAsync(new Project { Id = projectId, OrganizationId = orgId });

        BrandProfile? capturedBrand = null;
        brandRepo.Setup(r => r.AddAsync(It.IsAny<BrandProfile>()))
            .Callback<BrandProfile>(b =>
            {
                b.Id = Guid.NewGuid(); // simulate DB-generated ID
                capturedBrand = b;
            })
            .Returns(Task.CompletedTask);

        brandRepo.Setup(r => r.GetByProjectAsync(projectId))
            .ReturnsAsync(() => capturedBrand); // simulate persistence via closure

        var request = new CreateBrandRequest
        {
            Name = "Minha Marca",
            Description = "Uma marca de teste",
            WebsiteUrl = "https://minhamarca.com.br",
            SupportEmail = "suporte@minhamarca.com.br",
            PrimaryColor = "#FF5733",
            SecondaryColor = "#33FF57",
            Address = new BrandAddressRequest
            {
                Street = "Rua Teste, 123",
                City = "São Paulo",
                State = "SP",
                ZipCode = "01001-000",
                Country = "BR",
            },
        };

        // Act
        var response = await brandService.CreateAsync(projectId, orgId, request);

        // Assert — verificar resposta
        response.Should().NotBeNull();
        response.Name.Should().Be("Minha Marca");
        response.Description.Should().Be("Uma marca de teste");
        response.WebsiteUrl.Should().Be("https://minhamarca.com.br");
        response.SupportEmail.Should().Be("suporte@minhamarca.com.br");
        response.PrimaryColor.Should().Be("#FF5733");
        response.SecondaryColor.Should().Be("#33FF57");
        response.Address.Street.Should().Be("Rua Teste, 123");
        response.Address.City.Should().Be("São Paulo");
        response.Address.State.Should().Be("SP");
        response.ProjectId.Should().Be(projectId);
        response.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        // Assert — verificar que foi persistido (captured via mock)
        capturedBrand.Should().NotBeNull();
        capturedBrand!.Name.Should().Be("Minha Marca");
        capturedBrand.ProjectId.Should().Be(projectId);

        // Simulate a "read-back" to verify persistence semantics
        var readBack = await brandService.GetByProjectAsync(projectId, orgId);
        readBack.Should().NotBeNull();
        readBack!.Name.Should().Be("Minha Marca");
        readBack.WebsiteUrl.Should().Be("https://minhamarca.com.br");

        // Verify audit log was written
        auditMock.Verify(a => a.Write(
            orgId, projectId, null, "brand.created", "BrandProfile",
            capturedBrand.Id.ToString(), It.IsAny<object?>()), Times.Once);
    }

    // ========================================================================
    // 4. Store + Area: criar store, criar area, associar, verificar resultado
    // ========================================================================

    [Fact]
    public async Task StoreAndArea_CreateAndAssociate_ReturnsCorrectResult()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Mocks for StoreService
        var storeRepo = new Mock<IStoreRepository>();
        var auditMock = new Mock<IAuditLogWriter>();
        var storeService = new StoreService(storeRepo.Object, auditMock.Object);

        // Mocks for AreaService
        var areaRepo = new Mock<IAreaRepository>();
        var areaService = new AreaService(areaRepo.Object, storeRepo.Object, auditMock.Object);

        // --- Prepare store mocks ---
        Store? capturedStore = null;
        storeRepo.Setup(r => r.CodeExistsAsync(projectId, "LOJA001"))
            .ReturnsAsync(false);
        storeRepo.Setup(r => r.AddAsync(It.IsAny<Store>()))
            .Callback<Store>(s =>
            {
                s.Id = Guid.NewGuid();
                capturedStore = s;
            })
            .Returns(Task.CompletedTask);
        storeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), projectId))
            .ReturnsAsync((Guid id, Guid pid) =>
                capturedStore?.Id == id && pid == projectId
                    ? capturedStore
                    : null);

        // --- Create store ---
        var createStoreRequest = new CreateStoreRequest
        {
            Code = "LOJA001",
            Name = "Loja Matriz",
            StoreType = "Physical",
            City = "São Paulo",
            State = "SP",
            Country = "BR",
        };

        var storeResponse = await storeService.CreateAsync(projectId, createStoreRequest);

        storeResponse.Should().NotBeNull();
        storeResponse.Code.Should().Be("LOJA001");
        storeResponse.Name.Should().Be("Loja Matriz");
        storeResponse.Status.Should().Be("Active");

        // --- Prepare area mocks (before creation) ---
        Area? capturedArea = null;
        areaRepo.Setup(r => r.NameExistsAsync(projectId, "Varejo"))
            .ReturnsAsync(false);
        areaRepo.Setup(r => r.GetAncestorsAsync(It.IsAny<Guid>(), projectId))
            .ReturnsAsync(new List<Area>());
        areaRepo.Setup(r => r.GetStoresForAreaAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => new List<Store>()); // initially empty
        areaRepo.Setup(r => r.AddAsync(It.IsAny<Area>()))
            .Callback<Area>(a =>
            {
                a.Id = Guid.NewGuid();
                capturedArea = a;
            })
            .Returns(Task.CompletedTask);

        areaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), projectId))
            .ReturnsAsync((Guid id, Guid pid) =>
                capturedArea?.Id == id && pid == projectId
                    ? capturedArea
                    : null);

        areaRepo.Setup(r => r.GetByProjectAsync(projectId))
            .ReturnsAsync(() => capturedArea is not null ? new List<Area> { capturedArea } : new List<Area>());

        // --- Create area ---
        var createAreaRequest = new CreateAreaRequest
        {
            Name = "Varejo",
            Description = "Área de vendas no varejo",
        };

        var areaResponse = await areaService.CreateAsync(projectId, createAreaRequest);

        areaResponse.Should().NotBeNull();
        areaResponse.Name.Should().Be("Varejo");
        areaResponse.Depth.Should().Be(0);
        areaResponse.ParentAreaId.Should().BeNull();

        // --- Prepare association mocks ---
        var storeInArea = false;
        areaRepo.Setup(r => r.StoreInAreaAsync(areaResponse.Id, storeResponse.Id))
            .ReturnsAsync(() => storeInArea);
        areaRepo.Setup(r => r.AddStoreToAreaAsync(areaResponse.Id, storeResponse.Id))
            .Callback(() => storeInArea = true)
            .Returns(Task.CompletedTask);

        // After association, GetStoresForAreaAsync should return the store
        areaRepo.Setup(r => r.GetStoresForAreaAsync(areaResponse.Id))
            .ReturnsAsync(() => storeInArea && capturedStore is not null
                ? new List<Store> { capturedStore }
                : new List<Store>());

        // --- Associate store with area ---
        await areaService.AssignStoresAsync(areaResponse.Id, projectId, new List<Guid> { storeResponse.Id });

        // Assert — verify association
        storeInArea.Should().BeTrue();

        // Get tree to verify store appears in area
        var tree = await areaService.GetTreeAsync(projectId);
        tree.Should().NotBeNull();
        tree.Roots.Should().HaveCount(1);
        tree.Roots[0].Name.Should().Be("Varejo");
        tree.Roots[0].Stores.Should().HaveCount(1);
        tree.Roots[0].Stores[0].Code.Should().Be("LOJA001");
        tree.Roots[0].Stores[0].Name.Should().Be("Loja Matriz");
    }
}
