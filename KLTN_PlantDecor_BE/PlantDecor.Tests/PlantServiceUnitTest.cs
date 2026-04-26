using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.Tests;

public class PlantServiceUnitTest
{
    private static PlantRequestDto BuildValidCreateRequest() => new()
    {
        Name = "Monstera",
        Description = "Indoor plant",
        BasePrice = 250000,
        PlacementType = (int)PlacementTypeEnum.Indoor,
        RoomStyle = new List<int> { (int)RoomStyleEnum.Minimalist },
        RoomType = new List<int> { (int)RoomTypeEnum.LivingRoom },
        CareLevelType = 2,
        IsActive = true
    };

    private static Plant BuildExistingPlant(int id = 22) => new()
    {
        Id = id,
        Name = "Old name",
        PlacementType = (int)PlacementTypeEnum.Indoor,
        IsActive = true
    };

    private static PlantService CreateSut(
        Mock<IUnitOfWork> unitOfWork,
        Mock<ICacheService> cacheService)
    {
        return new PlantService(
            unitOfWork.Object,
            cacheService.Object,
            Mock.Of<ICloudinaryService>(),
            Mock.Of<ILogger<PlantService>>());
    }

    private static Mock<IUnitOfWork> CreateUnitOfWork(Mock<IPlantRepository> plantRepository)
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);
        unitOfWork.SetupGet(x => x.PlantRepository).Returns(plantRepository.Object);
        unitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);
        return unitOfWork;
    }

    [Fact]
    public async Task Normal_CreatePlantAsync_ShouldCreatePlant_WhenRequestIsValid()
    {
        var plantRepository = new Mock<IPlantRepository>(MockBehavior.Loose);
        plantRepository.Setup(r => r.ExistsByNameAsync("Monstera", null))
            .ReturnsAsync(false);
        plantRepository.Setup(r => r.PrepareCreate(It.IsAny<Plant>()))
            .Callback<Plant>(plant => plant.Id = 11);

        var unitOfWork = CreateUnitOfWork(plantRepository);
        var cacheService = new Mock<ICacheService>(MockBehavior.Loose);
        cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = CreateSut(unitOfWork, cacheService);

        var request = BuildValidCreateRequest();

        var result = await sut.CreatePlantAsync(request);

        result.Id.Should().Be(11);
        result.Name.Should().Be("Monstera");
        result.BasePrice.Should().Be(250000);
        result.PlacementType.Should().Be((int)PlacementTypeEnum.Indoor);
        plantRepository.Verify(r => r.PrepareCreate(It.IsAny<Plant>()), Times.Once);
        unitOfWork.Verify(x => x.SaveAsync(), Times.Once);
        unitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Normal_UpdatePlantAsync_ShouldUpdatePlant_WhenRequestIsValid()
    {
        var existingPlant = BuildExistingPlant();

        var plantRepository = new Mock<IPlantRepository>(MockBehavior.Loose);
        plantRepository.Setup(r => r.GetByIdWithDetailsAsync(22)).ReturnsAsync(existingPlant);
        plantRepository.Setup(r => r.ExistsByNameAsync("New name", 22)).ReturnsAsync(false);
        plantRepository.Setup(r => r.PrepareUpdate(It.IsAny<Plant>()));

        var unitOfWork = CreateUnitOfWork(plantRepository);
        var cacheService = new Mock<ICacheService>(MockBehavior.Loose);
        cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = CreateSut(unitOfWork, cacheService);

        var request = new PlantUpdateDto
        {
            Name = "New name",
            Description = "Updated description",
            BasePrice = 500000,
            RoomStyle = new List<int> { (int)RoomStyleEnum.Scandinavian },
            RoomType = new List<int> { (int)RoomTypeEnum.Balcony },
            IsUniqueInstance = true
        };

        var result = await sut.UpdatePlantAsync(22, request);

        result.Name.Should().Be("New name");
        result.Description.Should().Be("Updated description");
        result.BasePrice.Should().Be(500000);
        result.IsUniqueInstance.Should().BeTrue();
        plantRepository.Verify(r => r.PrepareUpdate(existingPlant), Times.Once);
        unitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Normal_UpdatePlantAsync_ShouldUpdatePartialFields_WhenNameIsNull()
    {
        var existingPlant = BuildExistingPlant(23);

        var plantRepository = new Mock<IPlantRepository>(MockBehavior.Loose);
        plantRepository.Setup(r => r.GetByIdWithDetailsAsync(23)).ReturnsAsync(existingPlant);
        plantRepository.Setup(r => r.PrepareUpdate(It.IsAny<Plant>()));

        var unitOfWork = CreateUnitOfWork(plantRepository);
        var cacheService = new Mock<ICacheService>(MockBehavior.Loose);
        cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = CreateSut(unitOfWork, cacheService);

        var result = await sut.UpdatePlantAsync(23, new PlantUpdateDto
        {
            Name = null,
            Description = "Only description changed"
        });

        result.Name.Should().Be("Old name");
        result.Description.Should().Be("Only description changed");
    }

    [Fact]
    public async Task Boundary_CreatePlantAsync_ShouldAllowEmptyEnumLists()
    {
        var plantRepository = new Mock<IPlantRepository>(MockBehavior.Loose);
        plantRepository.Setup(r => r.ExistsByNameAsync("Monstera", null)).ReturnsAsync(false);
        plantRepository.Setup(r => r.PrepareCreate(It.IsAny<Plant>()))
            .Callback<Plant>(plant => plant.Id = 24);

        var unitOfWork = CreateUnitOfWork(plantRepository);
        var cacheService = new Mock<ICacheService>(MockBehavior.Loose);
        cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = CreateSut(unitOfWork, cacheService);
        var request = BuildValidCreateRequest();
        request.RoomStyle = new List<int>();
        request.RoomType = new List<int>();

        var result = await sut.CreatePlantAsync(request);

        result.Id.Should().Be(24);
        result.RoomStyle.Should().NotBeNull();
        result.RoomType.Should().NotBeNull();
    }

    [Fact]
    public async Task Boundary_UpdatePlantAsync_ShouldAllowEmptyRoomStyleAndRoomType()
    {
        var existingPlant = BuildExistingPlant(25);

        var plantRepository = new Mock<IPlantRepository>(MockBehavior.Loose);
        plantRepository.Setup(r => r.GetByIdWithDetailsAsync(25)).ReturnsAsync(existingPlant);

        var unitOfWork = CreateUnitOfWork(plantRepository);
        var cacheService = new Mock<ICacheService>(MockBehavior.Loose);
        cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var sut = CreateSut(unitOfWork, cacheService);

        var result = await sut.UpdatePlantAsync(25, new PlantUpdateDto
        {
            RoomStyle = new List<int>(),
            RoomType = new List<int>()
        });

        result.RoomStyle.Should().NotBeNull();
        result.RoomType.Should().NotBeNull();
    }

    [Fact]
    public async Task Abnormal_CreatePlantAsync_ShouldThrowBadRequest_WhenNameAlreadyExists()
    {
        var plantRepository = new Mock<IPlantRepository>(MockBehavior.Loose);
        plantRepository.Setup(r => r.ExistsByNameAsync("Monstera", null)).ReturnsAsync(true);

        var unitOfWork = CreateUnitOfWork(plantRepository);
        var sut = CreateSut(unitOfWork, new Mock<ICacheService>(MockBehavior.Loose));

        var act = () => sut.CreatePlantAsync(BuildValidCreateRequest());

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Plant với tên 'Monstera' đã tồn tại");
    }

    [Fact]
    public async Task Abnormal_UpdatePlantAsync_ShouldThrowNotFound_WhenPlantDoesNotExist()
    {
        var plantRepository = new Mock<IPlantRepository>(MockBehavior.Loose);
        plantRepository.Setup(r => r.GetByIdWithDetailsAsync(999)).ReturnsAsync((Plant?)null);

        var unitOfWork = CreateUnitOfWork(plantRepository);
        var sut = CreateSut(unitOfWork, new Mock<ICacheService>(MockBehavior.Loose));

        var act = () => sut.UpdatePlantAsync(999, new PlantUpdateDto { Description = "x" });

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Plant với ID 999 không tồn tại");
    }

    [Fact]
    public async Task CreatePlantAsync_ShouldThrowBadRequest_WhenRoomStyleContainsInvalidEnum()
    {
        var plantRepository = new Mock<IPlantRepository>(MockBehavior.Loose);
        plantRepository.Setup(r => r.ExistsByNameAsync("Monstera", null)).ReturnsAsync(false);

        var sut = CreateSut(CreateUnitOfWork(plantRepository), new Mock<ICacheService>(MockBehavior.Loose));
        var request = BuildValidCreateRequest();
        request.RoomStyle = new List<int> { 999 };

        var act = () => sut.CreatePlantAsync(request);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("RoomStyle không hợp lệ: 999");
    }

    [Fact]
    public async Task UpdatePlantAsync_ShouldThrowBadRequest_WhenNameAlreadyExists()
    {
        var existingPlant = BuildExistingPlant(30);
        var plantRepository = new Mock<IPlantRepository>(MockBehavior.Loose);
        plantRepository.Setup(r => r.GetByIdWithDetailsAsync(30)).ReturnsAsync(existingPlant);
        plantRepository.Setup(r => r.ExistsByNameAsync("Duplicated", 30)).ReturnsAsync(true);

        var sut = CreateSut(CreateUnitOfWork(plantRepository), new Mock<ICacheService>(MockBehavior.Loose));

        var act = () => sut.UpdatePlantAsync(30, new PlantUpdateDto { Name = "Duplicated" });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Plant với tên 'Duplicated' đã tồn tại");
    }

    [Fact]
    public async Task UpdatePlantAsync_ShouldThrowBadRequest_WhenRoomTypeContainsInvalidEnum()
    {
        var existingPlant = BuildExistingPlant(31);
        var plantRepository = new Mock<IPlantRepository>(MockBehavior.Loose);
        plantRepository.Setup(r => r.GetByIdWithDetailsAsync(31)).ReturnsAsync(existingPlant);

        var sut = CreateSut(CreateUnitOfWork(plantRepository), new Mock<ICacheService>(MockBehavior.Loose));

        var act = () => sut.UpdatePlantAsync(31, new PlantUpdateDto { RoomType = new List<int> { 999 } });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("RoomType không hợp lệ: 999");
    }
}