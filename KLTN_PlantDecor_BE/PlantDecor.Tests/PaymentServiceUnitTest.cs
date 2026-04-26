using FluentAssertions;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Net;

namespace PlantDecor.Tests;

public class PaymentServiceUnitTest
{
    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeZoneId"] = "SE Asia Standard Time",
                ["Vnpay:Version"] = "2.1.0",
                ["Vnpay:Command"] = "pay",
                ["Vnpay:TmnCode"] = "UNITTEST",
                ["Vnpay:CurrCode"] = "VND",
                ["Vnpay:Locale"] = "vn",
                ["Vnpay:PaymentBackReturnUrl"] = "https://unit.test/return",
                ["Vnpay:BaseUrl"] = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
                ["Vnpay:HashSecret"] = "unit-test-hash-secret"
            })
            .Build();
    }

    private static HttpContext CreateHttpContext(string ip = "127.0.0.1")
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return ctx;
    }

    private static PaymentService CreateSut(
        Mock<IUnitOfWork> uow,
        IConfiguration configuration,
        Mock<ILogger<PaymentService>> logger)
    {
        var cache = new Mock<ICacheService>(MockBehavior.Strict);
        var background = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        return new PaymentService(uow.Object, cache.Object, configuration, background.Object, logger.Object);
    }

    [Fact]
    public async Task CreatePaymentUrlAsync_ShouldCreatePaymentAndTransaction_AndReturnUrl_Normal()
    {
        const int userId = 7;
        var request = new CreatePaymentRequestDto { InvoiceId = 10 };
        var httpContext = CreateHttpContext();

        var invoice = new Invoice
        {
            Id = 10,
            OrderId = 1,
            Type = (int)InvoiceTypeEnum.FullPayment,
            Status = (int)InvoiceStatusEnum.Pending,
            TotalAmount = 100m
        };

        var order = new Order { Id = 1, UserId = userId };

        Payment? createdPayment = null;

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        invoiceRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(invoice);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var paymentRepo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        paymentRepo.Setup(r => r.GetByInvoiceIdAsync(10)).ReturnsAsync(new List<Payment>());
        paymentRepo.Setup(r => r.PrepareCreate(It.IsAny<Payment>()))
            .Callback<Payment>(p =>
            {
                p.Id = 55;
                createdPayment = p;
            });

        var transactionRepo = new Mock<ITransactionRepository>(MockBehavior.Strict);
        transactionRepo.Setup(r => r.PrepareCreate(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => t.Id = 99);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.PaymentRepository).Returns(paymentRepo.Object);
        uow.SetupGet(x => x.TransactionRepository).Returns(transactionRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateTestConfiguration(), new Mock<ILogger<PaymentService>>());

        var result = await sut.CreatePaymentUrlAsync(userId, request, httpContext);

        result.Should().NotBeNull();
        result.PaymentId.Should().Be(55);
        result.PaymentUrl.Should().NotBeNullOrWhiteSpace();
        result.PaymentUrl.Should().Contain("vnp_Amount=10000");
        result.PaymentUrl.Should().Contain("vnp_TxnRef=");

        createdPayment.Should().NotBeNull();
        createdPayment!.Status.Should().Be((int)PaymentStatusEnum.Pending);
        createdPayment.Amount.Should().Be(100m);

        uow.Verify(x => x.SaveAsync(), Times.Exactly(2));
        paymentRepo.Verify(r => r.PrepareCreate(It.IsAny<Payment>()), Times.Once);
        transactionRepo.Verify(r => r.PrepareCreate(It.IsAny<Transaction>()), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentUrlAsync_ShouldUseInvoiceTypeAsPaymentType_Normal()
    {
        const int userId = 7;
        var request = new CreatePaymentRequestDto { InvoiceId = 11 };
        var httpContext = CreateHttpContext();

        var invoice = new Invoice
        {
            Id = 11,
            OrderId = 2,
            Type = (int)InvoiceTypeEnum.Deposit,
            Status = (int)InvoiceStatusEnum.Pending,
            TotalAmount = 200m
        };

        var order = new Order { Id = 2, UserId = userId };

        Payment? createdPayment = null;

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        invoiceRepo.Setup(r => r.GetByIdWithDetailsAsync(11)).ReturnsAsync(invoice);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(order);

        var paymentRepo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        paymentRepo.Setup(r => r.GetByInvoiceIdAsync(11)).ReturnsAsync(new List<Payment>());
        paymentRepo.Setup(r => r.PrepareCreate(It.IsAny<Payment>()))
            .Callback<Payment>(p =>
            {
                p.Id = 56;
                createdPayment = p;
            });

        var transactionRepo = new Mock<ITransactionRepository>(MockBehavior.Strict);
        transactionRepo.Setup(r => r.PrepareCreate(It.IsAny<Transaction>()));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.PaymentRepository).Returns(paymentRepo.Object);
        uow.SetupGet(x => x.TransactionRepository).Returns(transactionRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateTestConfiguration(), new Mock<ILogger<PaymentService>>());

        var result = await sut.CreatePaymentUrlAsync(userId, request, httpContext);

        result.PaymentId.Should().Be(56);
        createdPayment.Should().NotBeNull();
        createdPayment!.PaymentType.Should().Be((int)PaymentTypeEnum.Deposit);
    }

    [Fact]
    public async Task CreatePaymentUrlAsync_ShouldCreateTransactionWithTimeout_Normal()
    {
        const int userId = 7;
        var request = new CreatePaymentRequestDto { InvoiceId = 12 };
        var httpContext = CreateHttpContext();

        var invoice = new Invoice
        {
            Id = 12,
            OrderId = 3,
            Type = (int)InvoiceTypeEnum.FullPayment,
            Status = (int)InvoiceStatusEnum.Pending,
            TotalAmount = 150m
        };

        var order = new Order { Id = 3, UserId = userId };

        Transaction? createdTransaction = null;

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        invoiceRepo.Setup(r => r.GetByIdWithDetailsAsync(12)).ReturnsAsync(invoice);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(order);

        var paymentRepo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        paymentRepo.Setup(r => r.GetByInvoiceIdAsync(12)).ReturnsAsync(new List<Payment>());
        paymentRepo.Setup(r => r.PrepareCreate(It.IsAny<Payment>()))
            .Callback<Payment>(p => p.Id = 57);

        var transactionRepo = new Mock<ITransactionRepository>(MockBehavior.Strict);
        transactionRepo.Setup(r => r.PrepareCreate(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => createdTransaction = t);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.PaymentRepository).Returns(paymentRepo.Object);
        uow.SetupGet(x => x.TransactionRepository).Returns(transactionRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateTestConfiguration(), new Mock<ILogger<PaymentService>>());

        var _ = await sut.CreatePaymentUrlAsync(userId, request, httpContext);

        createdTransaction.Should().NotBeNull();
        createdTransaction!.Status.Should().Be((int)TransactionStatusEnum.Pending);
        createdTransaction.ExpiredAt.Should().NotBeNull();
        createdTransaction.ExpiredAt!.Value.Should().BeAfter(DateTime.Now.AddMinutes(29));
        createdTransaction.ExpiredAt!.Value.Should().BeBefore(DateTime.Now.AddMinutes(31));
    }

    [Fact]
    public async Task CreatePaymentUrlAsync_ShouldSucceed_WhenAmountIsMinimalPositive_Boundary()
    {
        const int userId = 7;
        var request = new CreatePaymentRequestDto { InvoiceId = 13 };
        var httpContext = CreateHttpContext();

        var invoice = new Invoice
        {
            Id = 13,
            OrderId = 4,
            Type = (int)InvoiceTypeEnum.FullPayment,
            Status = (int)InvoiceStatusEnum.Pending,
            TotalAmount = 0.01m
        };

        var order = new Order { Id = 4, UserId = userId };

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        invoiceRepo.Setup(r => r.GetByIdWithDetailsAsync(13)).ReturnsAsync(invoice);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(order);

        var paymentRepo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        paymentRepo.Setup(r => r.GetByInvoiceIdAsync(13)).ReturnsAsync(new List<Payment>());
        paymentRepo.Setup(r => r.PrepareCreate(It.IsAny<Payment>()))
            .Callback<Payment>(p => p.Id = 58);

        var transactionRepo = new Mock<ITransactionRepository>(MockBehavior.Strict);
        transactionRepo.Setup(r => r.PrepareCreate(It.IsAny<Transaction>()));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.PaymentRepository).Returns(paymentRepo.Object);
        uow.SetupGet(x => x.TransactionRepository).Returns(transactionRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateTestConfiguration(), new Mock<ILogger<PaymentService>>());

        var result = await sut.CreatePaymentUrlAsync(userId, request, httpContext);

        result.PaymentId.Should().Be(58);
        result.PaymentUrl.Should().Contain("vnp_Amount=1");
    }

    [Fact]
    public async Task CreatePaymentUrlAsync_ShouldAllowNewPayment_WhenExistingPendingTransactionExpired_Boundary()
    {
        const int userId = 7;
        var request = new CreatePaymentRequestDto { InvoiceId = 14 };
        var httpContext = CreateHttpContext();

        var invoice = new Invoice
        {
            Id = 14,
            OrderId = 5,
            Type = (int)InvoiceTypeEnum.FullPayment,
            Status = (int)InvoiceStatusEnum.Pending,
            TotalAmount = 100m
        };

        var order = new Order { Id = 5, UserId = userId };

        var existingPayment = new Payment
        {
            Id = 777,
            InvoiceId = 14,
            Status = (int)PaymentStatusEnum.Pending,
            CreatedAt = DateTime.Now.AddMinutes(-10),
            Transactions = new List<Transaction>
            {
                new()
                {
                    Id = 1,
                    Status = (int)TransactionStatusEnum.Pending,
                    ExpiredAt = DateTime.Now.AddMinutes(-1) // expired => not active
                }
            }
        };

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        invoiceRepo.Setup(r => r.GetByIdWithDetailsAsync(14)).ReturnsAsync(invoice);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(order);

        var paymentRepo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        paymentRepo.Setup(r => r.GetByInvoiceIdAsync(14)).ReturnsAsync(new List<Payment> { existingPayment });
        paymentRepo.Setup(r => r.PrepareCreate(It.IsAny<Payment>()))
            .Callback<Payment>(p => p.Id = 59);

        var transactionRepo = new Mock<ITransactionRepository>(MockBehavior.Strict);
        transactionRepo.Setup(r => r.PrepareCreate(It.IsAny<Transaction>()));

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.PaymentRepository).Returns(paymentRepo.Object);
        uow.SetupGet(x => x.TransactionRepository).Returns(transactionRepo.Object);
        uow.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        var sut = CreateSut(uow, CreateTestConfiguration(), new Mock<ILogger<PaymentService>>());

        var result = await sut.CreatePaymentUrlAsync(userId, request, httpContext);

        result.PaymentId.Should().Be(59);
    }

    [Fact]
    public async Task CreatePaymentUrlAsync_ShouldThrowBadRequest_WhenActivePendingPaymentExists_Abnormal()
    {
        const int userId = 7;
        var request = new CreatePaymentRequestDto { InvoiceId = 15 };
        var httpContext = CreateHttpContext();

        var invoice = new Invoice
        {
            Id = 15,
            OrderId = 6,
            Type = (int)InvoiceTypeEnum.FullPayment,
            Status = (int)InvoiceStatusEnum.Pending,
            TotalAmount = 100m
        };

        var order = new Order { Id = 6, UserId = userId };

        var existingPayment = new Payment
        {
            Id = 888,
            InvoiceId = 15,
            Status = (int)PaymentStatusEnum.Pending,
            CreatedAt = DateTime.Now.AddMinutes(-1),
            Transactions = new List<Transaction>
            {
                new()
                {
                    Id = 1,
                    Status = (int)TransactionStatusEnum.Pending,
                    ExpiredAt = DateTime.Now.AddMinutes(10) // active
                }
            }
        };

        var invoiceRepo = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        invoiceRepo.Setup(r => r.GetByIdWithDetailsAsync(15)).ReturnsAsync(invoice);

        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        orderRepo.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(order);

        var paymentRepo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        paymentRepo.Setup(r => r.GetByInvoiceIdAsync(15)).ReturnsAsync(new List<Payment> { existingPayment });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.InvoiceRepository).Returns(invoiceRepo.Object);
        uow.SetupGet(x => x.OrderRepository).Returns(orderRepo.Object);
        uow.SetupGet(x => x.PaymentRepository).Returns(paymentRepo.Object);

        var sut = CreateSut(uow, CreateTestConfiguration(), new Mock<ILogger<PaymentService>>());

        var act = () => sut.CreatePaymentUrlAsync(userId, request, httpContext);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("An active payment is pending. Please use retry payment API or wait for it to expire");
    }
}

