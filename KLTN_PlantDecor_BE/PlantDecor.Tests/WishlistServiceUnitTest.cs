using FluentAssertions;
using Moq;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.Tests;

public class WishlistServiceUnitTest
{
    private static WishlistService CreateSut(Mock<IUnitOfWork> uow, Mock<ICacheService> cache)
        => new(uow.Object, cache.Object);

    [Fact]
    public async Task AddToWishlistAsync_ShouldAddPlant_WhenValid_Normal()
    {
        const int userId = 1;
        const int plantId = 10;

        var plantRepo = new Mock<IPlantRepository>(MockBehavior.Strict);
        plantRepo.Setup(r => r.GetByIdAsync(plantId)).ReturnsAsync(new Plant { Id = plantId });

        var wishlistRepo = new Mock<IWishlistRepository>(MockBehavior.Strict);
        wishlistRepo.Setup(r => r.ExistsAsync(userId, WishlistItemType.Plant, plantId)).ReturnsAsync(false);
        wishlistRepo.Setup(r => r.CreateAsync(It.IsAny<Wishlist>())).ReturnsAsync(1);
        wishlistRepo.Setup(r => r.GetByUserAndItemAsync(userId, WishlistItemType.Plant, plantId))
            .ReturnsAsync(new Wishlist { Id = 1, UserId = userId, ItemType = WishlistItemType.Plant, PlantId = plantId });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.PlantRepository).Returns(plantRepo.Object);
        uow.SetupGet(x => x.WishlistRepository).Returns(wishlistRepo.Object);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(c => c.RemoveByPrefixAsync("wishlist_user_1")).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, cache);

        var result = await sut.AddToWishlistAsync(userId, WishlistItemType.Plant, plantId);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        cache.Verify(c => c.RemoveByPrefixAsync("wishlist_user_1"), Times.Once);
        wishlistRepo.Verify(r => r.CreateAsync(It.IsAny<Wishlist>()), Times.Once);
    }

    [Fact]
    public async Task AddToWishlistAsync_ShouldAddMaterial_WhenValid_Normal()
    {
        const int userId = 2;
        const int materialId = 20;

        var materialRepo = new Mock<IMaterialRepository>(MockBehavior.Strict);
        materialRepo.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(new Material { Id = materialId });

        Wishlist? createdEntity = null;
        var wishlistRepo = new Mock<IWishlistRepository>(MockBehavior.Strict);
        wishlistRepo.Setup(r => r.ExistsAsync(userId, WishlistItemType.Material, materialId)).ReturnsAsync(false);
        wishlistRepo.Setup(r => r.CreateAsync(It.IsAny<Wishlist>()))
            .Callback<Wishlist>(w => createdEntity = w)
            .ReturnsAsync(1);
        wishlistRepo.Setup(r => r.GetByUserAndItemAsync(userId, WishlistItemType.Material, materialId))
            .ReturnsAsync(new Wishlist { Id = 2, UserId = userId, ItemType = WishlistItemType.Material, MaterialId = materialId });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.MaterialRepository).Returns(materialRepo.Object);
        uow.SetupGet(x => x.WishlistRepository).Returns(wishlistRepo.Object);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(c => c.RemoveByPrefixAsync("wishlist_user_2")).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, cache);

        var _ = await sut.AddToWishlistAsync(userId, WishlistItemType.Material, materialId);

        createdEntity.Should().NotBeNull();
        createdEntity!.MaterialId.Should().Be(materialId);
        createdEntity.PlantId.Should().BeNull();
        createdEntity.PlantInstanceId.Should().BeNull();
        createdEntity.PlantComboId.Should().BeNull();
    }

    [Fact]
    public async Task AddToWishlistAsync_ShouldRemoveCachePrefix_Normal()
    {
        const int userId = 3;
        const int plantInstanceId = 30;

        var plantInstanceRepo = new Mock<IPlantInstanceRepository>(MockBehavior.Strict);
        plantInstanceRepo.Setup(r => r.GetByIdAsync(plantInstanceId)).ReturnsAsync(new PlantInstance { Id = plantInstanceId });

        var wishlistRepo = new Mock<IWishlistRepository>(MockBehavior.Strict);
        wishlistRepo.Setup(r => r.ExistsAsync(userId, WishlistItemType.PlantInstance, plantInstanceId)).ReturnsAsync(false);
        wishlistRepo.Setup(r => r.CreateAsync(It.IsAny<Wishlist>())).ReturnsAsync(1);
        wishlistRepo.Setup(r => r.GetByUserAndItemAsync(userId, WishlistItemType.PlantInstance, plantInstanceId))
            .ReturnsAsync(new Wishlist { Id = 3, UserId = userId, ItemType = WishlistItemType.PlantInstance, PlantInstanceId = plantInstanceId });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.PlantInstanceRepository).Returns(plantInstanceRepo.Object);
        uow.SetupGet(x => x.WishlistRepository).Returns(wishlistRepo.Object);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(c => c.RemoveByPrefixAsync("wishlist_user_3")).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, cache);

        var _ = await sut.AddToWishlistAsync(userId, WishlistItemType.PlantInstance, plantInstanceId);

        cache.Verify(c => c.RemoveByPrefixAsync("wishlist_user_3"), Times.Once);
    }

    [Fact]
    public async Task AddToWishlistAsync_ShouldThrowBadRequest_WhenAlreadyExists_Boundary()
    {
        const int userId = 4;
        const int plantId = 40;

        var plantRepo = new Mock<IPlantRepository>(MockBehavior.Strict);
        plantRepo.Setup(r => r.GetByIdAsync(plantId)).ReturnsAsync(new Plant { Id = plantId });

        var wishlistRepo = new Mock<IWishlistRepository>(MockBehavior.Strict);
        wishlistRepo.Setup(r => r.ExistsAsync(userId, WishlistItemType.Plant, plantId)).ReturnsAsync(true);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.PlantRepository).Returns(plantRepo.Object);
        uow.SetupGet(x => x.WishlistRepository).Returns(wishlistRepo.Object);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, cache);

        var act = () => sut.AddToWishlistAsync(userId, WishlistItemType.Plant, plantId);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Plant already existed in wishlist");
    }

    [Fact]
    public async Task AddToWishlistAsync_ShouldThrowBadRequest_WhenInvalidItemType_Boundary()
    {
        const int userId = 5;
        const int itemId = 50;

        var wishlistRepo = new Mock<IWishlistRepository>(MockBehavior.Strict);
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.WishlistRepository).Returns(wishlistRepo.Object);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, cache);

        var act = () => sut.AddToWishlistAsync(userId, (WishlistItemType)999, itemId);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid item type: 999");
    }

    [Fact]
    public async Task AddToWishlistAsync_ShouldThrowNotFound_WhenItemDoesNotExist_Abnormal()
    {
        const int userId = 6;
        const int plantComboId = 60;

        var comboRepo = new Mock<IPlantComboRepository>(MockBehavior.Strict);
        comboRepo.Setup(r => r.GetByIdAsync(plantComboId)).ReturnsAsync((PlantCombo?)null);

        var wishlistRepo = new Mock<IWishlistRepository>(MockBehavior.Strict);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.PlantComboRepository).Returns(comboRepo.Object);
        uow.SetupGet(x => x.WishlistRepository).Returns(wishlistRepo.Object);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, cache);

        var act = () => sut.AddToWishlistAsync(userId, WishlistItemType.PlantCombo, plantComboId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("PlantCombo with ID 60 not exists");
    }
}

