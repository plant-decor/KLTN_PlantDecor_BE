using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ReturnTicketService : IReturnTicketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public ReturnTicketService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ReturnTicketResponseDto> CreateReturnTicketAsync(int customerId, CreateReturnTicketRequestDto request)
        {
            if (request.Items == null || !request.Items.Any())
                throw new BadRequestException("Items are required to create return ticket");

            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(request.OrderId)
                ?? throw new NotFoundException($"Order {request.OrderId} not found");

            if (order.UserId != customerId)
                throw new ForbiddenException("You don't have access to this order");

            if (order.Status != (int)OrderStatusEnum.PendingConfirmation)
                throw new BadRequestException("Return ticket is only allowed when order is PendingConfirmation");

            var allOrderDetails = order.NurseryOrders
                .SelectMany(no => no.NurseryOrderDetails, (no, detail) => new { NurseryOrder = no, Detail = detail })
                .ToDictionary(x => x.Detail.Id, x => x);

            var duplicatedDetailIds = request.Items
                .GroupBy(i => i.NurseryOrderDetailId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatedDetailIds.Any())
                throw new BadRequestException($"Duplicated NurseryOrderDetailId(s): {string.Join(", ", duplicatedDetailIds)}");

            var now = DateTime.Now;
            var returnTicket = new ReturnTicket
            {
                OrderId = order.Id,
                CustomerId = customerId,
                Reason = request.Reason,
                Status = (int)ReturnTicketStatusEnum.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };

            var nurseryIds = new HashSet<int>();

            foreach (var item in request.Items)
            {
                if (!allOrderDetails.TryGetValue(item.NurseryOrderDetailId, out var detailPair))
                    throw new BadRequestException($"NurseryOrderDetail {item.NurseryOrderDetailId} does not belong to order {order.Id}");

                var detail = detailPair.Detail;
                var nurseryOrder = detailPair.NurseryOrder;

                if (item.RequestedQuantity <= 0)
                    throw new BadRequestException($"RequestedQuantity of NurseryOrderDetail {item.NurseryOrderDetailId} must be greater than 0");

                var purchasedQuantity = detail.Quantity ?? 0;
                if (item.RequestedQuantity > purchasedQuantity)
                    throw new BadRequestException($"RequestedQuantity of NurseryOrderDetail {item.NurseryOrderDetailId} exceeds purchased quantity ({purchasedQuantity})");

                if (detail.Status == (int)OrderStatusEnum.RefundRequested || detail.Status == (int)OrderStatusEnum.Refunded)
                    throw new ConflictException($"NurseryOrderDetail {item.NurseryOrderDetailId} already has a refund flow");

                returnTicket.ReturnTicketItems.Add(new ReturnTicketItem
                {
                    NurseryOrderDetailId = detail.Id,
                    RequestedQuantity = item.RequestedQuantity,
                    Reason = item.Reason,
                    Status = (int)ReturnTicketItemStatusEnum.Pending,
                    CreatedAt = now,
                    UpdatedAt = now
                });

                detail.Status = (int)OrderStatusEnum.RefundRequested;
                nurseryIds.Add(nurseryOrder.NurseryId);
            }

            var managers = (await _unitOfWork.UserRepository.GetAllAsync())
                .Where(u => u.RoleId == (int)RoleEnum.Manager && u.NurseryId.HasValue && nurseryIds.Contains(u.NurseryId.Value))
                .ToList();

            foreach (var nurseryId in nurseryIds)
            {
                var manager = managers.FirstOrDefault(m => m.NurseryId == nurseryId);

                returnTicket.ReturnTicketAssignments.Add(new ReturnTicketAssignment
                {
                    NurseryId = nurseryId,
                    ManagerId = manager?.Id,
                    Manager = manager,
                    Status = (int)ReturnTicketAssignmentStatusEnum.Pending,
                    AssignedAt = now,
                    UpdatedAt = now
                });
            }

            _unitOfWork.ReturnTicketRepository.PrepareCreate(returnTicket);
            _unitOfWork.OrderRepository.PrepareUpdate(order);
            await _unitOfWork.SaveAsync();

            return MapToResponse(returnTicket);
        }

        public async Task<List<ReturnTicketResponseDto>> GetMyReturnTicketsAsync(int customerId)
        {
            var tickets = await _unitOfWork.ReturnTicketRepository.GetByCustomerIdWithDetailsAsync(customerId);
            return tickets.Select(MapToResponse).ToList();
        }

        public async Task<ReturnTicketItemResponseDto> UploadReturnTicketItemImagesAsync(int customerId, int returnTicketId, int returnTicketItemId, List<IFormFile> files)
        {
            if (files == null || !files.Any())
                throw new BadRequestException("At least one image is required");

            var ticket = await _unitOfWork.ReturnTicketRepository.GetByIdAsync(returnTicketId)
                ?? throw new NotFoundException($"ReturnTicket {returnTicketId} not found");

            if (ticket.CustomerId != customerId)
                throw new ForbiddenException("You don't have permission to modify this return ticket");

            var tickets = await _unitOfWork.ReturnTicketRepository.GetByCustomerIdWithDetailsAsync(customerId);
            var fullTicket = tickets.FirstOrDefault(t => t.Id == returnTicketId)
                ?? throw new NotFoundException($"ReturnTicket {returnTicketId} not found");

            var item = fullTicket.ReturnTicketItems.FirstOrDefault(i => i.Id == returnTicketItemId)
                ?? throw new NotFoundException($"ReturnTicketItem {returnTicketItemId} not found in return ticket {returnTicketId}");

            var uploadFolder = $"return-tickets/{returnTicketId}/items/{returnTicketItemId}";
            var uploadResults = await _cloudinaryService.UploadFilesAsync(files, uploadFolder);

            foreach (var upload in uploadResults)
            {
                item.ReturnTicketItemImages.Add(new ReturnTicketItemImage
                {
                    ImageUrl = upload.SecureUrl,
                    PublicId = upload.PublicId,
                    CreatedAt = DateTime.Now
                });
            }

            item.UpdatedAt = DateTime.Now;
            fullTicket.UpdatedAt = DateTime.Now;

            _unitOfWork.ReturnTicketRepository.PrepareUpdate(fullTicket);
            await _unitOfWork.SaveAsync();

            return MapItemToResponse(item);
        }

        private static ReturnTicketResponseDto MapToResponse(ReturnTicket ticket)
        {
            return new ReturnTicketResponseDto
            {
                Id = ticket.Id,
                OrderId = ticket.OrderId,
                CustomerId = ticket.CustomerId,
                Reason = ticket.Reason,
                Status = ticket.Status,
                StatusName = ((ReturnTicketStatusEnum)ticket.Status).ToString(),
                CreatedAt = ticket.CreatedAt,
                Items = ticket.ReturnTicketItems.Select(i => new ReturnTicketItemResponseDto
                {
                    Id = i.Id,
                    NurseryOrderDetailId = i.NurseryOrderDetailId,
                    ItemName = i.NurseryOrderDetail?.ItemName,
                    RequestedQuantity = i.RequestedQuantity,
                    ApprovedQuantity = i.ApprovedQuantity,
                    Reason = i.Reason,
                    Status = i.Status,
                    StatusName = ((ReturnTicketItemStatusEnum)i.Status).ToString(),
                    NurseryOrderId = i.NurseryOrderDetail?.NurseryOrderId,
                    NurseryId = i.NurseryOrderDetail?.NurseryOrder?.NurseryId,
                    ImageUrls = i.ReturnTicketItemImages.Select(img => img.ImageUrl).ToList()
                }).ToList(),
                Assignments = ticket.ReturnTicketAssignments.Select(a => new ReturnTicketAssignmentResponseDto
                {
                    Id = a.Id,
                    NurseryId = a.NurseryId,
                    ManagerId = a.ManagerId,
                    ManagerName = a.Manager?.Username ?? a.Manager?.Email,
                    Status = a.Status,
                    StatusName = ((ReturnTicketAssignmentStatusEnum)a.Status).ToString(),
                    AssignedAt = a.AssignedAt
                }).ToList()
            };
        }

        private static ReturnTicketItemResponseDto MapItemToResponse(ReturnTicketItem item)
        {
            return new ReturnTicketItemResponseDto
            {
                Id = item.Id,
                NurseryOrderDetailId = item.NurseryOrderDetailId,
                ItemName = item.NurseryOrderDetail?.ItemName,
                RequestedQuantity = item.RequestedQuantity,
                ApprovedQuantity = item.ApprovedQuantity,
                Reason = item.Reason,
                Status = item.Status,
                StatusName = ((ReturnTicketItemStatusEnum)item.Status).ToString(),
                NurseryOrderId = item.NurseryOrderDetail?.NurseryOrderId,
                NurseryId = item.NurseryOrderDetail?.NurseryOrder?.NurseryId,
                ImageUrls = item.ReturnTicketItemImages.Select(i => i.ImageUrl).ToList()
            };
        }
    }
}
