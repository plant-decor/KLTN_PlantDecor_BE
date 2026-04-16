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

        public NurseryOrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

        public async Task<NurseryOrderResponseDto> GetNurseryOrderDetailForManagerAsync(int currentUserId, int nurseryOrderId)
        {
            var currentUser = await _unitOfWork.UserRepository.GetByIdAsync(currentUserId)
                ?? throw new UnauthorizedException("Unable to identify user from token");

            if (currentUser.RoleId != (int)RoleEnum.Manager)
                throw new ForbiddenException("Only manager can access this resource");

            if (!currentUser.NurseryId.HasValue)
                throw new ForbiddenException("Manager is not assigned to any nursery");

            var nurseryOrder = await _unitOfWork.NurseryOrderRepository.GetByIdWithDetailsAsync(nurseryOrderId)
                ?? throw new NotFoundException($"NurseryOrder {nurseryOrderId} not found");

            if (nurseryOrder.NurseryId != currentUser.NurseryId.Value)
                throw new ForbiddenException("You don't have permission to access this nursery order");

            return MapToDto(nurseryOrder);
        }

        public async Task<PaginatedResult<NurseryOrderResponseDto>> GetNurseryOrdersAsync(int currentUserId, int? status, Pagination pagination)
        {
            var currentUser = await _unitOfWork.UserRepository.GetByIdAsync(currentUserId)
                ?? throw new UnauthorizedException("Unable to identify user from token");

            if (currentUser.RoleId != (int)RoleEnum.Manager)
                throw new ForbiddenException("Only manager can access this resource");

            if (!currentUser.NurseryId.HasValue)
                throw new ForbiddenException("Manager is not assigned to any nursery");

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

            var now = GetCurrentVietnamTime();
            nurseryOrder.Status = (int)OrderStatusEnum.Delivered;
            nurseryOrder.DeliveredAt = now;
            nurseryOrder.DeliveryNote = request.DeliveryNote;
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
                        parentOrder.Status = parentOrder.OrderType == (int)OrderTypeEnum.PlantInstance
                            ? (int)OrderStatusEnum.RemainingPaymentPending
                            : (int)OrderStatusEnum.Delivered;
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
            Status = order.Status,
            StatusName = order.Status.HasValue ? ((OrderStatusEnum)order.Status.Value).ToString() : null,
            ShipperNote = order.ShipperNote,
            DeliveryNote = order.DeliveryNote,
            Note = order.Note,
            Items = order.NurseryOrderDetails
                .Select(d => d.ToOrderItemResponse())
                .ToList()
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
