using Microsoft.EntityFrameworkCore;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class NurseryOrderService : INurseryOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public NurseryOrderService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<PaginatedResult<NurseryOrderResponseDto>> GetMyNurseryOrdersAsync(int currentUserId, int? status, Pagination pagination)
        {
            var currentUser = await GetValidatedShipperAsync(currentUserId);

            var (items, totalCount) = await _unitOfWork.NurseryOrderRepository.GetByShipperAndNurseryPagedAsync(
                currentUserId,
                currentUser.NurseryId!.Value,
                status,
                pagination.Skip,
                pagination.Take);

            return new PaginatedResult<NurseryOrderResponseDto>(
                items.Select(MapToDto),
                totalCount,
                pagination.PageNumber,
                pagination.PageSize);
        }

        public async Task<List<InvoiceResponseDto>> GetPendingInvoicesForMyNurseryAsync(int currentUserId)
        {
            var currentUser = await GetValidatedShipperAsync(currentUserId);

            var invoices = await _unitOfWork.InvoiceRepository.GetPendingRemainingInvoicesForShipperAsync(
                currentUserId,
                currentUser.NurseryId!.Value);

            return invoices.Select(MapInvoiceToDto).ToList();
        }

        public async Task<NurseryOrderResponseDto> GetNurseryOrderDetailForManagerAsync(int currentUserId, int nurseryOrderId)
        {
            var currentUser = await GetValidatedManagerAsync(currentUserId);

            var nurseryOrder = await _unitOfWork.NurseryOrderRepository.GetByIdWithDetailsAsync(nurseryOrderId)
                ?? throw new NotFoundException($"NurseryOrder {nurseryOrderId} not found");

            if (nurseryOrder.NurseryId != currentUser.NurseryId.Value)
                throw new ForbiddenException("You don't have permission to access this nursery order");

            return MapToDto(nurseryOrder);
        }

        public async Task<RevenueSummaryResponseDto> GetMyNurseryRevenueSummaryAsync(int currentUserId, DateTime from, DateTime to)
        {
            var currentUser = await GetValidatedManagerAsync(currentUserId);
            var (fromInclusive, toExclusive) = NormalizeRevenueDateRange(from, to);

            var totalRevenue = await _unitOfWork.NurseryOrderRepository
                .GetCompletedRevenueByNurseryAsync(currentUser.NurseryId!.Value, fromInclusive, toExclusive);
            var totalOrders = await _unitOfWork.NurseryOrderRepository
                .CountCompletedOrdersByNurseryAsync(currentUser.NurseryId.Value, fromInclusive, toExclusive);

            return new RevenueSummaryResponseDto
            {
                From = fromInclusive,
                To = toExclusive.AddTicks(-1),
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders
            };
        }

        public async Task<RevenueSummaryResponseDto> GetSystemRevenueSummaryAsync(int currentUserId, DateTime from, DateTime to)
        {
            await GetValidatedAdminAsync(currentUserId);
            var (fromInclusive, toExclusive) = NormalizeRevenueDateRange(from, to);

            var totalRevenue = await _unitOfWork.NurseryOrderRepository
                .GetCompletedSystemRevenueAsync(fromInclusive, toExclusive);
            var totalOrders = await _unitOfWork.NurseryOrderRepository
                .CountCompletedSystemOrdersAsync(fromInclusive, toExclusive);

            return new RevenueSummaryResponseDto
            {
                From = fromInclusive,
                To = toExclusive.AddTicks(-1),
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders
            };
        }

        public async Task<List<NurseryRevenueItemResponseDto>> GetSystemRevenueByNurseryAsync(int currentUserId, DateTime from, DateTime to)
        {
            await GetValidatedAdminAsync(currentUserId);
            var (fromInclusive, toExclusive) = NormalizeRevenueDateRange(from, to);

            var items = await _unitOfWork.NurseryOrderRepository
                .GetCompletedRevenueByNurseryListAsync(fromInclusive, toExclusive);

            return items.Select(x => new NurseryRevenueItemResponseDto
            {
                NurseryId = x.NurseryId,
                NurseryName = x.NurseryName,
                Revenue = x.Revenue,
                TotalOrders = x.TotalOrders
            }).ToList();
        }

        public async Task<List<NurseryOrderShipperResponseDto>> GetNurseryShippersForManagerAsync(int currentUserId)
        {
            var currentUser = await GetValidatedManagerAsync(currentUserId);

            var shippers = await _unitOfWork.UserRepository.GetShippersByNurseryIdAsync(currentUser.NurseryId!.Value);
            var now = GetCurrentVietnamTime();
            var todayStart = now.Date;
            var tomorrowStart = todayStart.AddDays(1);

            var nurseryOrders = await _unitOfWork.NurseryOrderRepository.GetByNurseryIdAsync(currentUser.NurseryId.Value);
            var shipperOrderCountInDay = nurseryOrders
                .Where(no => no.ShipperId.HasValue
                    && no.AssignedAt.HasValue
                    && no.AssignedAt.Value >= todayStart
                    && no.AssignedAt.Value < tomorrowStart)
                .GroupBy(no => no.ShipperId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            return shippers
                .Select(s => MapShipperToDto(s, shipperOrderCountInDay.TryGetValue(s.Id, out var count) ? count : 0))
                .ToList();
        }

        public async Task<NurseryOrderResponseDto> UpdateNurseryOrderShipperForManagerAsync(int currentUserId, int nurseryOrderId, int shipperId)
        {
            var currentUser = await GetValidatedManagerAsync(currentUserId);

            var nurseryOrder = await _unitOfWork.NurseryOrderRepository.GetByIdWithDetailsAsync(nurseryOrderId)
                ?? throw new NotFoundException($"NurseryOrder {nurseryOrderId} not found");

            if (nurseryOrder.NurseryId != currentUser.NurseryId.Value)
                throw new ForbiddenException("You don't have permission to update this nursery order");

            if (nurseryOrder.Status != (int)OrderStatusEnum.Assigned
                && nurseryOrder.Status != (int)OrderStatusEnum.Paid
                && nurseryOrder.Status != (int)OrderStatusEnum.DepositPaid)
                throw new BadRequestException("Chỉ có thể cập nhật shipper cho đơn ở trạng thái DepositPaid, Paid hoặc Assigned.");

            var shipper = await _unitOfWork.UserRepository.GetByIdAsync(shipperId)
                ?? throw new NotFoundException($"Shipper {shipperId} not found");

            if (shipper.RoleId != (int)RoleEnum.Shipper
                || shipper.NurseryId != currentUser.NurseryId
                || shipper.Status != (int)UserStatusEnum.Active
                || !shipper.IsVerified)
                throw new BadRequestException("Shipper không hợp lệ hoặc không thuộc vườn của bạn.");

            var now = GetCurrentVietnamTime();

            nurseryOrder.ShipperId = shipper.Id;
            nurseryOrder.AssignedAt = now;
            if (nurseryOrder.Status == (int)OrderStatusEnum.Paid || nurseryOrder.Status == (int)OrderStatusEnum.DepositPaid)
                nurseryOrder.Status = (int)OrderStatusEnum.Assigned;
            nurseryOrder.UpdatedAt = now;

            _unitOfWork.NurseryOrderRepository.PrepareUpdate(nurseryOrder);
            await _unitOfWork.SaveAsync();

            var updatedNurseryOrder = await _unitOfWork.NurseryOrderRepository.GetByIdWithDetailsAsync(nurseryOrderId)
                ?? throw new NotFoundException($"NurseryOrder {nurseryOrderId} not found");

            return MapToDto(updatedNurseryOrder);
        }

        public async Task<PaginatedResult<NurseryOrderResponseDto>> GetNurseryOrdersAsync(int currentUserId, int? status, Pagination pagination)
        {
            var currentUser = await GetValidatedManagerAsync(currentUserId);

            var (items, totalCount) = await _unitOfWork.NurseryOrderRepository.GetByNurseryIdPagedAsync(
                currentUser.NurseryId.Value,
                status,
                pagination.Skip,
                pagination.Take);

            return new PaginatedResult<NurseryOrderResponseDto>(
                items.Select(MapToDto),
                totalCount,
                pagination.PageNumber,
                pagination.PageSize);
        }

        public async Task<NurseryOrderResponseDto> GetNurseryOrderDetailForShipperAsync(int currentUserId, int nurseryOrderId)
        {
            var currentUser = await GetValidatedShipperAsync(currentUserId);
            var nurseryOrder = await _unitOfWork.NurseryOrderRepository.GetByIdWithDetailsAsync(nurseryOrderId)
                ?? throw new NotFoundException($"NurseryOrder {nurseryOrderId} not found");

            ValidateOwnership(currentUser, nurseryOrder);

            return MapToDto(nurseryOrder);
        }

        public async Task<NurseryOrderResponseDto> StartShippingAsync(int currentUserId, int nurseryOrderId, StartShippingRequestDto request)
        {
            var currentUser = await GetValidatedShipperAsync(currentUserId);
            var nurseryOrder = await _unitOfWork.NurseryOrderRepository.GetByIdWithDetailsAsync(nurseryOrderId)
                ?? throw new NotFoundException($"NurseryOrder {nurseryOrderId} not found");

            ValidateOwnership(currentUser, nurseryOrder);

            if (nurseryOrder.Status != (int)OrderStatusEnum.Assigned)
                throw new BadRequestException("Đơn không ở trạng thái có thể bắt đầu giao.");

            var now = GetCurrentVietnamTime();
            nurseryOrder.Status = (int)OrderStatusEnum.Shipping;
            nurseryOrder.ShippingStartedAt = now;
            nurseryOrder.ShipperNote = request.ShipperNote;
            nurseryOrder.UpdatedAt = now;

            var parentOrder = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(nurseryOrder.OrderId);
            if (parentOrder != null)
            {
                if (parentOrder.Status != (int)OrderStatusEnum.Shipping)
                {
                    parentOrder.Status = (int)OrderStatusEnum.Shipping;
                    parentOrder.UpdatedAt = now;
                    _unitOfWork.OrderRepository.PrepareUpdate(parentOrder);
                }
            }

            _unitOfWork.NurseryOrderRepository.PrepareUpdate(nurseryOrder);
            await _unitOfWork.SaveAsync();

            return MapToDto(nurseryOrder);


        }

        public async Task<NurseryOrderResponseDto> MarkDeliveredAsync(int currentUserId, int nurseryOrderId, MarkDeliveredRequestDto request)
        {
            var currentUser = await GetValidatedShipperAsync(currentUserId);
            var nurseryOrder = await _unitOfWork.NurseryOrderRepository.GetByIdWithDetailsAsync(nurseryOrderId)
                ?? throw new NotFoundException($"NurseryOrder {nurseryOrderId} not found");

            ValidateOwnership(currentUser, nurseryOrder);

            if (nurseryOrder.Status != (int)OrderStatusEnum.Shipping)
                throw new BadRequestException("đơn chưa ở  trạng thái đang giao.");

            string? deliveryImageUrl = null;
            if (request.DeliveryImage != null)
            {
                var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(request.DeliveryImage);
                if (!isValid)
                    throw new BadRequestException(errorMessage);

                var uploadResult = await _cloudinaryService.UploadFileAsync(request.DeliveryImage, "NurseryOrderDelivery");
                deliveryImageUrl = uploadResult.SecureUrl;
            }

            if (!string.IsNullOrWhiteSpace(request.DeliveryNote) && request.DeliveryNote.Length > 255)
                throw new BadRequestException("Delivery note is too long");

            var now = GetCurrentVietnamTime();
            nurseryOrder.Status = (int)OrderStatusEnum.Delivered;
            nurseryOrder.DeliveredAt = now;
            nurseryOrder.DeliveryNote = request.DeliveryNote;
            nurseryOrder.DeliveryImageUrl = deliveryImageUrl;
            nurseryOrder.UpdatedAt = now;

            var parentOrder = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(nurseryOrder.OrderId);
            if (parentOrder != null)
            {
                if (parentOrder.Status != (int)OrderStatusEnum.Delivered)
                {
                    parentOrder.Status = (int)OrderStatusEnum.Delivered;
                    parentOrder.UpdatedAt = now;
                }

                var areAllNurseryOrdersDeliveredOrAbove = parentOrder.NurseryOrders
                    .All(no => no.Id == nurseryOrder.Id || (no.Status.HasValue && no.Status.Value >= (int)OrderStatusEnum.Delivered));

                if (areAllNurseryOrdersDeliveredOrAbove)
                {
                    if (parentOrder.PaymentStrategy == (int)PaymentStrategiesEnum.Deposit)
                    {
                        await EnsureRemainingBalanceInvoiceForDepositAsync(parentOrder, now);
                        parentOrder.Status = (int)OrderStatusEnum.RemainingPaymentPending;
                    }
                    else
                    {
                        parentOrder.Status = (int)OrderStatusEnum.PendingConfirmation;
                    }

                    parentOrder.UpdatedAt = now;
                }

                _unitOfWork.OrderRepository.PrepareUpdate(parentOrder);
            }

            _unitOfWork.NurseryOrderRepository.PrepareUpdate(nurseryOrder);
            await _unitOfWork.SaveAsync();

            return MapToDto(nurseryOrder);
        }

        public async Task<NurseryOrderResponseDto> MarkDeliveryFailedAsync(int currentUserId, int nurseryOrderId, MarkDeliveryFailedRequestDto request)
        {
            var currentUser = await GetValidatedShipperAsync(currentUserId);
            var nurseryOrder = await _unitOfWork.NurseryOrderRepository.GetByIdWithDetailsAsync(nurseryOrderId)
                ?? throw new NotFoundException($"NurseryOrder {nurseryOrderId} not found");

            ValidateOwnership(currentUser, nurseryOrder);

            if (nurseryOrder.Status != (int)OrderStatusEnum.Shipping)
                throw new BadRequestException("Đơn chưa ở trạng thái đang giao.");

            var now = GetCurrentVietnamTime();
            nurseryOrder.Status = (int)OrderStatusEnum.Failed;
            nurseryOrder.DeliveryNote = request.FailureReason;
            nurseryOrder.UpdatedAt = now;

            _unitOfWork.NurseryOrderRepository.PrepareUpdate(nurseryOrder);
            await _unitOfWork.SaveAsync();

            return MapToDto(nurseryOrder);
        }

        private async Task<User> GetValidatedShipperAsync(int currentUserId)
        {
            var currentUser = await _unitOfWork.UserRepository.GetByIdAsync(currentUserId)
                ?? throw new UnauthorizedException("Unable to identify user from token");

            if (currentUser.RoleId != (int)RoleEnum.Shipper)
                throw new ForbiddenException("Only shipper can access this resource");

            if (currentUser.Status != (int)UserStatusEnum.Active || !currentUser.IsVerified)
                throw new ForbiddenException("Shipper account is not active or verified");

            if (!currentUser.NurseryId.HasValue)
                throw new ForbiddenException("Shipper is not assigned to any nursery");

            return currentUser;
        }

        private async Task<User> GetValidatedAdminAsync(int currentUserId)
        {
            var currentUser = await _unitOfWork.UserRepository.GetByIdAsync(currentUserId)
                ?? throw new UnauthorizedException("Unable to identify user from token");

            if (currentUser.RoleId != (int)RoleEnum.Admin)
                throw new ForbiddenException("Only admin can access this resource");

            return currentUser;
        }

        private static (DateTime FromInclusive, DateTime ToExclusive) NormalizeRevenueDateRange(DateTime from, DateTime to)
        {
            var fromInclusive = from.Date;
            var toExclusive = to.Date.AddDays(1);

            if (fromInclusive >= toExclusive)
                throw new BadRequestException("Invalid date range: 'from' must be less than or equal to 'to'");

            return (fromInclusive, toExclusive);
        }

        private async Task<User> GetValidatedManagerAsync(int currentUserId)
        {
            var currentUser = await _unitOfWork.UserRepository.GetByIdAsync(currentUserId)
                ?? throw new UnauthorizedException("Unable to identify user from token");

            if (currentUser.RoleId != (int)RoleEnum.Manager)
                throw new ForbiddenException("Only manager can access this resource");

            if (!currentUser.NurseryId.HasValue)
                throw new ForbiddenException("Manager is not assigned to any nursery");

            return currentUser;
        }

        private static void ValidateOwnership(User currentUser, NurseryOrder nurseryOrder)
        {
            if (nurseryOrder.ShipperId != currentUser.Id || nurseryOrder.NurseryId != currentUser.NurseryId)
                throw new ForbiddenException("You don't have permission to modify this nursery order");
        }

        private static NurseryOrderResponseDto MapToDto(NurseryOrder order) => new()
        {
            Id = order.Id,
            OrderId = order.OrderId,
            NurseryId = order.NurseryId,
            NurseryName = order.Nursery?.Name,
            ShipperId = order.ShipperId,
            ShipperName = order.Shipper?.Username ?? order.Shipper?.Email,
            ShipperEmail = order.Shipper?.Email,
            ShipperPhone = order.Shipper?.PhoneNumber,
            CustomerId = order.Order?.UserId ?? 0,
            CustomerName = order.Order?.CustomerName,
            CustomerEmail = order.Order?.Customer?.Email,
            CustomerPhone = order.Order?.Phone ?? order.Order?.Customer?.PhoneNumber,
            CustomerAddress = order.Order?.Address,
            SubTotalAmount = order.SubTotalAmount,
            TotalAmount = order.Order?.TotalAmount,
            DepositAmount = order.DepositAmount ?? order.Order?.DepositAmount,
            RemainingAmount = order.RemainingAmount ?? order.Order?.RemainingAmount,
            Status = order.Status,
            StatusName = order.Status.HasValue ? ((OrderStatusEnum)order.Status.Value).ToString() : null,
            ShipperNote = order.ShipperNote,
            DeliveryNote = order.DeliveryNote,
            DeliveryImageUrl = order.DeliveryImageUrl,
            Note = order.Note,
            Items = order.NurseryOrderDetails
                .Select(d => d.ToOrderItemResponse())
                .ToList()
        };

        private static InvoiceResponseDto MapInvoiceToDto(Invoice invoice) => new()
        {
            Id = invoice.Id,
            OrderId = invoice.OrderId,
            IssuedDate = invoice.IssuedDate,
            TotalAmount = invoice.TotalAmount,
            Type = invoice.Type,
            TypeName = invoice.Type.HasValue ? ((InvoiceTypeEnum)invoice.Type.Value).ToString() : null,
            Status = invoice.Status,
            StatusName = invoice.Status.HasValue ? ((InvoiceStatusEnum)invoice.Status.Value).ToString() : null,
            Details = invoice.InvoiceDetails.Select(d => new InvoiceDetailResponseDto
            {
                Id = d.Id,
                ItemName = d.ItemName,
                UnitPrice = d.UnitPrice,
                Quantity = d.Quantity,
                Amount = d.Amount
            }).ToList()
        };

        private static NurseryOrderShipperResponseDto MapShipperToDto(User shipper, int totalOrdersInDay) => new()
        {
            Id = shipper.Id,
            Username = shipper.Username,
            Email = shipper.Email,
            PhoneNumber = shipper.PhoneNumber,
            TotalOrdersInDay = totalOrdersInDay
        };

        private static DateTime GetCurrentVietnamTime()
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            }
            catch
            {
                return DateTime.UtcNow.AddHours(7);
            }
        }

        private async Task EnsureRemainingBalanceInvoiceForDepositAsync(Order order, DateTime issuedDate)
        {
            if ((order.RemainingAmount ?? 0) <= 0)
            {
                return;
            }

            var hasExistingRemainingInvoice = order.Invoices.Any(i =>
                i.Type == (int)InvoiceTypeEnum.RemainingBalance &&
                i.Status != (int)InvoiceStatusEnum.Cancelled);

            if (hasExistingRemainingInvoice)
            {
                return;
            }

            var invoiceDetails = order.NurseryOrders
                .SelectMany(no => no.NurseryOrderDetails)
                .Select(d => new InvoiceDetail
                {
                    ItemName = d.ItemName,
                    UnitPrice = d.UnitPrice,
                    Quantity = d.Quantity,
                    Amount = d.Amount
                })
                .ToList();

            if (!invoiceDetails.Any())
            {
                throw new BadRequestException($"Cannot create remaining invoice for order {order.Id} because no invoice details were found");
            }

            var remainingInvoice = new Invoice
            {
                OrderId = order.Id,
                Type = (int)InvoiceTypeEnum.RemainingBalance,
                TotalAmount = order.RemainingAmount,
                Status = (int)InvoiceStatusEnum.Pending,
                IssuedDate = issuedDate,
                InvoiceDetails = invoiceDetails
            };

            _unitOfWork.InvoiceRepository.PrepareCreate(remainingInvoice);
        }
    }
}
