using FluentAssertions;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.Tests;

public class CareServicePackageServiceUnitTest
{
    private static CreateCareServicePackageRequestDto BuildOneTimeRequest() => new()
    {
        Name = "Starter",
        Description = "One time service",
        ServiceType = (int)CareServiceTypeEnum.OneTime,
        VisitPerWeek = 3,
        DurationDays = 14,
        UnitPrice = 150000,
        SuitabilityRules = new List<PackagePlantSuitabilityRuleRequestDto>
        {
            new() { CategoryId = 1 }
        }
    };

    private static CareServicePackageService CreateSut(Mock<IUnitOfWork> unitOfWork, Mock<ICacheService> cacheService)
    {
        return new CareServicePackageService(unitOfWork.Object, cacheService.Object);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWork(
        Mock<ICareServicePackageRepository> packageRepository,
        Mock<ICategoryRepository> categoryRepository)
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.CareServicePackageRepository).Returns(packageRepository.Object);
        unitOfWork.SetupGet(x => x.CategoryRepository).Returns(categoryRepository.Object);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);
        return unitOfWork;
    }

    [Fact]
    public async Task Normal_CreateAsync_ShouldCreatePackageAndNormalizeOneTimeValues()
    {
        var category = new Category { Id = 1, Name = "Indoor" };
        var package = new CareServicePackage
        {
            Id = 25,
            Name = "Starter",
            ServiceType = (int)CareServiceTypeEnum.OneTime,
            DurationDays = 1,
            IsActive = true
        };

        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.ExistsByNameAsync("Starter", null)).ReturnsAsync(false);
        packageRepository.Setup(r => r.PrepareCreate(It.IsAny<CareServicePackage>()))
            .Callback<CareServicePackage>(p => p.Id = package.Id);
        packageRepository.Setup(r => r.AddSuitabilityRulesAsync(package.Id, It.IsAny<IEnumerable<PackagePlantSuitability>>()))
            .Callback<int, IEnumerable<PackagePlantSuitability>>((_, rules) =>
            {
                foreach (var rule in rules)
                {
                    rule.Category = category;
                    package.PackagePlantSuitabilities.Add(rule);
                }
            })
            .Returns(Task.CompletedTask);
        packageRepository.Setup(r => r.GetByIdWithDetailsAsync(package.Id)).ReturnsAsync(package);

        var categoryRepository = new Mock<ICategoryRepository>(MockBehavior.Loose);
        categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

        var unitOfWork = CreateUnitOfWork(packageRepository, categoryRepository);
        var cacheService = new Mock<ICacheService>(MockBehavior.Loose);
        cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = CreateSut(unitOfWork, cacheService);

        var request = BuildOneTimeRequest();

        var result = await sut.CreateAsync(request);

        result.Id.Should().Be(package.Id);
        result.ServiceType.Should().Be((int)CareServiceTypeEnum.OneTime);
        result.DurationDays.Should().Be(1);
        result.VisitPerWeek.Should().BeNull();
        result.SuitabilityRules.Should().ContainSingle();
    }

    [Fact]
    public async Task Abnormal_CreateAsync_ShouldThrow_WhenSuitabilityRuleHasTwoConditions()
    {
        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), null)).ReturnsAsync(false);

        var categoryRepository = new Mock<ICategoryRepository>(MockBehavior.Loose);
        categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Category { Id = 1, Name = "Indoor" });

        var unitOfWork = CreateUnitOfWork(packageRepository, categoryRepository);
        var cacheService = new Mock<ICacheService>(MockBehavior.Loose);

        var sut = CreateSut(unitOfWork, cacheService);

        var request = new CreateCareServicePackageRequestDto
        {
            Name = "Invalid",
            ServiceType = (int)CareServiceTypeEnum.OneTime,
            DurationDays = 1,
            UnitPrice = 100000,
            SuitabilityRules = new List<PackagePlantSuitabilityRuleRequestDto>
            {
                new() { CategoryId = 1, CareDifficultyLevel = 1 }
            }
        };

        var act = () => sut.CreateAsync(request);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Each suitability rule must contain only one condition");
    }

    [Fact]
    public async Task Normal_UpdateAsync_ShouldUpdatePackage_WhenRequestIsValid()
    {
        var package = new CareServicePackage
        {
            Id = 40,
            Name = "Original",
            IsActive = true,
            PackagePlantSuitabilities = new List<PackagePlantSuitability>()
        };

        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.GetByIdAsync(40)).ReturnsAsync(package);
        packageRepository.Setup(r => r.ExistsByNameAsync("Updated", 40)).ReturnsAsync(false);
        packageRepository.Setup(r => r.GetByIdWithDetailsAsync(40)).ReturnsAsync(package);
        packageRepository.Setup(r => r.PrepareUpdate(It.IsAny<CareServicePackage>()));

        var categoryRepository = new Mock<ICategoryRepository>(MockBehavior.Loose);
        var unitOfWork = CreateUnitOfWork(packageRepository, categoryRepository);
        var cacheService = new Mock<ICacheService>(MockBehavior.Loose);
        cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = CreateSut(unitOfWork, cacheService);

        var request = new UpdateCareServicePackageRequestDto
        {
            Name = "Updated",
            Description = "Updated desc",
            UnitPrice = 200000,
            IsActive = false
        };

        var result = await sut.UpdateAsync(40, request);

        result.Name.Should().Be("Updated");
        result.Description.Should().Be("Updated desc");
        result.UnitPrice.Should().Be(200000);
        result.IsActive.Should().BeFalse();
        packageRepository.Verify(r => r.PrepareUpdate(package), Times.Once);
    }

    [Fact]
    public async Task Normal_CreateAsync_ShouldCreatePeriodicPackage_WhenVisitPerWeekIsValid()
    {
        var package = new CareServicePackage
        {
            Id = 26,
            Name = "Periodic",
            ServiceType = (int)CareServiceTypeEnum.Periodic,
            DurationDays = 30,
            VisitPerWeek = 3,
            IsActive = true
        };

        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.ExistsByNameAsync("Periodic", null)).ReturnsAsync(false);
        packageRepository.Setup(r => r.PrepareCreate(It.IsAny<CareServicePackage>())).Callback<CareServicePackage>(p => p.Id = 26);
        packageRepository.Setup(r => r.GetByIdWithDetailsAsync(26)).ReturnsAsync(package);
        packageRepository.Setup(r => r.AddSuitabilityRulesAsync(26, It.IsAny<IEnumerable<PackagePlantSuitability>>())).Returns(Task.CompletedTask);

        var categoryRepository = new Mock<ICategoryRepository>(MockBehavior.Loose);
        categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Indoor" });

        var unitOfWork = CreateUnitOfWork(packageRepository, categoryRepository);
        var cacheService = new Mock<ICacheService>(MockBehavior.Loose);
        cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var sut = CreateSut(unitOfWork, cacheService);

        var request = new CreateCareServicePackageRequestDto
        {
            Name = "Periodic",
            ServiceType = (int)CareServiceTypeEnum.Periodic,
            DurationDays = 30,
            VisitPerWeek = 3,
            UnitPrice = 250000,
            SuitabilityRules = new List<PackagePlantSuitabilityRuleRequestDto> { new() { CategoryId = 1 } }
        };

        var result = await sut.CreateAsync(request);

        result.Id.Should().Be(26);
        result.VisitPerWeek.Should().Be(3);
        result.DurationDays.Should().Be(30);
    }

    [Fact]
    public async Task Boundary_CreateAsync_ShouldAcceptVisitPerWeekAtLowerBound()
    {
        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.ExistsByNameAsync("B1", null)).ReturnsAsync(false);
        packageRepository.Setup(r => r.PrepareCreate(It.IsAny<CareServicePackage>())).Callback<CareServicePackage>(p => p.Id = 27);
        packageRepository.Setup(r => r.GetByIdWithDetailsAsync(27)).ReturnsAsync(new CareServicePackage
        {
            Id = 27,
            Name = "B1",
            VisitPerWeek = 1,
            DurationDays = 30,
            ServiceType = (int)CareServiceTypeEnum.Periodic,
            IsActive = true
        });
        packageRepository.Setup(r => r.AddSuitabilityRulesAsync(27, It.IsAny<IEnumerable<PackagePlantSuitability>>())).Returns(Task.CompletedTask);

        var categoryRepository = new Mock<ICategoryRepository>(MockBehavior.Loose);
        categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Indoor" });

        var sut = CreateSut(CreateUnitOfWork(packageRepository, categoryRepository), new Mock<ICacheService>(MockBehavior.Loose));

        var result = await sut.CreateAsync(new CreateCareServicePackageRequestDto
        {
            Name = "B1",
            ServiceType = (int)CareServiceTypeEnum.Periodic,
            DurationDays = 30,
            VisitPerWeek = 1,
            UnitPrice = 100000,
            SuitabilityRules = new List<PackagePlantSuitabilityRuleRequestDto> { new() { CategoryId = 1 } }
        });

        result.VisitPerWeek.Should().Be(1);
    }

    [Fact]
    public async Task Boundary_CreateAsync_ShouldAcceptVisitPerWeekAtUpperBound()
    {
        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.ExistsByNameAsync("B6", null)).ReturnsAsync(false);
        packageRepository.Setup(r => r.PrepareCreate(It.IsAny<CareServicePackage>())).Callback<CareServicePackage>(p => p.Id = 28);
        packageRepository.Setup(r => r.GetByIdWithDetailsAsync(28)).ReturnsAsync(new CareServicePackage
        {
            Id = 28,
            Name = "B6",
            VisitPerWeek = 6,
            DurationDays = 30,
            ServiceType = (int)CareServiceTypeEnum.Periodic,
            IsActive = true
        });
        packageRepository.Setup(r => r.AddSuitabilityRulesAsync(28, It.IsAny<IEnumerable<PackagePlantSuitability>>())).Returns(Task.CompletedTask);

        var categoryRepository = new Mock<ICategoryRepository>(MockBehavior.Loose);
        categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Indoor" });

        var sut = CreateSut(CreateUnitOfWork(packageRepository, categoryRepository), new Mock<ICacheService>(MockBehavior.Loose));

        var result = await sut.CreateAsync(new CreateCareServicePackageRequestDto
        {
            Name = "B6",
            ServiceType = (int)CareServiceTypeEnum.Periodic,
            DurationDays = 30,
            VisitPerWeek = 6,
            UnitPrice = 100000,
            SuitabilityRules = new List<PackagePlantSuitabilityRuleRequestDto> { new() { CategoryId = 1 } }
        });

        result.VisitPerWeek.Should().Be(6);
    }

    [Fact]
    public async Task Abnormal_CreateAsync_ShouldThrow_WhenPackageNameAlreadyExists()
    {
        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.ExistsByNameAsync("Starter", null)).ReturnsAsync(true);

        var sut = CreateSut(
            CreateUnitOfWork(packageRepository, new Mock<ICategoryRepository>(MockBehavior.Loose)),
            new Mock<ICacheService>(MockBehavior.Loose));

        var act = () => sut.CreateAsync(BuildOneTimeRequest());

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("A package named 'Starter' already exists");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenSuitabilityRulesMissing()
    {
        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.ExistsByNameAsync("NoRules", null)).ReturnsAsync(false);

        var sut = CreateSut(
            CreateUnitOfWork(packageRepository, new Mock<ICategoryRepository>(MockBehavior.Loose)),
            new Mock<ICacheService>(MockBehavior.Loose));

        var act = () => sut.CreateAsync(new CreateCareServicePackageRequestDto
        {
            Name = "NoRules",
            ServiceType = (int)CareServiceTypeEnum.OneTime,
            DurationDays = 1,
            UnitPrice = 100000,
            SuitabilityRules = new List<PackagePlantSuitabilityRuleRequestDto>()
        });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("SuitabilityRules is required when creating a care service package");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenPeriodicVisitPerWeekMissing()
    {
        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.ExistsByNameAsync("PeriodicMissing", null)).ReturnsAsync(false);

        var sut = CreateSut(
            CreateUnitOfWork(packageRepository, new Mock<ICategoryRepository>(MockBehavior.Loose)),
            new Mock<ICacheService>(MockBehavior.Loose));

        var act = () => sut.CreateAsync(new CreateCareServicePackageRequestDto
        {
            Name = "PeriodicMissing",
            ServiceType = (int)CareServiceTypeEnum.Periodic,
            DurationDays = 30,
            VisitPerWeek = null,
            UnitPrice = 100000,
            SuitabilityRules = new List<PackagePlantSuitabilityRuleRequestDto>
            {
                new() { CategoryId = 1 }
            }
        });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("VisitPerWeek (1-6) is required for Periodic service packages");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenPeriodicVisitPerWeekOutOfRange()
    {
        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.ExistsByNameAsync("PeriodicOut", null)).ReturnsAsync(false);

        var sut = CreateSut(
            CreateUnitOfWork(packageRepository, new Mock<ICategoryRepository>(MockBehavior.Loose)),
            new Mock<ICacheService>(MockBehavior.Loose));

        var act = () => sut.CreateAsync(new CreateCareServicePackageRequestDto
        {
            Name = "PeriodicOut",
            ServiceType = (int)CareServiceTypeEnum.Periodic,
            DurationDays = 30,
            VisitPerWeek = 7,
            UnitPrice = 100000,
            SuitabilityRules = new List<PackagePlantSuitabilityRuleRequestDto>
            {
                new() { CategoryId = 1 }
            }
        });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("VisitPerWeek must be between 1 and 6");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFound_WhenPackageDoesNotExist()
    {
        var packageRepository = new Mock<ICareServicePackageRepository>(MockBehavior.Loose);
        packageRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CareServicePackage?)null);

        var sut = CreateSut(
            CreateUnitOfWork(packageRepository, new Mock<ICategoryRepository>(MockBehavior.Loose)),
            new Mock<ICacheService>(MockBehavior.Loose));

        var act = () => sut.UpdateAsync(999, new UpdateCareServicePackageRequestDto { Name = "x" });

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("CareServicePackage 999 not found");
    }
}