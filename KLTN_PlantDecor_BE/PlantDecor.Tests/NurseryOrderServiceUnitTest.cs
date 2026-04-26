using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Text;

namespace PlantDecor.Tests;

public class NurseryOrderServiceUnitTest
{
    private static NurseryOrderService CreateSut(Mock<IUnitOfWork> uow, Mock<ICloudinaryService> cloudinary)
        => new(uow.Object, cloudinary.Object);

    private static User CreateValidShipper(int userId, int nurseryId)
        => new()
        {
            Id = userId,
            RoleId = (int)RoleEnum.Shipper,
            Status = (int)UserStatusEnum.Active,
            IsVerified = true,
            NurseryId = nurseryId,
            Email = "shipper@test.local"
        };

    private static NurseryOrder CreateNurseryOrder(int nurseryOrderId, int orderId, int nurseryId, int shipperId, OrderStatusEnum status)
        => new()
        {
            Id = nurseryOrderId,
            OrderId = orderId,
            NurseryId = nurseryId,
            ShipperId = shipperId,
            Status = (int)status,
            Order = new Order { Id = orderId, UserId = 999 },
            Nursery = new Nursery { Id = nurseryId, Name = "Nursery" },
            NurseryOrderDetails = new List<NurseryOrderDetail>()
        };

    private static Order CreateParentOrder(
        int orderId,
        PaymentStrategiesEnum strategy,
        OrderStatusEnum status,
        decimal? remainingAmount,
        List<NurseryOrder> nurseryOrders,
        List<Invoice>? invoices = null)
        => new()
        {
            Id = orderId,
            UserId = 999,
            PaymentStrategy = (int)strategy,
            Status = (int)status,
            RemainingAmount = remainingAmount,
            NurseryOrders = nurseryOrders,
            Invoices = invoices ?? new List<Invoice>()
        };

    private static IFormFile CreateFakeImageFile(string fileName = "delivery.jpg")
    {
        var bytes = Encoding.UTF8.GetBytes("fake-image-content");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }

    // ----------------------------
    // StartShippingAsync (3 normal, 2 boundary, 1 abnormal)
    // ----------------------------

    [Fact]
    public async Task StartShippingAsync_ShouldSetNurseryOrderToShipping_AndSetStartedAt_Normal()
    {
        const int shipperId = 10;
        const int nurseryId = 2;
        const int nurseryOrderId = 100;
        const int orderId = 500;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Assigned);

        var parentOrder = CreateParentOrder(
            orderId,
            PaymentStrategiesEnum.FullPayment,
            status: OrderStatusEnum.Paid,
            remainingAmount: null,
            nurseryOrders: new List<NurseryOrder> { nurseryOrder });

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);
        nurseryOrderRepo.Setup(r => r.PrepareUpdate(nurseryOrder));

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(parentOrder);
        orderRepo.Setup(r => r.PrepareUpdate(parentOrder));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);
        var sut = CreateSut(uow, cloudinary);

        var result = await sut.StartShippingAsync(shipperId, nurseryOrderId, new StartShippingRequestDto { ShipperNote = "note" });

        result.Should().NotBeNull();
        nurseryOrder.Status.Should().Be((int)OrderStatusEnum.Shipping);
        nurseryOrder.ShippingStartedAt.Should().NotBeNull();
        nurseryOrder.ShipperNote.Should().Be("note");
        parentOrder.Status.Should().Be((int)OrderStatusEnum.Shipping);

        uow.Verify(x => x.SaveAsync(), Times.Once);
        orderRepo.Verify(r => r.PrepareUpdate(parentOrder), Times.Once);
        nurseryOrderRepo.Verify(r => r.PrepareUpdate(nurseryOrder), Times.Once);
    }

    [Fact]
    public async Task StartShippingAsync_ShouldSetParentOrderToShipping_OnlyIfNotAlreadyShipping_Normal()
    {
        const int shipperId = 11;
        const int nurseryId = 2;
        const int nurseryOrderId = 101;
        const int orderId = 501;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Assigned);

        var parentOrder = CreateParentOrder(
            orderId,
            PaymentStrategiesEnum.FullPayment,
            status: OrderStatusEnum.PendingConfirmation,
            remainingAmount: null,
            nurseryOrders: new List<NurseryOrder> { nurseryOrder });

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);
        nurseryOrderRepo.Setup(r => r.PrepareUpdate(nurseryOrder));

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(parentOrder);
        orderRepo.Setup(r => r.PrepareUpdate(parentOrder));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, new Mock<ICloudinaryService>(MockBehavior.Strict));

        var _ = await sut.StartShippingAsync(shipperId, nurseryOrderId, new StartShippingRequestDto { ShipperNote = null });

        parentOrder.Status.Should().Be((int)OrderStatusEnum.Shipping);
        orderRepo.Verify(r => r.PrepareUpdate(parentOrder), Times.Once);
    }

    [Fact]
    public async Task StartShippingAsync_ShouldReturnDto_WhenSuccess_Normal()
    {
        const int shipperId = 12;
        const int nurseryId = 3;
        const int nurseryOrderId = 102;
        const int orderId = 502;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Assigned);

        var parentOrder = CreateParentOrder(
            orderId,
            PaymentStrategiesEnum.FullPayment,
            status: OrderStatusEnum.Paid,
            remainingAmount: null,
            nurseryOrders: new List<NurseryOrder> { nurseryOrder });

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);
        nurseryOrderRepo.Setup(r => r.PrepareUpdate(nurseryOrder));

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(parentOrder);
        orderRepo.Setup(r => r.PrepareUpdate(parentOrder));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, new Mock<ICloudinaryService>(MockBehavior.Strict));

        NurseryOrderResponseDto dto = await sut.StartShippingAsync(shipperId, nurseryOrderId, new StartShippingRequestDto());

        dto.Id.Should().Be(nurseryOrderId);
        dto.Status.Should().Be((int)OrderStatusEnum.Shipping);
    }

    [Fact]
    public async Task StartShippingAsync_ShouldNotUpdateParentOrder_WhenAlreadyShipping_Boundary()
    {
        const int shipperId = 13;
        const int nurseryId = 4;
        const int nurseryOrderId = 103;
        const int orderId = 503;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Assigned);

        var parentOrder = CreateParentOrder(
            orderId,
            PaymentStrategiesEnum.FullPayment,
            status: OrderStatusEnum.Shipping, // boundary: already Shipping
            remainingAmount: null,
            nurseryOrders: new List<NurseryOrder> { nurseryOrder });

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);
        nurseryOrderRepo.Setup(r => r.PrepareUpdate(nurseryOrder));

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(parentOrder);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, new Mock<ICloudinaryService>(MockBehavior.Strict));

        var _ = await sut.StartShippingAsync(shipperId, nurseryOrderId, new StartShippingRequestDto());

        orderRepo.Verify(r => r.PrepareUpdate(It.IsAny<Order>()), Times.Never);
        parentOrder.Status.Should().Be((int)OrderStatusEnum.Shipping);
    }

    [Fact]
    public async Task StartShippingAsync_ShouldAllowEmptyShipperNote_Boundary()
    {
        const int shipperId = 14;
        const int nurseryId = 5;
        const int nurseryOrderId = 104;
        const int orderId = 504;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Assigned);

        var parentOrder = CreateParentOrder(
            orderId,
            PaymentStrategiesEnum.FullPayment,
            status: OrderStatusEnum.Paid,
            remainingAmount: null,
            nurseryOrders: new List<NurseryOrder> { nurseryOrder });

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);
        nurseryOrderRepo.Setup(r => r.PrepareUpdate(nurseryOrder));

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(parentOrder);
        orderRepo.Setup(r => r.PrepareUpdate(parentOrder));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, new Mock<ICloudinaryService>(MockBehavior.Strict));

        var _ = await sut.StartShippingAsync(shipperId, nurseryOrderId, new StartShippingRequestDto { ShipperNote = "" });

        nurseryOrder.ShipperNote.Should().Be("");
    }

    [Fact]
    public async Task StartShippingAsync_ShouldThrowForbidden_WhenNotOwner_Abnormal()
    {
        const int shipperId = 15;
        const int nurseryId = 6;
        const int nurseryOrderId = 105;
        const int orderId = 505;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId: 9999, status: OrderStatusEnum.Assigned);

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);

        var sut = CreateSut(uow, new Mock<ICloudinaryService>(MockBehavior.Strict));

        var act = () => sut.StartShippingAsync(shipperId, nurseryOrderId, new StartShippingRequestDto());

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You don't have permission to modify this nursery order");
    }

    // ----------------------------
    // MarkDeliveredAsync (3 normal, 2 boundary, 1 abnormal)
    // ----------------------------

    [Fact]
    public async Task MarkDeliveredAsync_ShouldMarkDelivered_AndUploadImage_WhenProvided_Normal()
    {
        const int shipperId = 20;
        const int nurseryId = 7;
        const int nurseryOrderId = 200;
        const int orderId = 600;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Shipping);

        var otherNurseryOrder = CreateNurseryOrder(201, orderId, nurseryId, shipperId, OrderStatusEnum.Delivered);

        var parentOrder = CreateParentOrder(
            orderId,
            PaymentStrategiesEnum.FullPayment,
            status: OrderStatusEnum.Shipping,
            remainingAmount: null,
            nurseryOrders: new List<NurseryOrder> { nurseryOrder, otherNurseryOrder },
            invoices: new List<Invoice>());

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);
        nurseryOrderRepo.Setup(r => r.PrepareUpdate(nurseryOrder));

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(parentOrder);
        orderRepo.Setup(r => r.PrepareUpdate(parentOrder));

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);
        var file = CreateFakeImageFile();
        cloudinary.Setup(c => c.ValidateDocumentFile(file, It.IsAny<int>())).Returns((true, ""));
        cloudinary.Setup(c => c.UploadFileAsync(file, "NurseryOrderDelivery"))
            .ReturnsAsync(new FileUploadResponse { SecureUrl = "https://cdn.test/delivery.jpg" });

        var sut = CreateSut(uow, cloudinary);

        var dto = await sut.MarkDeliveredAsync(shipperId, nurseryOrderId, new MarkDeliveredRequestDto
        {
            DeliveryNote = "ok",
            DeliveryImage = file
        });

        dto.Status.Should().Be((int)OrderStatusEnum.Delivered);
        nurseryOrder.DeliveryImageUrl.Should().Be("https://cdn.test/delivery.jpg");
        parentOrder.Status.Should().Be((int)OrderStatusEnum.PendingConfirmation);
        invoiceRepo.Verify(r => r.PrepareCreate(It.IsAny<Invoice>()), Times.Never);
        uow.Verify(x => x.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkDeliveredAsync_ShouldSetParentToRemainingPaymentPending_AndCreateRemainingInvoice_ForDeposit_Normal()
    {
        const int shipperId = 21;
        const int nurseryId = 8;
        const int nurseryOrderId = 210;
        const int orderId = 610;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Shipping);
        nurseryOrder.NurseryOrderDetails.Add(new NurseryOrderDetail { Id = 1, ItemName = "Item", UnitPrice = 10m, Quantity = 1, Amount = 10m });

        var parentOrder = CreateParentOrder(
            orderId,
            PaymentStrategiesEnum.Deposit,
            status: OrderStatusEnum.Shipping,
            remainingAmount: 50m,
            nurseryOrders: new List<NurseryOrder> { nurseryOrder },
            invoices: new List<Invoice>());

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);
        nurseryOrderRepo.Setup(r => r.PrepareUpdate(nurseryOrder));

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(parentOrder);
        orderRepo.Setup(r => r.PrepareUpdate(parentOrder));

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        invoiceRepo.Setup(r => r.PrepareCreate(It.IsAny<Invoice>()));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, new Mock<ICloudinaryService>(MockBehavior.Strict));

        var dto = await sut.MarkDeliveredAsync(shipperId, nurseryOrderId, new MarkDeliveredRequestDto { DeliveryNote = "done" });

        dto.Status.Should().Be((int)OrderStatusEnum.Delivered);
        parentOrder.Status.Should().Be((int)OrderStatusEnum.RemainingPaymentPending);
        invoiceRepo.Verify(r => r.PrepareCreate(It.IsAny<Invoice>()), Times.Once);
    }

    [Fact]
    public async Task MarkDeliveredAsync_ShouldSetParentDelivered_WhenNotAllNurseryOrdersDelivered_Normal()
    {
        const int shipperId = 22;
        const int nurseryId = 9;
        const int nurseryOrderId = 220;
        const int orderId = 620;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Shipping);
        var otherNurseryOrder = CreateNurseryOrder(221, orderId, nurseryId, shipperId, OrderStatusEnum.Shipping); // not delivered yet

        var parentOrder = CreateParentOrder(
            orderId,
            PaymentStrategiesEnum.FullPayment,
            status: OrderStatusEnum.Shipping,
            remainingAmount: null,
            nurseryOrders: new List<NurseryOrder> { nurseryOrder, otherNurseryOrder },
            invoices: new List<Invoice>());

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);
        nurseryOrderRepo.Setup(r => r.PrepareUpdate(nurseryOrder));

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(parentOrder);
        orderRepo.Setup(r => r.PrepareUpdate(parentOrder));

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, new Mock<ICloudinaryService>(MockBehavior.Strict));

        var _ = await sut.MarkDeliveredAsync(shipperId, nurseryOrderId, new MarkDeliveredRequestDto());

        parentOrder.Status.Should().Be((int)OrderStatusEnum.Delivered); // only set Delivered, not PendingConfirmation yet
    }

    [Fact]
    public async Task MarkDeliveredAsync_ShouldAllowDeliveryNoteLength255_Boundary()
    {
        const int shipperId = 23;
        const int nurseryId = 10;
        const int nurseryOrderId = 230;
        const int orderId = 630;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Shipping);

        var parentOrder = CreateParentOrder(
            orderId,
            PaymentStrategiesEnum.FullPayment,
            status: OrderStatusEnum.Shipping,
            remainingAmount: null,
            nurseryOrders: new List<NurseryOrder> { nurseryOrder },
            invoices: new List<Invoice>());

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);
        nurseryOrderRepo.Setup(r => r.PrepareUpdate(nurseryOrder));

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(parentOrder);
        orderRepo.Setup(r => r.PrepareUpdate(parentOrder));

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, new Mock<ICloudinaryService>(MockBehavior.Strict));

        var note255 = new string('a', 255);
        var dto = await sut.MarkDeliveredAsync(shipperId, nurseryOrderId, new MarkDeliveredRequestDto { DeliveryNote = note255 });

        dto.Status.Should().Be((int)OrderStatusEnum.Delivered);
        nurseryOrder.DeliveryNote.Should().Be(note255);
    }

    [Fact]
    public async Task MarkDeliveredAsync_ShouldNotCreateRemainingInvoice_WhenAlreadyExists_Boundary()
    {
        const int shipperId = 24;
        const int nurseryId = 11;
        const int nurseryOrderId = 240;
        const int orderId = 640;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Shipping);
        nurseryOrder.NurseryOrderDetails.Add(new NurseryOrderDetail { Id = 1, ItemName = "Item", UnitPrice = 10m, Quantity = 1, Amount = 10m });

        var existingRemainingInvoice = new Invoice
        {
            Id = 1,
            Type = (int)InvoiceTypeEnum.RemainingBalance,
            Status = (int)InvoiceStatusEnum.Pending
        };

        var parentOrder = CreateParentOrder(
            orderId,
            PaymentStrategiesEnum.Deposit,
            status: OrderStatusEnum.Shipping,
            remainingAmount: 50m,
            nurseryOrders: new List<NurseryOrder> { nurseryOrder },
            invoices: new List<Invoice> { existingRemainingInvoice });

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);
        nurseryOrderRepo.Setup(r => r.PrepareUpdate(nurseryOrder));

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId)).ReturnsAsync(parentOrder);
        orderRepo.Setup(r => r.PrepareUpdate(parentOrder));

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, new Mock<ICloudinaryService>(MockBehavior.Strict));

        var _ = await sut.MarkDeliveredAsync(shipperId, nurseryOrderId, new MarkDeliveredRequestDto());

        invoiceRepo.Verify(r => r.PrepareCreate(It.IsAny<Invoice>()), Times.Never);
        parentOrder.Status.Should().Be((int)OrderStatusEnum.RemainingPaymentPending);
    }

    [Fact]
    public async Task MarkDeliveredAsync_ShouldThrowBadRequest_WhenDeliveryImageInvalid_Abnormal()
    {
        const int shipperId = 25;
        const int nurseryId = 12;
        const int nurseryOrderId = 250;
        const int orderId = 650;

        var shipper = CreateValidShipper(shipperId, nurseryId);
        var nurseryOrder = CreateNurseryOrder(nurseryOrderId, orderId, nurseryId, shipperId, OrderStatusEnum.Shipping);

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(r => r.GetByIdAsync(shipperId)).ReturnsAsync(shipper);

        var nurseryOrderRepo = new Mock<INurseryOrderRepository>(MockBehavior.Strict);
        nurseryOrderRepo.Setup(r => r.GetByIdWithDetailsAsync(nurseryOrderId)).ReturnsAsync(nurseryOrder);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(orderId))
            .ReturnsAsync(CreateParentOrder(orderId, PaymentStrategiesEnum.FullPayment, OrderStatusEnum.Shipping, null, new List<NurseryOrder> { nurseryOrder }));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.UserRepository).Returns(userRepo.Object);
        uow.SetupGet(x => x.NurseryOrderRepository).Returns(nurseryOrderRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);

        var cloudinary = new Mock<ICloudinaryService>(MockBehavior.Strict);
        var file = CreateFakeImageFile();
        cloudinary.Setup(c => c.ValidateDocumentFile(file, It.IsAny<int>()))
            .Returns((false, "Invalid file"));

        var sut = CreateSut(uow, cloudinary);

        var act = () => sut.MarkDeliveredAsync(shipperId, nurseryOrderId, new MarkDeliveredRequestDto
        {
            DeliveryImage = file
        });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid file");
    }
}

