using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Libraries;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private const int PaymentTimeoutMinutes = 30;
        private const int MaxRetryAttempts = 3;

        public PaymentService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<CreatePaymentUrlResponseDto> CreatePaymentUrlAsync(int userId, CreatePaymentRequestDto request, HttpContext httpContext)
        {
            var order = await ResolveTargetOrderAsync(userId, request);
            var (paymentType, amount) = DeterminePaymentTypeAndAmount(order);

            if (amount <= 0)
                throw new BadRequestException("Payment amount must be greater than 0");

            var existingPayments = await _unitOfWork.PaymentRepository.GetByOrderIdAsync(order.Id);

            var now = DateTime.Now;

            var paymentsForType = existingPayments
                .Where(p => p.PaymentType == (int)paymentType)
                .ToList();

            var activePendingPayment = paymentsForType.FirstOrDefault(p =>
                p.Status == (int)PaymentStatusEnum.Pending &&
                p.Transactions.Any(t =>
                    t.Status == (int)TransactionStatusEnum.Pending &&
                    (!t.ExpiredAt.HasValue || t.ExpiredAt.Value > now)));

            if (activePendingPayment != null)
                throw new BadRequestException("An active payment is pending. Please complete or cancel it before retrying");

            var attemptCount = paymentsForType.Count;
            if (attemptCount >= MaxRetryAttempts)
                throw new BadRequestException($"Payment retry limit exceeded. Maximum attempts: {MaxRetryAttempts}");

            // For RemainingBalance payment, Invoice should already exist (created when order was delivered)
            if (paymentType == PaymentTypeEnum.RemainingBalance)
            {
                var existingInvoice = await _unitOfWork.InvoiceRepository
                    .GetPendingByOrderIdAndTypeAsync(order.Id, (int)InvoiceTypeEnum.RemainingBalance);

                if (existingInvoice == null)
                    throw new BadRequestException("RemainingBalance invoice not found. Order may not be in the correct status for remaining payment.");
            }

            // Create Payment record (Pending)
            var payment = new Payment
            {
                OrderId = order.Id,
                PaymentType = (int)paymentType,
                Amount = amount,
                Status = (int)PaymentStatusEnum.Pending,
                CreatedAt = DateTime.Now
            };
            _unitOfWork.PaymentRepository.PrepareCreate(payment);
            await _unitOfWork.SaveAsync();

            // Create Transaction record (Pending)
            var txnRef = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var transaction = new Transaction
            {
                PaymentId = payment.Id,
                Amount = amount,
                Status = (int)TransactionStatusEnum.Pending,
                TransactionId = txnRef.ToString(),
                OrderInfo = $"Thanh toan don hang {order.Id}",
                CreatedAt = DateTime.Now,
                ExpiredAt = DateTime.Now.AddMinutes(PaymentTimeoutMinutes)
            };
            _unitOfWork.TransactionRepository.PrepareCreate(transaction);
            await _unitOfWork.SaveAsync();

            var paymentUrl = GenerateVnPayUrl(txnRef, amount, order.Id, httpContext);

            return new CreatePaymentUrlResponseDto
            {
                PaymentId = payment.Id,
                PaymentUrl = paymentUrl
            };
        }

        public async Task<PaymentResponse> ProcessVnpayCallbackAsync(IQueryCollection queryParams)
        {
            // Build fake URL for the VnPayLibrary's URL parser
            var queryString = string.Join("&", queryParams.Select(q => $"{q.Key}={q.Value}"));
            var fakeUrl = $"https://callback?{queryString}";

            var vnPayLib = new VnPayLibrary();
            var hashSecret = _configuration["Vnpay:HashSecret"]!;
            var response = vnPayLib.GetFullResponseData(fakeUrl, hashSecret);

            if (!response.Success)
                return new PaymentResponse { Success = false, ResponseCode = "97" };

            // Callback chỉ trả về kết quả để hiển thị cho user.
            // Việc cập nhật DB được xử lý bởi IPN (ProcessVnpayIpnAsync).
            var responseCode = queryParams["vnp_ResponseCode"].ToString();
            response.Success = responseCode == "00";
            response.ResponseCode = responseCode;

            return response;
        }

        public async Task<VnpayIpnResponseDto> ProcessVnpayIpnAsync(IQueryCollection queryParams)
        {
            // Build fake URL for the VnPayLibrary's URL parser
            var queryString = string.Join("&", queryParams.Select(q => $"{q.Key}={q.Value}"));
            var fakeUrl = $"https://ipn?{queryString}";

            var vnPayLib = new VnPayLibrary();
            var hashSecret = _configuration["Vnpay:HashSecret"]!;
            var response = vnPayLib.GetFullResponseData(fakeUrl, hashSecret);

            if (!response.Success)
                return new VnpayIpnResponseDto { RspCode = "97", Message = "Invalid signature" };

            var responseCode = queryParams["vnp_ResponseCode"].ToString();
            var txnRefStr = queryParams["vnp_TxnRef"].ToString();

            var dbTransaction = await _unitOfWork.TransactionRepository.GetByTransactionIdAsync(txnRefStr);
            if (dbTransaction == null)
                return new VnpayIpnResponseDto { RspCode = "01", Message = "Order not found" };

            // Idempotency: nếu đã xử lý rồi thì không xử lý lại
            if (dbTransaction.Status != (int)TransactionStatusEnum.Pending)
                return new VnpayIpnResponseDto { RspCode = "02", Message = "Order already confirmed" };

            var payment = await _unitOfWork.PaymentRepository.GetByIdWithTransactionsAsync(dbTransaction.PaymentId!.Value);
            if (payment == null)
                return new VnpayIpnResponseDto { RspCode = "01", Message = "Payment not found" };

            dbTransaction.ResponseCode = responseCode;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (responseCode == "00")
                {
                    var paymentType = (PaymentTypeEnum)payment.PaymentType!.Value;
                    var order = await ResolveTargetOrderForPaymentAsync(payment);

                    dbTransaction.Status = (int)TransactionStatusEnum.Completed;
                    payment.Status = (int)PaymentStatusEnum.Success;
                    payment.PaidAt = DateTime.Now;

                    var newOrderStatus = DetermineSuccessOrderStatus(paymentType);
                    order.Status = newOrderStatus;
                    order.UpdatedAt = DateTime.Now;
                    _unitOfWork.OrderRepository.PrepareUpdate(order);

                    // Update all NurseryOrder status to match parent Order status
                    foreach (var nurseryOrder in order.NurseryOrders)
                    {
                        nurseryOrder.Status = newOrderStatus;
                        nurseryOrder.UpdatedAt = DateTime.Now;
                    }

                    // PaymentType và InvoiceType dùng cùng giá trị số: Deposit=1, FullPayment=2, RemainingBalance=3
                    var invoice = await _unitOfWork.InvoiceRepository
                        .GetPendingByOrderIdAndTypeAsync(order.Id, (int)paymentType);

                    if (invoice == null)
                        throw new NotFoundException($"Invoice not found for order {order.Id}");

                    invoice.Status = (int)InvoiceStatusEnum.Paid;
                    _unitOfWork.InvoiceRepository.PrepareUpdate(invoice);
                }
                else
                {
                    dbTransaction.Status = responseCode == "24"
                        ? (int)TransactionStatusEnum.Cancelled
                        : (int)TransactionStatusEnum.Failed;

                    payment.Status = responseCode == "24"
                        ? (int)PaymentStatusEnum.Cancelled
                        : (int)PaymentStatusEnum.Failed;
                }

                _unitOfWork.TransactionRepository.PrepareUpdate(dbTransaction);
                _unitOfWork.PaymentRepository.PrepareUpdate(payment);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return new VnpayIpnResponseDto { RspCode = "00", Message = "Confirm Success" };
        }

        #region Helpers

        private string GenerateVnPayUrl(long txnRef, decimal amount, int orderId, HttpContext httpContext)
        {
            var vnpay = new VnPayLibrary();
            var timeZoneId = _configuration["TimeZoneId"] ?? "SE Asia Standard Time";
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var vnpCreateDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone)
                .ToString("yyyyMMddHHmmss");

            vnpay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]!);
            vnpay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]!);
            vnpay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]!);
            vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());
            vnpay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]!);
            vnpay.AddRequestData("vnp_TxnRef", txnRef.ToString());
            vnpay.AddRequestData("vnp_OrderInfo", $"OrderId: {orderId}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]!);
            vnpay.AddRequestData("vnp_ReturnUrl", _configuration["Vnpay:PaymentBackReturnUrl"]!);
            vnpay.AddRequestData("vnp_IpAddr", vnpay.GetIpAddress(httpContext));
            vnpay.AddRequestData("vnp_CreateDate", vnpCreateDate);

            return vnpay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"]!, _configuration["Vnpay:HashSecret"]!);
        }

        private static (PaymentTypeEnum paymentType, decimal amount) DeterminePaymentTypeAndAmount(Order order)
        {
            var strategy = (PaymentStrategiesEnum)(order.PaymentStrategy ?? (int)PaymentStrategiesEnum.FullPayment);

            if (strategy == PaymentStrategiesEnum.FullPayment)
                return (PaymentTypeEnum.FullPayment, order.TotalAmount ?? 0);

            // Deposit strategy
            if (order.Status == (int)OrderStatusEnum.Pending)
                return (PaymentTypeEnum.Deposit, order.DepositAmount ?? 0);

            if (order.Status == (int)OrderStatusEnum.RemainingPaymentPending)
                return (PaymentTypeEnum.RemainingBalance, order.RemainingAmount ?? 0);

            throw new BadRequestException("Order is not in a payable state");
        }

        private static int DetermineSuccessOrderStatus(PaymentTypeEnum paymentType) => paymentType switch
        {
            PaymentTypeEnum.FullPayment => (int)OrderStatusEnum.Paid,
            PaymentTypeEnum.Deposit => (int)OrderStatusEnum.DepositPaid,
            PaymentTypeEnum.RemainingBalance => (int)OrderStatusEnum.Completed,
            _ => (int)OrderStatusEnum.Paid
        };

        private async Task<Order> ResolveTargetOrderAsync(int userId, CreatePaymentRequestDto request)
        {
            if (!request.OrderId.HasValue)
                throw new BadRequestException("OrderId is required");

            var order = await _unitOfWork.OrderRepository.GetByIdAsync(request.OrderId.Value);
            if (order == null)
                throw new NotFoundException($"Order {request.OrderId.Value} not found");

            if (order.UserId != userId)
                throw new ForbiddenException("You don't have access to this order");

            return order;
        }

        private async Task<Order> ResolveTargetOrderForPaymentAsync(Payment payment)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(payment.OrderId!.Value)
                ?? throw new NotFoundException("Order not found");

            return order;
        }

        #endregion
    }
}
