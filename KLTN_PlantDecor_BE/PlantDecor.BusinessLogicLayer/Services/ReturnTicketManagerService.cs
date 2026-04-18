using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ReturnTicketManagerService : IReturnTicketManagerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReturnTicketManagerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ManagerReturnTicketAssignmentResponseDto>> GetMyAssignmentsAsync(int managerId, int? status)
        {
            var manager = await GetValidatedManagerAsync(managerId);
            var assignments = await _unitOfWork.ReturnTicketAssignmentRepository.GetByManagerIdWithDetailsAsync(managerId);

            var query = assignments.Where(a => a.NurseryId == manager.NurseryId);

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            return query.Select(MapAssignmentToResponse).ToList();
        }

        public async Task<ManagerReturnTicketAssignmentResponseDto> GetAssignmentDetailAsync(int managerId, int assignmentId)
        {
            var manager = await GetValidatedManagerAsync(managerId);
            var assignment = await GetOwnedAssignmentAsync(manager, assignmentId);

            return MapAssignmentToResponse(assignment);
        }

        public async Task<ManagerReturnTicketAssignmentResponseDto> StartReviewAsync(int managerId, int assignmentId)
        {
            var manager = await GetValidatedManagerAsync(managerId);
            var assignment = await GetOwnedAssignmentAsync(manager, assignmentId);

            if (assignment.Status != (int)ReturnTicketAssignmentStatusEnum.Pending)
                throw new BadRequestException("Assignment is not in Pending status");

            var now = DateTime.Now;
            assignment.Status = (int)ReturnTicketAssignmentStatusEnum.InReview;
            assignment.UpdatedAt = now;

            if (assignment.ReturnTicket.Status == (int)ReturnTicketStatusEnum.Pending)
            {
                assignment.ReturnTicket.Status = (int)ReturnTicketStatusEnum.InReview;
                assignment.ReturnTicket.UpdatedAt = now;
            }

            _unitOfWork.ReturnTicketAssignmentRepository.PrepareUpdate(assignment);
            await _unitOfWork.SaveAsync();

            return MapAssignmentToResponse(assignment);
        }

        public async Task<ReturnTicketItemResponseDto> ApproveItemAsync(int managerId, int assignmentId, int itemId, ApproveReturnTicketItemRequestDto request)
        {
            var manager = await GetValidatedManagerAsync(managerId);
            var assignment = await GetOwnedAssignmentAsync(manager, assignmentId);

            var item = GetAssignmentItem(assignment, itemId);

            if (item.Status != (int)ReturnTicketItemStatusEnum.Pending)
                throw new BadRequestException("Item is not in Pending status");

            if (request.ApprovedQuantity <= 0)
                throw new BadRequestException("ApprovedQuantity must be greater than 0");

            if (request.ApprovedQuantity > item.RequestedQuantity)
                throw new BadRequestException("ApprovedQuantity cannot exceed RequestedQuantity");

            var now = DateTime.Now;
            item.ApprovedQuantity = request.ApprovedQuantity;
            item.ManagerDecisionNote = request.Note;
            item.Status = (int)ReturnTicketItemStatusEnum.Approved;
            item.UpdatedAt = now;

            FinalizeAssignmentAndTicketStatus(assignment, now);

            _unitOfWork.ReturnTicketAssignmentRepository.PrepareUpdate(assignment);
            await _unitOfWork.SaveAsync();

            return MapItemToResponse(item);
        }

        public async Task<ReturnTicketItemResponseDto> RejectItemAsync(int managerId, int assignmentId, int itemId, RejectReturnTicketItemRequestDto request)
        {
            var manager = await GetValidatedManagerAsync(managerId);
            var assignment = await GetOwnedAssignmentAsync(manager, assignmentId);

            var item = GetAssignmentItem(assignment, itemId);

            if (item.Status != (int)ReturnTicketItemStatusEnum.Pending)
                throw new BadRequestException("Item is not in Pending status");

            var now = DateTime.Now;
            item.ApprovedQuantity = 0;
            item.ManagerDecisionNote = request.Note;
            item.Status = (int)ReturnTicketItemStatusEnum.Rejected;
            item.UpdatedAt = now;

            FinalizeAssignmentAndTicketStatus(assignment, now);

            _unitOfWork.ReturnTicketAssignmentRepository.PrepareUpdate(assignment);
            await _unitOfWork.SaveAsync();

            return MapItemToResponse(item);
        }

        private static ReturnTicketItem GetAssignmentItem(ReturnTicketAssignment assignment, int itemId)
        {
            var item = assignment.ReturnTicket.ReturnTicketItems.FirstOrDefault(i => i.Id == itemId)
                ?? throw new NotFoundException($"ReturnTicketItem {itemId} not found in ReturnTicket {assignment.ReturnTicketId}");

            if (item.NurseryOrderDetail?.NurseryOrder?.NurseryId != assignment.NurseryId)
                throw new ForbiddenException("This item does not belong to your nursery assignment");

            return item;
        }

        private static void FinalizeAssignmentAndTicketStatus(ReturnTicketAssignment assignment, DateTime now)
        {
            var assignmentItems = assignment.ReturnTicket.ReturnTicketItems
                .Where(i => i.NurseryOrderDetail?.NurseryOrder?.NurseryId == assignment.NurseryId)
                .ToList();

            if (!assignmentItems.Any())
                return;

            var assignmentDone = assignmentItems.All(i =>
                i.Status == (int)ReturnTicketItemStatusEnum.Approved ||
                i.Status == (int)ReturnTicketItemStatusEnum.Rejected ||
                i.Status == (int)ReturnTicketItemStatusEnum.Refunded);

            if (assignmentDone)
            {
                assignment.Status = (int)ReturnTicketAssignmentStatusEnum.Completed;
            }
            else if (assignment.Status == (int)ReturnTicketAssignmentStatusEnum.Pending)
            {
                assignment.Status = (int)ReturnTicketAssignmentStatusEnum.InReview;
            }

            assignment.UpdatedAt = now;

            var ticket = assignment.ReturnTicket;
            var allItems = ticket.ReturnTicketItems;

            var allResolved = allItems.All(i =>
                i.Status == (int)ReturnTicketItemStatusEnum.Approved ||
                i.Status == (int)ReturnTicketItemStatusEnum.Rejected ||
                i.Status == (int)ReturnTicketItemStatusEnum.Refunded);

            if (!allResolved)
            {
                ticket.Status = (int)ReturnTicketStatusEnum.InReview;
                ticket.UpdatedAt = now;
                return;
            }

            var allRejected = allItems.All(i => i.Status == (int)ReturnTicketItemStatusEnum.Rejected);
            if (allRejected)
            {
                ticket.Status = (int)ReturnTicketStatusEnum.Rejected;
                ticket.UpdatedAt = now;
                return;
            }

            var allApprovedOrRefunded = allItems.All(i =>
                i.Status == (int)ReturnTicketItemStatusEnum.Approved ||
                i.Status == (int)ReturnTicketItemStatusEnum.Refunded);

            ticket.Status = allApprovedOrRefunded
                ? (int)ReturnTicketStatusEnum.Approved
                : (int)ReturnTicketStatusEnum.PartiallyApproved;
            ticket.UpdatedAt = now;
        }

        private async Task<User> GetValidatedManagerAsync(int managerId)
        {
            var manager = await _unitOfWork.UserRepository.GetByIdAsync(managerId)
                ?? throw new UnauthorizedException("Unable to identify user from token");

            if (manager.RoleId != (int)RoleEnum.Manager)
                throw new ForbiddenException("Only manager can access this resource");

            if (!manager.NurseryId.HasValue)
                throw new ForbiddenException("Manager is not assigned to any nursery");

            return manager;
        }

        private async Task<ReturnTicketAssignment> GetOwnedAssignmentAsync(User manager, int assignmentId)
        {
            var assignment = await _unitOfWork.ReturnTicketAssignmentRepository.GetByIdWithDetailsAsync(assignmentId)
                ?? throw new NotFoundException($"ReturnTicketAssignment {assignmentId} not found");

            if (assignment.NurseryId != manager.NurseryId.Value)
                throw new ForbiddenException("You don't have permission to access this assignment");

            if (assignment.ManagerId.HasValue && assignment.ManagerId.Value != manager.Id)
                throw new ForbiddenException("This assignment is assigned to another manager");

            if (!assignment.ManagerId.HasValue)
            {
                assignment.ManagerId = manager.Id;
            }

            return assignment;
        }

        private static ManagerReturnTicketAssignmentResponseDto MapAssignmentToResponse(ReturnTicketAssignment assignment)
        {
            var ticket = assignment.ReturnTicket;
            var nurseryItems = ticket.ReturnTicketItems
                .Where(i => i.NurseryOrderDetail?.NurseryOrder?.NurseryId == assignment.NurseryId)
                .Select(MapItemToResponse)
                .ToList();

            return new ManagerReturnTicketAssignmentResponseDto
            {
                AssignmentId = assignment.Id,
                ReturnTicketId = assignment.ReturnTicketId,
                NurseryId = assignment.NurseryId,
                NurseryName = assignment.Nursery?.Name,
                ManagerId = assignment.ManagerId,
                ManagerName = assignment.Manager?.Username ?? assignment.Manager?.Email,
                AssignmentStatus = assignment.Status,
                AssignmentStatusName = ((ReturnTicketAssignmentStatusEnum)assignment.Status).ToString(),
                AssignedAt = assignment.AssignedAt,
                OrderId = ticket.OrderId,
                CustomerId = ticket.CustomerId,
                CustomerName = ticket.Customer?.Username ?? ticket.Customer?.Email,
                TicketReason = ticket.Reason,
                TicketStatus = ticket.Status,
                TicketStatusName = ((ReturnTicketStatusEnum)ticket.Status).ToString(),
                Items = nurseryItems
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
                ManagerDecisionNote = item.ManagerDecisionNote,
                Status = item.Status,
                StatusName = ((ReturnTicketItemStatusEnum)item.Status).ToString(),
                NurseryOrderId = item.NurseryOrderDetail?.NurseryOrderId,
                NurseryId = item.NurseryOrderDetail?.NurseryOrder?.NurseryId,
                ImageUrls = item.ReturnTicketItemImages.Select(i => i.ImageUrl).ToList()
            };
        }
    }
}
