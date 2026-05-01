using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Libraries;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private const int PaymentTimeoutMinutes = 30;
        private const int MaxRetryAttempts = 3;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IUnitOfWork unitOfWork, ICacheService cacheService, IConfiguration configuration, IBackgroundJobClient backgroundJobClient, ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _configuration = configuration;
            _backgroundJobClient = backgroundJobClient;
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
            var existingPayments = await _unitOfWork.PaymentRepository.GetByInvoiceIdAsync(invoice.Id);
            var now = DateTime.Now;

            var activePendingPayment = existingPayments.FirstOrDefault(p =>
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
                InvoiceId = invoice.Id,
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
            var order = await ResolveTargetOrderForPaymentAsync(payment);

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

            // Always expire old pending transactions so the previous payment link becomes invalid immediately.
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

        public async Task<CreatePaymentUrlResponseDto> ContinuePaymentByInvoiceAsync(int userId, int invoiceId, HttpContext httpContext)
        {
            var invoice = await _unitOfWork.InvoiceRepository.GetByIdWithDetailsAsync(invoiceId);
            if (invoice == null)
                throw new NotFoundException($"Invoice {invoiceId} not found");

            if (!invoice.OrderId.HasValue)
                throw new BadRequestException("Invoice is not associated with any order");

            var order = await _unitOfWork.OrderRepository.GetByIdAsync(invoice.OrderId.Value);
            if (order == null)
                throw new NotFoundException("Order not found");

            if (order.UserId != userId)
                throw new ForbiddenException("You don't have access to this invoice");

            if (invoice.Status != (int)InvoiceStatusEnum.Pending)
                throw new BadRequestException($"Invoice is not in Pending status. Current status: {(InvoiceStatusEnum)invoice.Status!.Value}");

            var existingPayments = await _unitOfWork.PaymentRepository.GetByInvoiceIdAsync(invoiceId);
            var now = DateTime.Now;

            var activePendingPayment = existingPayments
                .Where(p => p.Status == (int)PaymentStatusEnum.Pending)
                .OrderByDescending(p => p.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(p => p.Id)
                .FirstOrDefault(p => p.Transactions.Any(t =>
                    t.Status == (int)TransactionStatusEnum.Pending &&
                    (!t.ExpiredAt.HasValue || t.ExpiredAt.Value > now)));

            if (activePendingPayment != null)
            {
                if (activePendingPayment.Transactions.Count >= MaxRetryAttempts)
                {
                    // Reached retry limit on current active payment: close current pending transactions/payment,
                    // then create a brand new payment so customer can continue checkout.
                    foreach (var transaction in activePendingPayment.Transactions
                        .Where(t => t.Status == (int)TransactionStatusEnum.Pending))
                    {
                        transaction.Status = (int)TransactionStatusEnum.TimedOut;
                        transaction.ExpiredAt = now;
                        _unitOfWork.TransactionRepository.PrepareUpdate(transaction);
                    }

                    activePendingPayment.Status = (int)PaymentStatusEnum.Failed;
                    _unitOfWork.PaymentRepository.PrepareUpdate(activePendingPayment);
                    await _unitOfWork.SaveAsync();

                    return await CreatePaymentUrlAsync(userId, new CreatePaymentRequestDto { InvoiceId = invoiceId }, httpContext);
                }

                return await RetryPaymentAsync(userId, activePendingPayment.Id, httpContext);
            }

            return await CreatePaymentUrlAsync(userId, new CreatePaymentRequestDto { InvoiceId = invoiceId }, httpContext);
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
            var shouldInvalidateInventoryCaches = false;
            var shouldEnqueueOrderSuccessEmail = false;
            var orderIdForSuccessEmail = 0;
            var updatedPlantInstanceIds = new List<int>();
            try
            {
                if (responseCode == "00")
                {
                    // Cập nhật Payment, Order, Invoice chỉ khi thanh toán thành công (code 00).
                    // Các code lỗi khác chỉ cập nhật Transaction mà không thay đổi trạng thái Payment để cho phép người dùng retry.
                    var paymentType = (PaymentTypeEnum)payment.PaymentType!.Value;
                    // Load Order with details for status update and inventory adjustment
                    var order = await ResolveTargetOrderForPaymentAsync(payment);
                    orderIdForSuccessEmail = order.Id;

                    // Update Transaction status to Completed
                    dbTransaction.Status = (int)TransactionStatusEnum.Paid;
                    // Update Payment status to Success
                    payment.Status = (int)PaymentStatusEnum.Paid;
                    payment.PaidAt = DateTime.Now;

                    // Update Order status based on payment type and order type
                    int newOrderStatus;
                    if (order.OrderType == (int)OrderTypeEnum.Design && paymentType == PaymentTypeEnum.RemainingBalance)
                    {
                        // Design order with remaining balance payment → Completed
                        newOrderStatus = (int)OrderStatusEnum.Completed;
                    }
                    else
                    {
                        // Other orders → use default status determination
                        newOrderStatus = DetermineSuccessOrderStatus(paymentType);
                    }

                    order.Status = newOrderStatus;
                    order.UpdatedAt = DateTime.Now;
                    
                    // Clear remaining amount if payment type is RemainingBalance
                    if (paymentType == PaymentTypeEnum.RemainingBalance)
                    {
                        order.RemainingAmount = 0;
                    }
                    
                    _unitOfWork.OrderRepository.PrepareUpdate(order);

                    // Update all NurseryOrder status to match parent Order status
                    foreach (var nurseryOrder in order.NurseryOrders)
                    {
                        nurseryOrder.Status = newOrderStatus;
                        nurseryOrder.UpdatedAt = DateTime.Now;
                    }

                    if (newOrderStatus == (int)OrderStatusEnum.Paid
                        || newOrderStatus == (int)OrderStatusEnum.DepositPaid)
                    {
                        await AssignShippersForPaidOrderAsync(order);
                    }

                    // Update inventory for OtherProduct orders (cart + buy-now)
                    if (order.OrderType == (int)OrderTypeEnum.OtherProduct
                        || order.OrderType == (int)OrderTypeEnum.OtherProductBuyNow)
                    {
                        await UpdateInventoryForOrderAsync(order);
                    }

                    // Update PlantInstance status for PlantInstance orders
                    if (order.OrderType == (int)OrderTypeEnum.PlantInstance)
                    {
                        updatedPlantInstanceIds = await UpdatePlantInstanceStatusForOrderAsync(order, paymentType);
                    }

                    // Generate care service schedule for Service orders
                    if (order.OrderType == (int)OrderTypeEnum.Service)
                    {
                        var serviceRegistration = await _unitOfWork.ServiceRegistrationRepository.GetByOrderIdAsync(order.Id);
                        if (serviceRegistration != null)
                        {
                            _backgroundJobClient.Enqueue<IServiceCareBackgroundJobService>(
                                service => service.GenerateServiceScheduleAsync(serviceRegistration.Id));
                        }
                    }

                    // Synchronize design registration lifecycle with payment milestones.
                    if (order.OrderType == (int)OrderTypeEnum.Design)
                    {
                        var designRegistration = await _unitOfWork.DesignRegistrationRepository.GetByOrderIdAsync(order.Id);
                        if (designRegistration != null)
                        {
                            if (paymentType == PaymentTypeEnum.Deposit)
                            {
                                await ActivateDesignRegistrationAndGenerateTasksAsync(designRegistration.Id);
                            }
                            else if (paymentType == PaymentTypeEnum.RemainingBalance)
                            {
                                await CompleteDesignRegistrationAfterRemainingPaymentAsync(designRegistration.Id);
                            }
                        }
                    }

                    var invoice = await ResolveTargetInvoiceForPaymentAsync(payment);

                    invoice.Status = (int)InvoiceStatusEnum.Paid;
                    _unitOfWork.InvoiceRepository.PrepareUpdate(invoice);

                    shouldInvalidateInventoryCaches = true;
                    shouldEnqueueOrderSuccessEmail = true;
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

            if (shouldInvalidateInventoryCaches)
            {
                await InvalidateInventoryAndShopCachesAsync();
            }

            foreach (var plantInstanceId in updatedPlantInstanceIds)
            {
                await QueuePlantInstanceEmbeddingByIdAsync(plantInstanceId);
            }

            if (shouldEnqueueOrderSuccessEmail && orderIdForSuccessEmail > 0)
            {
                try
                {
                    _backgroundJobClient.Enqueue<IEmailBackgroundJobService>(
                        service => service.SendOrderSuccessEmailAsync(orderIdForSuccessEmail));
                }
                catch (Exception ex)
                {
                    // Payment was committed successfully; do not fail VNPay IPN confirmation because enqueue failed.
                    _logger.LogError(
                        ex,
                        "Failed to enqueue order success email job for OrderId={OrderId}",
                        orderIdForSuccessEmail);
                }
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
            if (payment.OrderId.HasValue)
            {
                var orderByOrderId = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(payment.OrderId.Value);
                if (orderByOrderId != null)
                    return orderByOrderId;
            }

            if (payment.InvoiceId.HasValue)
            {
                var invoice = await _unitOfWork.InvoiceRepository.GetByIdWithDetailsAsync(payment.InvoiceId.Value)
                    ?? throw new NotFoundException("Invoice not found");

                if (!invoice.OrderId.HasValue)
                    throw new BadRequestException("Invoice is not associated with any order");

                var orderByInvoice = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(invoice.OrderId.Value)
                    ?? throw new NotFoundException("Order not found");

                return orderByInvoice;
            }

            throw new BadRequestException("Payment is not associated with order or invoice");
        }

        private async Task<Invoice> ResolveTargetInvoiceForPaymentAsync(Payment payment)
        {
            if (payment.InvoiceId.HasValue)
            {
                var linkedInvoice = await _unitOfWork.InvoiceRepository.GetByIdWithDetailsAsync(payment.InvoiceId.Value)
                    ?? throw new NotFoundException("Invoice not found");

                return linkedInvoice;
            }

            if (payment.OrderId.HasValue && payment.PaymentType.HasValue)
            {
                var fallbackInvoice = await _unitOfWork.InvoiceRepository
                    .GetPendingByOrderIdAndTypeAsync(payment.OrderId.Value, payment.PaymentType.Value)
                    ?? throw new NotFoundException($"Invoice not found for order {payment.OrderId.Value}");

                return fallbackInvoice;
            }

            throw new BadRequestException("Payment is not associated with any invoice");
        }

        private async Task CompleteDesignRegistrationAfterRemainingPaymentAsync(int designRegistrationId)
        {
            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(designRegistrationId);
            if (registration == null)
                return;

            if (registration.Status == (int)DesignRegistrationStatus.Completed
                || registration.Status == (int)DesignRegistrationStatus.Cancelled
                || registration.Status == (int)DesignRegistrationStatus.Rejected)
            {
                return;
            }

            registration.Status = (int)DesignRegistrationStatus.Completed;
            _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);
        }

        private async Task ActivateDesignRegistrationAndGenerateTasksAsync(int designRegistrationId)
        {
            var registrationDetail = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(designRegistrationId);
            if (registrationDetail == null)
                return;

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(designRegistrationId);
            if (registration == null)
                return;

            if (registration.Status != (int)DesignRegistrationStatus.AwaitDeposit
                && registration.Status != (int)DesignRegistrationStatus.DepositPaid
                && registration.Status != (int)DesignRegistrationStatus.InProgress)
            {
                return;
            }

            registration.Status = (int)DesignRegistrationStatus.DepositPaid;
            _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);

            var existingTasks = await _unitOfWork.DesignTaskRepository.GetByRegistrationIdAsync(designRegistrationId);
            if (existingTasks.Any())
            {
                var orderedExistingTasks = existingTasks
                    .OrderBy(x => x.ScheduledDate ?? DateOnly.MaxValue)
                    .ThenBy(x => x.Id)
                    .ToList();

                if (registration.AssignedCaretakerId.HasValue)
                {
                    for (var index = 0; index < orderedExistingTasks.Count; index++)
                    {
                        var task = orderedExistingTasks[index];
                        var trackedTask = await _unitOfWork.DesignTaskRepository.GetByIdAsync(task.Id);
                        if (trackedTask == null)
                            continue;

                        trackedTask.TaskType = ResolveTaskTypeByIndex(index, orderedExistingTasks.Count);

                        if (task.Status == (int)DesignTaskStatusEnum.Completed
                            || task.Status == (int)DesignTaskStatusEnum.Cancelled)
                        {
                            _unitOfWork.DesignTaskRepository.PrepareUpdate(trackedTask);
                            continue;
                        }

                        trackedTask.AssignedStaffId = registration.AssignedCaretakerId;
                        if (trackedTask.Status == (int)DesignTaskStatusEnum.Pending)
                        {
                            trackedTask.Status = (int)DesignTaskStatusEnum.Assigned;
                        }
                        _unitOfWork.DesignTaskRepository.PrepareUpdate(trackedTask);
                    }
                }
                else
                {
                    for (var index = 0; index < orderedExistingTasks.Count; index++)
                    {
                        var task = orderedExistingTasks[index];
                        var trackedTask = await _unitOfWork.DesignTaskRepository.GetByIdAsync(task.Id);
                        if (trackedTask == null)
                            continue;

                        trackedTask.TaskType = ResolveTaskTypeByIndex(index, orderedExistingTasks.Count);
                        if (task.Status != (int)DesignTaskStatusEnum.Completed
                            && task.Status != (int)DesignTaskStatusEnum.Cancelled)
                        {
                            trackedTask.ScheduledDate = null;
                        }
                        _unitOfWork.DesignTaskRepository.PrepareUpdate(trackedTask);
                    }
                }

                return;
            }

            var estimatedDays = registrationDetail.DesignTemplateTier?.EstimatedDays ?? 1;
            if (estimatedDays <= 0)
                estimatedDays = 1;

            var assignedStaffId = registration.AssignedCaretakerId;
            var taskStatus = assignedStaffId.HasValue
                ? (int)DesignTaskStatusEnum.Assigned
                : (int)DesignTaskStatusEnum.Pending;

            for (var index = 0; index < estimatedDays; index++)
            {
                var task = new DesignTask
                {
                    DesignRegistrationId = designRegistrationId,
                    AssignedStaffId = assignedStaffId,
                    ScheduledDate = null,
                    TaskType = ResolveTaskTypeByIndex(index, estimatedDays),
                    Status = taskStatus,
                    CreatedAt = DateTime.Now
                };
                _unitOfWork.DesignTaskRepository.PrepareCreate(task);
            }
        }

        private static List<DateOnly> BuildConsecutiveWorkingDates(DateOnly startDate, int totalDays)
        {
            var dates = new List<DateOnly>();
            var cursor = startDate;

            while (dates.Count < totalDays)
            {
                if (cursor.DayOfWeek != DayOfWeek.Sunday)
                {
                    dates.Add(cursor);
                }

                cursor = cursor.AddDays(1);
            }

            return dates;
        }

        private static int ResolveTaskTypeByIndex(int index, int totalTasks)
        {
            if (totalTasks <= 1 || index == 0)
                return (int)TaskTypeEnum.Survey;

            if (index == totalTasks - 1)
                return (int)TaskTypeEnum.Acceptance;

            return (int)TaskTypeEnum.Construction;
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
        private async Task<List<int>> UpdatePlantInstanceStatusForOrderAsync(Order order, PaymentTypeEnum paymentType)
        {
            var updatedPlantInstanceIds = new List<int>();

            // Only update to Reserved when first payment (Deposit or FullPayment), not for RemainingBalance
            if (paymentType == PaymentTypeEnum.RemainingBalance)
                return updatedPlantInstanceIds;

            var paymentStrategies = (PaymentStrategiesEnum)order.PaymentStrategy;

            foreach (var nurseryOrder in order.NurseryOrders)
            {
                foreach (var detail in nurseryOrder.NurseryOrderDetails)
                {
                    if (detail.PlantInstanceId.HasValue)
                    {
                        var plantInstance = await _unitOfWork.PlantInstanceRepository.GetByIdAsync(detail.PlantInstanceId.Value);
                        if (plantInstance != null && plantInstance.Status == (int)PlantInstanceStatusEnum.Available && paymentStrategies == PaymentStrategiesEnum.Deposit)
                        {
                            plantInstance.Status = (int)PlantInstanceStatusEnum.Reserved;
                            _unitOfWork.PlantInstanceRepository.PrepareUpdate(plantInstance);
                            updatedPlantInstanceIds.Add(plantInstance.Id);
                        }
                        else if (plantInstance != null && plantInstance.Status == (int)PlantInstanceStatusEnum.Available && paymentStrategies == PaymentStrategiesEnum.FullPayment)
                        {
                            plantInstance.Status = (int)PlantInstanceStatusEnum.Sold;
                            _unitOfWork.PlantInstanceRepository.PrepareUpdate(plantInstance);
                            updatedPlantInstanceIds.Add(plantInstance.Id);
                        }
                    }
                }
            }

            return updatedPlantInstanceIds;
        }

        private async Task QueuePlantInstanceEmbeddingByIdAsync(int plantInstanceId)
        {
            try
            {
                var plantInstance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(plantInstanceId);
                if (plantInstance == null)
                {
                    return;
                }

                var entityId = ConvertToGuid(plantInstance.Id);
                _backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                    service => service.ProcessPlantInstanceEmbeddingAsync(
                        plantInstance.ToEmbeddingBackfillDto(),
                        entityId,
                        EmbeddingEntityTypes.PlantInstance));
            }
            catch
            {
                // Re-embedding is best-effort and should not fail payment success handling.
            }
        }

        private static Guid ConvertToGuid(int id)
            => new Guid(id.ToString().PadLeft(32, '0'));

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
                .Where(no => no.Status == (int)OrderStatusEnum.Paid
                    || no.Status == (int)OrderStatusEnum.DepositPaid)
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

        private async Task InvalidateInventoryAndShopCachesAsync()
        {
            try
            {
                // Keep this aligned with inventory/search cache keys in CommonPlant/Material/Combo/PlantInstance/ShopSearch services.
                var cachePrefixes = new[]
                {
                    "common_plants_all",
                    "nursery_common_plants",
                    "plant_nurseries_common",
                    "plants_shop_search",
                    "nursery_materials_all",
                    "materials_shop",
                    "combos_shop",
                    "selling_combos_",
                    "nursery_instances",
                    "plant_nurseries",
                    "nurseries_all_",
                    "shop_unified_search"
                };

                foreach (var prefix in cachePrefixes)
                {
                    await _cacheService.RemoveByPrefixAsync(prefix);
                }
            }
            catch (Exception ex)
            {
                // Payment was already committed successfully; avoid failing IPN response due to cache-layer issues.
                _logger.LogError(ex, "Failed to invalidate inventory/shop caches after payment commit");
            }
        }

        #endregion
    }
}
