using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CreatePaymentUrlResponseDto> CreatePaymentUrlAsync(int userId, CreatePaymentRequestDto request, HttpContext httpContext)
        {
            // Resolve Invoice and validate access
            var invoice = await _unitOfWork.InvoiceRepository.GetByIdWithDetailsAsync(request.InvoiceId);
            if (invoice == null)
                throw new NotFoundException($"Invoice {request.InvoiceId} not found");

            if (!invoice.OrderId.HasValue)
                throw new BadRequestException("Invoice is not associated with any order");

            var order = await _unitOfWork.OrderRepository.GetByIdAsync(invoice.OrderId.Value);
            if (order == null)
                throw new NotFoundException("Order not found");

            if (order.UserId != userId)
                throw new ForbiddenException("You don't have access to this invoice");

            // Validate Invoice status
            if (invoice.Status != (int)InvoiceStatusEnum.Pending)
                throw new BadRequestException($"Invoice is not in Pending status. Current status: {(InvoiceStatusEnum)invoice.Status!.Value}");

            // Get payment type and amount from Invoice
            var paymentType = (PaymentTypeEnum)invoice.Type!.Value;
            var amount = invoice.TotalAmount ?? 0;

            if (amount <= 0)
                throw new BadRequestException("Payment amount must be greater than 0");

            // Check if there's a pending payment with active transaction for this invoice
            var existingPayments = await _unitOfWork.PaymentRepository.GetByOrderIdAsync(order.Id);
            var now = DateTime.Now;

            var activePendingPayment = existingPayments.FirstOrDefault(p =>
                p.PaymentType == (int)paymentType &&
                p.Status == (int)PaymentStatusEnum.Pending &&
                p.Transactions.Any(t =>
                    t.Status == (int)TransactionStatusEnum.Pending &&
                    (!t.ExpiredAt.HasValue || t.ExpiredAt.Value > now)));

            if (activePendingPayment != null)
                throw new BadRequestException("An active payment is pending. Please use retry payment API or wait for it to expire");

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

        public async Task<CreatePaymentUrlResponseDto> RetryPaymentAsync(int userId, int paymentId, HttpContext httpContext)
        {
            var payment = await _unitOfWork.PaymentRepository.GetByIdWithTransactionsAsync(paymentId);
            if (payment == null)
                throw new NotFoundException($"Payment {paymentId} not found");

            // Verify payment belongs to user's order
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(payment.OrderId!.Value);
            if (order == null)
                throw new NotFoundException("Order not found");

            if (order.UserId != userId)
                throw new ForbiddenException("You don't have access to this payment");

            // Only allow retry for Pending payments
            if (payment.Status != (int)PaymentStatusEnum.Pending)
                throw new BadRequestException($"Cannot retry payment with status: {(PaymentStatusEnum)payment.Status!.Value}");

            var now = DateTime.Now;

            // Check retry limit (max 3 transactions per payment)
            var transactionCount = payment.Transactions.Count;
            if (transactionCount >= MaxRetryAttempts)
                throw new BadRequestException($"Payment retry limit exceeded. Maximum attempts: {MaxRetryAttempts}");

            // Check if there's an active pending transaction
            var activePendingTransaction = payment.Transactions.FirstOrDefault(t =>
                t.Status == (int)TransactionStatusEnum.Pending &&
                (!t.ExpiredAt.HasValue || t.ExpiredAt.Value > now));

            if (activePendingTransaction != null)
                throw new BadRequestException("An active transaction is pending. Please wait for it to expire or complete");

            // Expire all pending transactions
            foreach (var transaction in payment.Transactions.Where(t => t.Status == (int)TransactionStatusEnum.Pending))
            {
                transaction.Status = (int)TransactionStatusEnum.TimedOut;
                transaction.ExpiredAt = now;
                _unitOfWork.TransactionRepository.PrepareUpdate(transaction);
            }
            await _unitOfWork.SaveAsync();

            // Create new Transaction
            var txnRef = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var newTransaction = new Transaction
            {
                PaymentId = payment.Id,
                Amount = payment.Amount,
                Status = (int)TransactionStatusEnum.Pending,
                TransactionId = txnRef.ToString(),
                OrderInfo = $"Thanh toan don hang {order.Id}",
                CreatedAt = DateTime.Now,
                ExpiredAt = DateTime.Now.AddMinutes(PaymentTimeoutMinutes)
            };
            _unitOfWork.TransactionRepository.PrepareCreate(newTransaction);
            await _unitOfWork.SaveAsync();

            var paymentUrl = GenerateVnPayUrl(txnRef, payment.Amount ?? 0, order.Id, httpContext);

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

            // Cập nhật Transaction với response code từ VnPay (dù thành công hay thất bại) để lưu lại kết quả callback và phục vụ mục đích tra cứu, thống kê sau này.
            dbTransaction.ResponseCode = responseCode;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (responseCode == "00")
                {
                    // Cập nhật Payment, Order, Invoice chỉ khi thanh toán thành công (code 00).
                    // Các code lỗi khác chỉ cập nhật Transaction mà không thay đổi trạng thái Payment để cho phép người dùng retry.
                    var paymentType = (PaymentTypeEnum)payment.PaymentType!.Value;
                    // Load Order with details for status update and inventory adjustment
                    var order = await ResolveTargetOrderForPaymentAsync(payment);

                    // Update Transaction status to Completed
                    dbTransaction.Status = (int)TransactionStatusEnum.Paid;
                    // Update Payment status to Success
                    payment.Status = (int)PaymentStatusEnum.Paid;
                    payment.PaidAt = DateTime.Now;

                    // Update Order status based on payment type and current order status
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

                    if (newOrderStatus == (int)OrderStatusEnum.Paid)
                    {
                        await AssignShippersForPaidOrderAsync(order);
                    }

                    // Update inventory for OtherProduct orders (CommonPlant, NurseryMaterial, NurseryPlantCombo)
                    if (order.OrderType == (int)OrderTypeEnum.OtherProduct)
                    {
                        await UpdateInventoryForOrderAsync(order);
                    }

                    // Update PlantInstance status for PlantInstance orders
                    if (order.OrderType == (int)OrderTypeEnum.PlantInstance)
                    {
                        await UpdatePlantInstanceStatusForOrderAsync(order, paymentType);
                    }

                    // PaymentType và InvoiceType dùng cùng giá trị số: Deposit=1, FullPayment=2, RemainingBalance=3
                    // Dùng để xác định Invoice nào cần cập nhật trạng thái sang Paid sau khi thanh toán thành công, dựa trên loại thanh toán (Deposit, FullPayment, RemainingBalance).
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

                    // Only update Payment status to Cancelled if user explicitly cancelled (code 24)
                    // For other errors, keep Payment as Pending to allow retry
                    if (responseCode == "24")
                    {
                        payment.Status = (int)PaymentStatusEnum.Cancelled;
                    }
                    // For other error codes, Payment remains Pending (no status update needed)
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

        // Dùng để xác định trạng thái mới của đơn hàng sau khi thanh toán thành công dựa trên loại thanh toán (Deposit, FullPayment, RemainingBalance).
        private static int DetermineSuccessOrderStatus(PaymentTypeEnum paymentType) => paymentType switch
        {
            PaymentTypeEnum.FullPayment => (int)OrderStatusEnum.Paid,
            PaymentTypeEnum.Deposit => (int)OrderStatusEnum.DepositPaid,
            PaymentTypeEnum.RemainingBalance => (int)OrderStatusEnum.PendingConfirmation,
            _ => (int)OrderStatusEnum.Paid
        };

        private async Task<Order> ResolveTargetOrderForPaymentAsync(Payment payment)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(payment.OrderId!.Value)
                ?? throw new NotFoundException("Order not found");

            return order;
        }

        /// <summary>
        /// Update inventory (Quantity and ReservedQuantity) for OtherProduct orders.
        /// - CommonPlant: Quantity -= quantity, ReservedQuantity += quantity
        /// - NurseryMaterial: Quantity -= quantity, ReservedQuantity += quantity
        /// - NurseryPlantCombo: Quantity -= quantity (combo quantity already deducted from plants when created)
        /// </summary>
        private async Task UpdateInventoryForOrderAsync(Order order)
        {
            foreach (var nurseryOrder in order.NurseryOrders)
            {
                foreach (var detail in nurseryOrder.NurseryOrderDetails)
                {
                    var quantity = detail.Quantity ?? 0;
                    if (quantity <= 0) continue;

                    // Handle CommonPlant
                    if (detail.CommonPlantId.HasValue)
                    {
                        var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdAsync(detail.CommonPlantId.Value);
                        if (commonPlant != null)
                        {
                            commonPlant.Quantity -= quantity;
                            commonPlant.ReservedQuantity += quantity;
                            _unitOfWork.CommonPlantRepository.PrepareUpdate(commonPlant);
                        }
                    }
                    // Handle NurseryMaterial
                    else if (detail.NurseryMaterialId.HasValue)
                    {
                        var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository.GetByIdAsync(detail.NurseryMaterialId.Value);
                        if (nurseryMaterial != null)
                        {
                            nurseryMaterial.Quantity -= quantity;
                            nurseryMaterial.ReservedQuantity += quantity;
                            _unitOfWork.NurseryMaterialRepository.PrepareUpdate(nurseryMaterial);
                        }
                    }
                    // Handle NurseryPlantCombo - only update combo quantity (plant quantities already deducted when combo was created)
                    else if (detail.NurseryPlantComboId.HasValue)
                    {
                        var nurseryPlantCombo = await _unitOfWork.NurseryPlantComboRepository
                            .GetByIdAsync(detail.NurseryPlantComboId.Value);

                        if (nurseryPlantCombo != null)
                        {
                            nurseryPlantCombo.Quantity -= quantity;
                            _unitOfWork.NurseryPlantComboRepository.PrepareUpdate(nurseryPlantCombo);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update PlantInstance status for PlantInstance orders.
        /// - Deposit/FullPayment: Available → Reserved (item paid but not yet delivered)
        /// - RemainingBalance: Keep Reserved (will be Sold when delivered)
        /// </summary>
        private async Task UpdatePlantInstanceStatusForOrderAsync(Order order, PaymentTypeEnum paymentType)
        {
            // Only update to Reserved when first payment (Deposit or FullPayment), not for RemainingBalance
            if (paymentType == PaymentTypeEnum.RemainingBalance)
                return;

            foreach (var nurseryOrder in order.NurseryOrders)
            {
                foreach (var detail in nurseryOrder.NurseryOrderDetails)
                {
                    if (detail.PlantInstanceId.HasValue)
                    {
                        var plantInstance = await _unitOfWork.PlantInstanceRepository.GetByIdAsync(detail.PlantInstanceId.Value);
                        if (plantInstance != null && plantInstance.Status == (int)PlantInstanceStatusEnum.Available)
                        {
                            plantInstance.Status = (int)PlantInstanceStatusEnum.Reserved;
                            _unitOfWork.PlantInstanceRepository.PrepareUpdate(plantInstance);
                        }
                    }
                }
            }
        }

        private async Task AssignShippersForPaidOrderAsync(Order order)
        {
            _logger.LogInformation("Start assigning shippers for OrderId={OrderId}", order.Id);

            var currentOrderNurseryOrderIds = order.NurseryOrders.Select(no => no.Id).ToHashSet();

            _logger.LogInformation(
                "Current order nursery order ids: {NurseryOrderIds}",
                string.Join(", ", currentOrderNurseryOrderIds));

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            var todayStart = now.Date;
            var tomorrowStart = todayStart.AddDays(1);

            _logger.LogInformation(
                "Assign time window: Now={Now}, TodayStart={TodayStart}, TomorrowStart={TomorrowStart}",
                now, todayStart, tomorrowStart);

            var paidNurseryOrders = order.NurseryOrders
                .Where(no => no.Status == (int)OrderStatusEnum.Paid)
                .ToList();

            _logger.LogInformation(
                "Found {Count} paid nursery orders to assign for OrderId={OrderId}",
                paidNurseryOrders.Count, order.Id);

            foreach (var nurseryOrder in paidNurseryOrders)
            {
                _logger.LogInformation(
                    "Processing NurseryOrderId={NurseryOrderId}, NurseryId={NurseryId}, CurrentStatus={Status}, CurrentShipperId={ShipperId}",
                    nurseryOrder.Id, nurseryOrder.NurseryId, nurseryOrder.Status, nurseryOrder.ShipperId);

                var nurseryShippers = await _unitOfWork.UserRepository.GetShippersByNurseryIdAsync(nurseryOrder.NurseryId);

                _logger.LogInformation(
                    "Found {Count} shippers for NurseryId={NurseryId}. ShipperIds=[{ShipperIds}]",
                    nurseryShippers.Count(),
                    nurseryOrder.NurseryId,
                    string.Join(", ", nurseryShippers.Select(s => s.Id)));

                if (!nurseryShippers.Any())
                {
                    _logger.LogWarning(
                        "No shipper found for NurseryOrderId={NurseryOrderId}, NurseryId={NurseryId}. Skip assigning.",
                        nurseryOrder.Id, nurseryOrder.NurseryId);
                    continue;
                }

                var nurseryScopedExistingOrders = (await _unitOfWork.NurseryOrderRepository.GetByNurseryIdAsync(nurseryOrder.NurseryId))
                    .Where(no => !currentOrderNurseryOrderIds.Contains(no.Id))
                    .ToList();

                _logger.LogInformation(
                    "NurseryId={NurseryId} has {Count} existing nursery orders for workload calculation (excluding current order). ExistingNurseryOrderIds=[{ExistingIds}]",
                    nurseryOrder.NurseryId,
                    nurseryScopedExistingOrders.Count,
                    string.Join(", ", nurseryScopedExistingOrders.Select(x => x.Id)));

                foreach (var existingOrder in nurseryScopedExistingOrders)
                {
                    _logger.LogInformation(
                        "Existing NurseryOrderId={NurseryOrderId}, ShipperId={ShipperId}, Status={Status}, AssignedAt={AssignedAt}",
                        existingOrder.Id,
                        existingOrder.ShipperId,
                        existingOrder.Status,
                        existingOrder.AssignedAt);
                }

                var shipperMetrics = nurseryShippers
                    .Select(shipper => new
                    {
                        Shipper = shipper,
                        CurrentLoad = nurseryScopedExistingOrders.Count(no =>
                            no.ShipperId == shipper.Id &&
                            (no.Status == (int)OrderStatusEnum.Assigned || no.Status == (int)OrderStatusEnum.Shipping)),
                        DailyAssignedCount = nurseryScopedExistingOrders.Count(no =>
                            no.ShipperId == shipper.Id &&
                            no.AssignedAt.HasValue &&
                            no.AssignedAt.Value >= todayStart &&
                            no.AssignedAt.Value < tomorrowStart)
                    })
                    .ToList();

                foreach (var metric in shipperMetrics)
                {
                    _logger.LogInformation(
                        "ShipperId={ShipperId}, NurseryId={NurseryId}, CurrentLoad={CurrentLoad}, DailyAssignedCount={DailyAssignedCount}",
                        metric.Shipper.Id,
                        nurseryOrder.NurseryId,
                        metric.CurrentLoad,
                        metric.DailyAssignedCount);
                }

                var selectedShipper = shipperMetrics
                    .OrderBy(x => x.CurrentLoad)
                    .ThenBy(x => x.DailyAssignedCount)
                    .ThenBy(x => x.Shipper.Id)
                    .First()
                    .Shipper;

                _logger.LogInformation(
                    "Selected ShipperId={ShipperId} for NurseryOrderId={NurseryOrderId}, NurseryId={NurseryId}",
                    selectedShipper.Id, nurseryOrder.Id, nurseryOrder.NurseryId);

                nurseryOrder.ShipperId = selectedShipper.Id;
                nurseryOrder.AssignedAt = now;
                nurseryOrder.Status = (int)OrderStatusEnum.Assigned;
                nurseryOrder.UpdatedAt = now;

                _logger.LogInformation(
                    "Updated NurseryOrderId={NurseryOrderId}: ShipperId={ShipperId}, AssignedAt={AssignedAt}, NewStatus={Status}, UpdatedAt={UpdatedAt}",
                    nurseryOrder.Id,
                    nurseryOrder.ShipperId,
                    nurseryOrder.AssignedAt,
                    nurseryOrder.Status,
                    nurseryOrder.UpdatedAt);

                _unitOfWork.NurseryOrderRepository.PrepareUpdate(nurseryOrder);
            }

            _logger.LogInformation("Finish assigning shippers for OrderId={OrderId}", order.Id);
        }

        #endregion
    }
}
