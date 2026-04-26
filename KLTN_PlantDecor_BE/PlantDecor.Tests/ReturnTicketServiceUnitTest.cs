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

public class ReturnTicketServiceUnitTest
{
    private static ReturnTicketService CreateSut(Mock<IUnitOfWork> uow)
    {
        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);
        return new ReturnTicketService(uow.Object, cloudinary.Object);
    }

    private static Order BuildOrderForReturn(int orderId, int customerId, OrderStatusEnum status, params NurseryOrderDetail[] details)
    {
        var nurseryOrder = new NurseryOrder
        {
            Id = 1,
            OrderId = orderId,
            NurseryId = 1,
            Status = (int)status,
            NurseryOrderDetails = details.ToList(),
            Order = new Order { Id = orderId, UserId = customerId }
        };

        foreach (var d in details)
        {
            d.NurseryOrderId = nurseryOrder.Id;
            d.NurseryOrder = nurseryOrder;
        }

        return new Order
        {
            Id = orderId,
            UserId = customerId,
            Status = (int)status,
            NurseryOrders = new List<NurseryOrder> { nurseryOrder }
        };
    }

    private static NurseryOrderDetail Detail(int id, int quantity, OrderStatusEnum detailStatus = OrderStatusEnum.Delivered)
        => new()
        {
            Id = id,
            ItemName = $"Item-{id}",
            Quantity = quantity,
            Status = (int)detailStatus
        };

    [Fact]
    public async Task CreateReturnTicketAsync_ShouldCreateTicket_AndMarkDetailsRefundRequested_Normal()
    {
        const int customerId = 10;
        const int orderId = 100;
        var d1 = Detail(1, quantity: 2, detailStatus: OrderStatusEnum.Delivered);
        var order = BuildOrderForReturn(orderId, customerId, OrderStatusEnum.PendingConfirmation, d1);

        var request = new CreateReturnTicketRequestDto
        {
            OrderId = orderId,
            Reason = "broken",
            Items = new List<CreateReturnTicketItemRequestDto>
            {
                new() { NurseryOrderDetailId = 1, RequestedQuantity = 1, Reason = "leaf damaged" }
            }
        };

        ReturnTicket? createdTicket = null;

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(order);
        orderRepo.Setup(r => r.PrepareUpdate(order));

        var returnRepo = new Mock<IReturnTicketRepository>(MockBehavior.Strict);
        returnRepo.Setup(r => r.PrepareCreate(It.IsAny<ReturnTicket>()))
            .Callback<ReturnTicket>(t => createdTicket = t);

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>()); // no managers found => assignment ManagerId null

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.ReturnTicketRepository).Returns(returnRepo.Object);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow);

        var result = await sut.CreateReturnTicketAsync(customerId, request);

        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.CustomerId.Should().Be(customerId);
        result.Status.Should().Be((int)ReturnTicketStatusEnum.Pending);
        result.Items.Should().HaveCount(1);

        d1.Status.Should().Be((int)OrderStatusEnum.RefundRequested);
        createdTicket.Should().NotBeNull();
        createdTicket!.ReturnTicketItems.Should().HaveCount(1);
        createdTicket.ReturnTicketAssignments.Should().HaveCount(1); // 1 nursery => 1 assignment even without manager

        uow.Verify(x => x.SaveAsync(), Times.Once);
        returnRepo.Verify(r => r.PrepareCreate(It.IsAny<ReturnTicket>()), Times.Once);
        orderRepo.Verify(r => r.PrepareUpdate(order), Times.Once);
    }

    [Fact]
    public async Task CreateReturnTicketAsync_ShouldAssignManager_WhenManagerExistsForNursery_Normal()
    {
        const int customerId = 11;
        const int orderId = 101;
        var d1 = Detail(10, quantity: 1);
        var order = BuildOrderForReturn(orderId, customerId, OrderStatusEnum.PendingConfirmation, d1);

        // ensure nursery id = 1
        order.NurseryOrders.First().NurseryId = 9;
        d1.NurseryOrder!.NurseryId = 9;

        var request = new CreateReturnTicketRequestDto
        {
            OrderId = orderId,
            Items = new List<CreateReturnTicketItemRequestDto>
            {
                new() { NurseryOrderDetailId = 10, RequestedQuantity = 1 }
            }
        };

        ReturnTicket? createdTicket = null;

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(order);
        orderRepo.Setup(r => r.PrepareUpdate(order));

        var returnRepo = new Mock<IReturnTicketRepository>(MockBehavior.Strict);
        returnRepo.Setup(r => r.PrepareCreate(It.IsAny<ReturnTicket>()))
            .Callback<ReturnTicket>(t => createdTicket = t);

        var manager = new User
        {
            Id = 777,
            RoleId = (int)RoleEnum.Manager,
            NurseryId = 9,
            Status = (int)UserStatusEnum.Active,
            IsVerified = true,
            Email = "m@x.com"
        };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { manager });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.ReturnTicketRepository).Returns(returnRepo.Object);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow);

        var _ = await sut.CreateReturnTicketAsync(customerId, request);

        createdTicket.Should().NotBeNull();
        createdTicket!.ReturnTicketAssignments.Should().ContainSingle(a => a.NurseryId == 9 && a.ManagerId == 777);
    }

    [Fact]
    public async Task CreateReturnTicketAsync_ShouldCreateMultipleItems_WhenValid_Normal()
    {
        const int customerId = 12;
        const int orderId = 102;
        var d1 = Detail(1, quantity: 2);
        var d2 = Detail(2, quantity: 1);
        var order = BuildOrderForReturn(orderId, customerId, OrderStatusEnum.PendingConfirmation, d1, d2);

        var request = new CreateReturnTicketRequestDto
        {
            OrderId = orderId,
            Items = new List<CreateReturnTicketItemRequestDto>
            {
                new() { NurseryOrderDetailId = 1, RequestedQuantity = 2 },
                new() { NurseryOrderDetailId = 2, RequestedQuantity = 1 }
            }
        };

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(order);
        orderRepo.Setup(r => r.PrepareUpdate(order));

        var returnRepo = new Mock<IReturnTicketRepository>(MockBehavior.Strict);
        returnRepo.Setup(r => r.PrepareCreate(It.IsAny<ReturnTicket>()));

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.ReturnTicketRepository).Returns(returnRepo.Object);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow);

        var result = await sut.CreateReturnTicketAsync(customerId, request);

        result.Items.Should().HaveCount(2);
        d1.Status.Should().Be((int)OrderStatusEnum.RefundRequested);
        d2.Status.Should().Be((int)OrderStatusEnum.RefundRequested);
    }

    [Fact]
    public async Task CreateReturnTicketAsync_ShouldAllowRequestedQuantityEqualsPurchased_Boundary()
    {
        const int customerId = 13;
        const int orderId = 103;
        var d1 = Detail(1, quantity: 3);
        var order = BuildOrderForReturn(orderId, customerId, OrderStatusEnum.PendingConfirmation, d1);

        var request = new CreateReturnTicketRequestDto
        {
            OrderId = orderId,
            Items = new List<CreateReturnTicketItemRequestDto>
            {
                new() { NurseryOrderDetailId = 1, RequestedQuantity = 3 }
            }
        };

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(order);
        orderRepo.Setup(r => r.PrepareUpdate(order));

        var returnRepo = new Mock<IReturnTicketRepository>(MockBehavior.Strict);
        returnRepo.Setup(r => r.PrepareCreate(It.IsAny<ReturnTicket>()));

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.ReturnTicketRepository).Returns(returnRepo.Object);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow);

        var result = await sut.CreateReturnTicketAsync(customerId, request);

        result.Items.Should().ContainSingle(i => i.NurseryOrderDetailId == 1 && i.RequestedQuantity == 3);
    }

    [Fact]
    public async Task CreateReturnTicketAsync_ShouldThrowBadRequest_WhenDuplicatedDetailIds_Boundary()
    {
        const int customerId = 14;
        const int orderId = 104;
        var d1 = Detail(1, quantity: 2);
        var order = BuildOrderForReturn(orderId, customerId, OrderStatusEnum.PendingConfirmation, d1);

        var request = new CreateReturnTicketRequestDto
        {
            OrderId = orderId,
            Items = new List<CreateReturnTicketItemRequestDto>
            {
                new() { NurseryOrderDetailId = 1, RequestedQuantity = 1 },
                new() { NurseryOrderDetailId = 1, RequestedQuantity = 1 }
            }
        };

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(order);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);

        var sut = CreateSut(uow);

        var act = () => sut.CreateReturnTicketAsync(customerId, request);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Duplicated NurseryOrderDetailId(s): 1");
    }

    [Fact]
    public async Task CreateReturnTicketAsync_ShouldThrowConflict_WhenDetailAlreadyRefundFlow_Abnormal()
    {
        const int customerId = 15;
        const int orderId = 105;
        var d1 = Detail(1, quantity: 1, detailStatus: OrderStatusEnum.RefundRequested);
        var order = BuildOrderForReturn(orderId, customerId, OrderStatusEnum.PendingConfirmation, d1);

        var request = new CreateReturnTicketRequestDto
        {
            OrderId = orderId,
            Items = new List<CreateReturnTicketItemRequestDto>
            {
                new() { NurseryOrderDetailId = 1, RequestedQuantity = 1 }
            }
        };

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(order);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);

        var sut = CreateSut(uow);

        var act = () => sut.CreateReturnTicketAsync(customerId, request);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("NurseryOrderDetail 1 already has a refund flow");
    }
}

