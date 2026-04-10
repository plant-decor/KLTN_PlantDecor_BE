using Microsoft.EntityFrameworkCore;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
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

        public async Task<NurseryOrderResponseDto> StartShippingAsync(int currentUserId, int nurseryOrderId, StartShippingRequestDto request)
        {
            var currentUser = await GetValidatedShipperAsync(currentUserId);
            var nurseryOrder = await _unitOfWork.NurseryOrderRepository.GetByIdWithDetailsAsync(nurseryOrderId)
                ?? throw new NotFoundException($"NurseryOrder {nurseryOrderId} not found");

            ValidateOwnership(currentUser, nurseryOrder);

            if (nurseryOrder.Status != (int)NurseryOrderStatus.Assigned)
                throw new BadRequestException("??n không ? tr?ng thái có th? b?t ??u giao.");

            var now = GetCurrentVietnamTime();
            nurseryOrder.Status = (int)NurseryOrderStatus.Shipping;
            nurseryOrder.ShippingStartedAt = now;
            nurseryOrder.ShipperNote = request.ShipperNote;
            nurseryOrder.UpdatedAt = now;

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

            if (nurseryOrder.Status != (int)NurseryOrderStatus.Shipping)
                throw new BadRequestException("??n ch?a ? tr?ng thái ?ang giao.");

            var now = GetCurrentVietnamTime();
            nurseryOrder.Status = (int)NurseryOrderStatus.Delivered;
            nurseryOrder.DeliveredAt = now;
            nurseryOrder.DeliveryNote = request.DeliveryNote;
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
            NurseryId = order.NurseryId,
            NurseryName = order.Nursery?.Name,
            ShipperId = order.ShipperId,
            ShipperName = order.Shipper?.Username ?? order.Shipper?.Email,
            SubTotalAmount = order.SubTotalAmount,
            Status = order.Status,
            StatusName = order.Status.HasValue ? ((NurseryOrderStatus)order.Status.Value).ToString() : null,
            ShipperNote = order.ShipperNote,
            Items = order.NurseryOrderDetails.Select(d => new OrderItemResponseDto
            {
                Id = d.Id,
                ItemName = d.ItemName,
                Quantity = d.Quantity,
                Price = d.UnitPrice,
                Status = d.Status,
                StatusName = d.Status.HasValue ? ((NurseryOrderStatus)d.Status.Value).ToString() : null
            }).ToList()
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
    }
}
