using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IReturnTicketManagerService
    {
        Task<List<ManagerReturnTicketAssignmentResponseDto>> GetMyAssignmentsAsync(int managerId, int? status);
        Task<ManagerReturnTicketAssignmentResponseDto> GetAssignmentDetailAsync(int managerId, int assignmentId);
        Task<ManagerReturnTicketAssignmentResponseDto> StartReviewAsync(int managerId, int assignmentId);
        Task<ReturnTicketItemResponseDto> ApproveItemAsync(int managerId, int assignmentId, int itemId, ApproveReturnTicketItemRequestDto request);
        Task<ReturnTicketItemResponseDto> RejectItemAsync(int managerId, int assignmentId, int itemId, RejectReturnTicketItemRequestDto request);
        Task<ReturnTicketItemResponseDto> RefundItemAsync(int managerId, int assignmentId, int itemId, RefundReturnTicketItemRequestDto request);
    }
}
