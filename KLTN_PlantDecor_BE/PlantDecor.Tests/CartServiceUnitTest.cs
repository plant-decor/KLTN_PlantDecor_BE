using FluentAssertions;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.Tests;

public class CartServiceUnitTest
{
    private static CartService CreateSut(Mock<IUnitOfWork> uow, Mock<ICacheService> cache)
        => new(uow.Object, cache.Object);

    [Fact]
    public async Task AddItemAsync_ShouldCreateNewCartItem_WhenNotExists_Normal()
    {
        const int userId = 1;
        var request = new CartItemRequestDto { CommonPlantId = 10, Quantity = 1 };

        var commonPlant = new CommonPlant
        {
            Id = 10,
            IsActive = true,
            Quantity = 5,
            Plant = new Plant { BasePrice = 100m }
        };

        var cart = new Cart { Id = 50, UserId = userId, CartItems = new List<CartItem>() };

        var commonPlantRepo = new Mock<ICommonPlantRepository>(MockBehavior.Strict);
        commonPlantRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(commonPlant);

        var cartRepo = new Mock<ICartRepository>(MockBehavior.Strict);
        cartRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(cart);
        cartRepo.Setup(r => r.GetCartItemByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(() => cart.CartItems.FirstOrDefault());

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.CommonPlantRepository).Returns(commonPlantRepo.Object);
        uow.SetupGet(x => x.CartRepository).Returns(cartRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(c => c.RemoveByPrefixAsync("cart_user_1")).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, cache);

        var result = await sut.AddItemAsync(userId, request);

        cart.CartItems.Should().HaveCount(1);
        cart.CartItems.First().CommonPlantId.Should().Be(10);
        cart.CartItems.First().Quantity.Should().Be(1);
        cart.CartItems.First().Price.Should().Be(100m);

        cache.Verify(c => c.RemoveByPrefixAsync("cart_user_1"), Times.Once);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AddItemAsync_ShouldIncreaseQuantity_WhenItemExists_Normal()
    {
        const int userId = 2;
        var request = new CartItemRequestDto { CommonPlantId = 10, Quantity = 2 };

        var commonPlant = new CommonPlant
        {
            Id = 10,
            IsActive = true,
            Quantity = 10,
            Plant = new Plant { BasePrice = 50m }
        };

        var existingItem = new CartItem { Id = 1, CommonPlantId = 10, Quantity = 1, Price = 10m };
        var cart = new Cart { Id = 51, UserId = userId, CartItems = new List<CartItem> { existingItem } };

        var commonPlantRepo = new Mock<ICommonPlantRepository>(MockBehavior.Strict);
        commonPlantRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(commonPlant);

        var cartRepo = new Mock<ICartRepository>(MockBehavior.Strict);
        cartRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(cart);
        cartRepo.Setup(r => r.PrepareUpdate(cart));
        cartRepo.Setup(r => r.GetCartItemByIdAsync(existingItem.Id)).ReturnsAsync(existingItem);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.CommonPlantRepository).Returns(commonPlantRepo.Object);
        uow.SetupGet(x => x.CartRepository).Returns(cartRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(c => c.RemoveByPrefixAsync("cart_user_2")).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, cache);

        var _ = await sut.AddItemAsync(userId, request);

        existingItem.Quantity.Should().Be(3);
        existingItem.Price.Should().Be(50m); // updated price from backend
        cartRepo.Verify(r => r.PrepareUpdate(cart), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_ShouldCreateCart_WhenUserHasNoCart_Normal()
    {
        const int userId = 3;
        var request = new CartItemRequestDto { CommonPlantId = 10, Quantity = 1 };

        var commonPlant = new CommonPlant
        {
            Id = 10,
            IsActive = true,
            Quantity = 5,
            Plant = new Plant { BasePrice = 20m }
        };

        Cart? createdCart = null;
        var cartAfterCreate = new Cart { Id = 70, UserId = userId, CartItems = new List<CartItem>() };

        var commonPlantRepo = new Mock<ICommonPlantRepository>(MockBehavior.Strict);
        commonPlantRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(commonPlant);

        var cartRepo = new Mock<ICartRepository>(MockBehavior.Strict);
        cartRepo.SetupSequence(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((Cart?)null)        // first call => no cart
            .ReturnsAsync(cartAfterCreate);   // after create
        cartRepo.Setup(r => r.CreateAsync(It.IsAny<Cart>()))
            .Callback<Cart>(c => createdCart = c)
            .ReturnsAsync(1);
        cartRepo.Setup(r => r.GetCartItemByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(() => cartAfterCreate.CartItems.FirstOrDefault());

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.CommonPlantRepository).Returns(commonPlantRepo.Object);
        uow.SetupGet(x => x.CartRepository).Returns(cartRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(c => c.RemoveByPrefixAsync("cart_user_3")).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, cache);

        var _ = await sut.AddItemAsync(userId, request);

        createdCart.Should().NotBeNull();
        createdCart!.UserId.Should().Be(userId);
        cartRepo.Verify(r => r.CreateAsync(It.IsAny<Cart>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_ShouldAllowQuantityEqualsStock_Boundary()
    {
        const int userId = 4;
        var request = new CartItemRequestDto { CommonPlantId = 10, Quantity = 5 };

        var commonPlant = new CommonPlant
        {
            Id = 10,
            IsActive = true,
            Quantity = 5,
            Plant = new Plant { BasePrice = 100m }
        };

        var cart = new Cart { Id = 80, UserId = userId, CartItems = new List<CartItem>() };

        var commonPlantRepo = new Mock<ICommonPlantRepository>(MockBehavior.Strict);
        commonPlantRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(commonPlant);

        var cartRepo = new Mock<ICartRepository>(MockBehavior.Strict);
        cartRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(cart);
        cartRepo.Setup(r => r.GetCartItemByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(() => cart.CartItems.FirstOrDefault());

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.CommonPlantRepository).Returns(commonPlantRepo.Object);
        uow.SetupGet(x => x.CartRepository).Returns(cartRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        cache.Setup(c => c.RemoveByPrefixAsync("cart_user_4")).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, cache);

        var _ = await sut.AddItemAsync(userId, request);

        cart.CartItems.Should().ContainSingle(i => i.Quantity == 5);
    }

    [Fact]
    public async Task AddItemAsync_ShouldThrowBadRequest_WhenNoProductTypeChosen_Boundary()
    {
        const int userId = 5;
        var request = new CartItemRequestDto { Quantity = 1 };

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, cache);

        var act = () => sut.AddItemAsync(userId, request);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Must choose 1 of the following types (CommonPlantId, NurseryPlantComboId hoặc NurseryMaterialId)");
    }

    [Fact]
    public async Task AddItemAsync_ShouldThrowNotFound_WhenCommonPlantNotExists_Abnormal()
    {
        const int userId = 6;
        var request = new CartItemRequestDto { CommonPlantId = 999, Quantity = 1 };

        var commonPlantRepo = new Mock<ICommonPlantRepository>(MockBehavior.Strict);
        commonPlantRepo.Setup(r => r.GetByIdWithDetailsAsync(999)).ReturnsAsync((CommonPlant?)null);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.CommonPlantRepository).Returns(commonPlantRepo.Object);

        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        var sut = CreateSut(uow, cache);

        var act = () => sut.AddItemAsync(userId, request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("CommonPlant 999 not exists or has been discontinued");
    }
}

