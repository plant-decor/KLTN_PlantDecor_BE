using FluentAssertions;
using Hangfire;
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

public class OrderServiceUnitTest
{
    private static OrderService CreateSut(
        Mock<IUnitOfWork> uow,
        Mock<IBackgroundJobClient> backgroundJobClient,
        Mock<ICacheService> cacheService)
    {
        return new OrderService(uow.Object, backgroundJobClient.Object, cacheService.Object);
    }

    private static CreateOrderRequestDto CreateBaseRequest(int orderType, int paymentStrategy)
    {
        return new CreateOrderRequestDto
        {
            OrderType = orderType,
            PaymentStrategy = paymentStrategy,
            Address = "123 Test Street",
            Phone = "0900000000",
            CustomerName = "Test Customer",
            Note = "note"
        };
    }

    private static Order CreateHydratedOrder(
        int orderId,
        int userId,
        int nurseryId,
        int orderType,
        int paymentStrategy,
        decimal unitPrice,
        int quantity)
    {
        var detail = new NurseryOrderDetail
        {
            Id = 1,
            ItemName = "Item",
            Quantity = quantity,
            UnitPrice = unitPrice,
            Amount = unitPrice * quantity,
            Status = (int)OrderStatusEnum.Pending
        };

        var nurseryOrder = new NurseryOrder
        {
            Id = 1,
            OrderId = orderId,
            NurseryId = nurseryId,
            SubTotalAmount = unitPrice * quantity,
            PaymentStrategy = paymentStrategy,
            Status = (int)OrderStatusEnum.Pending,
            NurseryOrderDetails = new List<NurseryOrderDetail> { detail }
        };

        var invoice = new Invoice
        {
            Id = 1,
            OrderId = orderId,
            Type = (int)InvoiceTypeEnum.FullPayment,
            TotalAmount = unitPrice * quantity,
            Status = (int)InvoiceStatusEnum.Pending,
            IssuedDate = DateTime.Now,
            InvoiceDetails = new List<InvoiceDetail>
            {
                new()
                {
                    Id = 1,
                    ItemName = "Item",
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    Amount = unitPrice * quantity
                }
            }
        };

        return new Order
        {
            Id = orderId,
            UserId = userId,
            Address = "123 Test Street",
            Phone = "0900000000",
            CustomerName = "Test Customer",
            OrderType = orderType,
            PaymentStrategy = paymentStrategy,
            Status = (int)OrderStatusEnum.Pending,
            TotalAmount = unitPrice * quantity,
            NurseryOrders = new List<NurseryOrder> { nurseryOrder },
            Invoices = new List<Invoice> { invoice }
        };
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCreatePlantInstanceOrder_FullPayment_Normal()
    {
        const int userId = 7;
        var request = CreateBaseRequest(orderType: (int)OrderTypeEnum.PlantInstance, paymentStrategy: (int)PaymentStrategiesEnum.FullPayment);
        request.PlantInstanceId = 99;

        var plantInstance = new PlantInstance
        {
            Id = 99,
            CurrentNurseryId = 5,
            Status = (int)PlantInstanceStatusEnum.Available,
            SpecificPrice = 100m,
            Plant = new Plant { Name = "Monstera" }
        };

        var hydrated = CreateHydratedOrder(orderId: 123, userId: userId, nurseryId: 5,
            orderType: request.OrderType, paymentStrategy: request.PaymentStrategy,
            unitPrice: 100m, quantity: 1);

        var plantInstanceRepo = new Mock<IPlantInstanceRepository>(MockBehavior.Strict);
        plantInstanceRepo.Setup(r => r.GetByIdWithDetailsAsync(99)).ReturnsAsync(plantInstance);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.PrepareCreate(It.IsAny<Order>()))
            .Callback<Order>(o => o.Id = 123);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(123)).ReturnsAsync(hydrated);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.PlantInstanceRepository).Returns(plantInstanceRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var backgroundJobClient = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var cacheService = new Mock<ICacheService>(MockBehavior.Strict);

        var sut = CreateSut(uow, backgroundJobClient, cacheService);

        var result = await sut.CreateOrderAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().Be(123);
        result.UserId.Should().Be(userId);
        result.OrderType.Should().Be((int)OrderTypeEnum.PlantInstance);
        result.PaymentStrategy.Should().Be((int)PaymentStrategiesEnum.FullPayment);
        result.NurseryOrders.Should().HaveCount(1);

        orderRepo.Verify(r => r.PrepareCreate(It.IsAny<Order>()), Times.Once);
        uow.Verify(x => x.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCreateOtherProductBuyNow_CommonPlant_Normal()
    {
        const int userId = 7;
        var request = CreateBaseRequest(orderType: (int)OrderTypeEnum.OtherProductBuyNow, paymentStrategy: (int)PaymentStrategiesEnum.FullPayment);
        request.BuyNowItemType = (int)BuyNowItemTypeEnum.CommonPlant;
        request.BuyNowItemId = 10;
        request.BuyNowQuantity = 2;

        var commonPlant = new CommonPlant
        {
            Id = 10,
            NurseryId = 3,
            IsActive = true,
            Quantity = 10,
            Plant = new Plant { Name = "Aloe", BasePrice = 50m }
        };

        var hydrated = CreateHydratedOrder(orderId: 200, userId: userId, nurseryId: 3,
            orderType: request.OrderType, paymentStrategy: request.PaymentStrategy,
            unitPrice: 50m, quantity: 2);

        var commonPlantRepo = new Mock<ICommonPlantRepository>(MockBehavior.Strict);
        commonPlantRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(commonPlant);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.PrepareCreate(It.IsAny<Order>()))
            .Callback<Order>(o => o.Id = 200);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(200)).ReturnsAsync(hydrated);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.CommonPlantRepository).Returns(commonPlantRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var backgroundJobClient = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var cacheService = new Mock<ICacheService>(MockBehavior.Strict);

        var sut = CreateSut(uow, backgroundJobClient, cacheService);

        var result = await sut.CreateOrderAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().Be(200);
        result.NurseryOrders.Should().HaveCount(1);
        result.Items.Should().HaveCount(1);
        result.TotalAmount.Should().Be(100m);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCheckoutSelectedCartItems_RemoveOnlySelected_AndInvalidateCache_Normal()
    {
        const int userId = 7;
        var request = CreateBaseRequest(orderType: (int)OrderTypeEnum.OtherProduct, paymentStrategy: (int)PaymentStrategiesEnum.FullPayment);
        request.CartItemIds = new List<int> { 1 };

        var cart = new Cart
        {
            Id = 500,
            UserId = userId,
            CartItems = new List<CartItem>
            {
                new()
                {
                    Id = 1,
                    Quantity = 1,
                    CommonPlant = new CommonPlant
                    {
                        Id = 10,
                        NurseryId = 3,
                        Plant = new Plant { Name = "Aloe", BasePrice = 50m }
                    }
                },
                new()
                {
                    Id = 2,
                    Quantity = 2,
                    CommonPlant = new CommonPlant
                    {
                        Id = 11,
                        NurseryId = 3,
                        Plant = new Plant { Name = "Cactus", BasePrice = 20m }
                    }
                }
            }
        };

        var hydrated = CreateHydratedOrder(orderId: 300, userId: userId, nurseryId: 3,
            orderType: request.OrderType, paymentStrategy: request.PaymentStrategy,
            unitPrice: 50m, quantity: 1);

        var cartRepo = new Mock<ICartRepository>(MockBehavior.Strict);
        cartRepo.SetupSequence(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart) // for building order items
            .ReturnsAsync(cart); // for clearing items after save
        cartRepo.Setup(r => r.RemoveCartItemAsync(It.Is<CartItem>(ci => ci.Id == 1))).ReturnsAsync(true);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.PrepareCreate(It.IsAny<Order>()))
            .Callback<Order>(o => o.Id = 300);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(300)).ReturnsAsync(hydrated);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.CartRepository).Returns(cartRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var backgroundJobClient = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var cacheService = new Mock<ICacheService>(MockBehavior.Strict);
        cacheService.Setup(c => c.RemoveByPrefixAsync($"cart_user_{userId}")).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, backgroundJobClient, cacheService);

        var result = await sut.CreateOrderAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().Be(300);

        cartRepo.Verify(r => r.RemoveCartItemAsync(It.Is<CartItem>(ci => ci.Id == 1)), Times.Once);
        cartRepo.Verify(r => r.ClearCartItemsAsync(It.IsAny<int>()), Times.Never);
        cacheService.Verify(c => c.RemoveByPrefixAsync($"cart_user_{userId}"), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldSucceed_WhenBuyNowQuantityIsOne_Minimum_Boundary()
    {
        const int userId = 7;
        var request = CreateBaseRequest(orderType: (int)OrderTypeEnum.OtherProductBuyNow, paymentStrategy: (int)PaymentStrategiesEnum.FullPayment);
        request.BuyNowItemType = (int)BuyNowItemTypeEnum.CommonPlant;
        request.BuyNowItemId = 10;
        request.BuyNowQuantity = 1;

        var commonPlant = new CommonPlant
        {
            Id = 10,
            NurseryId = 3,
            IsActive = true,
            Quantity = 1, // boundary: just enough stock
            Plant = new Plant { Name = "Aloe", BasePrice = 50m }
        };

        var hydrated = CreateHydratedOrder(orderId: 201, userId: userId, nurseryId: 3,
            orderType: request.OrderType, paymentStrategy: request.PaymentStrategy,
            unitPrice: 50m, quantity: 1);

        var commonPlantRepo = new Mock<ICommonPlantRepository>(MockBehavior.Strict);
        commonPlantRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(commonPlant);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.PrepareCreate(It.IsAny<Order>()))
            .Callback<Order>(o => o.Id = 201);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(201)).ReturnsAsync(hydrated);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.CommonPlantRepository).Returns(commonPlantRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var backgroundJobClient = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var cacheService = new Mock<ICacheService>(MockBehavior.Strict);

        var sut = CreateSut(uow, backgroundJobClient, cacheService);

        var result = await sut.CreateOrderAsync(userId, request);

        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(50m);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCheckoutAllCartItems_WhenCartItemIdsNull_Boundary()
    {
        const int userId = 7;
        var request = CreateBaseRequest(orderType: (int)OrderTypeEnum.OtherProduct, paymentStrategy: (int)PaymentStrategiesEnum.FullPayment);
        request.CartItemIds = null; // boundary: null => checkout all

        var cart = new Cart
        {
            Id = 501,
            UserId = userId,
            CartItems = new List<CartItem>
            {
                new()
                {
                    Id = 1,
                    Quantity = 1,
                    CommonPlant = new CommonPlant
                    {
                        Id = 10,
                        NurseryId = 3,
                        Plant = new Plant { Name = "Aloe", BasePrice = 50m }
                    }
                }
            }
        };

        var hydrated = CreateHydratedOrder(orderId: 301, userId: userId, nurseryId: 3,
            orderType: request.OrderType, paymentStrategy: request.PaymentStrategy,
            unitPrice: 50m, quantity: 1);

        var cartRepo = new Mock<ICartRepository>(MockBehavior.Strict);
        cartRepo.SetupSequence(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart)
            .ReturnsAsync(cart);
        cartRepo.Setup(r => r.ClearCartItemsAsync(501)).ReturnsAsync(1);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.PrepareCreate(It.IsAny<Order>()))
            .Callback<Order>(o => o.Id = 301);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(301)).ReturnsAsync(hydrated);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.CartRepository).Returns(cartRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var backgroundJobClient = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var cacheService = new Mock<ICacheService>(MockBehavior.Strict);
        cacheService.Setup(c => c.RemoveByPrefixAsync($"cart_user_{userId}")).Returns(Task.CompletedTask);

        var sut = CreateSut(uow, backgroundJobClient, cacheService);

        var result = await sut.CreateOrderAsync(userId, request);

        result.Should().NotBeNull();
        cartRepo.Verify(r => r.ClearCartItemsAsync(501), Times.Once);
        cacheService.Verify(c => c.RemoveByPrefixAsync($"cart_user_{userId}"), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldNotClearCart_WhenSaveAsyncThrows_Abnormal()
    {
        const int userId = 7;
        var request = CreateBaseRequest(orderType: (int)OrderTypeEnum.OtherProduct, paymentStrategy: (int)PaymentStrategiesEnum.FullPayment);
        request.CartItemIds = null;

        var cart = new Cart
        {
            Id = 600,
            UserId = userId,
            CartItems = new List<CartItem>
            {
                new()
                {
                    Id = 1,
                    Quantity = 1,
                    CommonPlant = new CommonPlant
                    {
                        Id = 10,
                        NurseryId = 3,
                        Plant = new Plant { Name = "Aloe", BasePrice = 50m }
                    }
                }
            }
        };

        var cartRepo = new Mock<ICartRepository>(MockBehavior.Strict);
        cartRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(cart);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.PrepareCreate(It.IsAny<Order>()))
            .Callback<Order>(o => o.Id = 400);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.CartRepository).Returns(cartRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ThrowsAsync(new Exception("DB down"));

        var backgroundJobClient = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var cacheService = new Mock<ICacheService>(MockBehavior.Strict);

        var sut = CreateSut(uow, backgroundJobClient, cacheService);

        var act = () => sut.CreateOrderAsync(userId, request);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("DB down");

        cartRepo.Verify(r => r.ClearCartItemsAsync(It.IsAny<int>()), Times.Never);
        cartRepo.Verify(r => r.RemoveCartItemAsync(It.IsAny<CartItem>()), Times.Never);
        cacheService.Verify(c => c.RemoveByPrefixAsync(It.IsAny<string>()), Times.Never);
    }
}

