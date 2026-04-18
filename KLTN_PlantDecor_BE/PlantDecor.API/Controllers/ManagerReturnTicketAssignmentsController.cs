using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/manager/return-ticket-assignments")]
    [ApiController]
    [Authorize(Roles = "Manager")]
    public class ManagerReturnTicketAssignmentsController : ControllerBase
    {
        private readonly IReturnTicketManagerService _returnTicketManagerService;

        public ManagerReturnTicketAssignmentsController(IReturnTicketManagerService returnTicketManagerService)
        {
            _returnTicketManagerService = returnTicketManagerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAssignments([FromQuery] int? status)
        {
            var managerId = GetUserId();
            var result = await _returnTicketManagerService.GetMyAssignmentsAsync(managerId, status);

            return Ok(new ApiResponse<List<ManagerReturnTicketAssignmentResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get return ticket assignments successfully",
                Payload = result
            });
        }

        [HttpPatch("{assignmentId:int}/items/{itemId:int}/refund")]
        public async Task<IActionResult> RefundItem(int assignmentId, int itemId, [FromBody] RefundReturnTicketItemRequestDto request)
        {
            var managerId = GetUserId();
            var result = await _returnTicketManagerService.RefundItemAsync(managerId, assignmentId, itemId, request);

            return Ok(new ApiResponse<ReturnTicketItemResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Return ticket item refunded successfully",
                Payload = result
            });
        }

        [HttpGet("{assignmentId:int}")]
        public async Task<IActionResult> GetAssignmentDetail(int assignmentId)
        {
            var managerId = GetUserId();
            var result = await _returnTicketManagerService.GetAssignmentDetailAsync(managerId, assignmentId);

            return Ok(new ApiResponse<ManagerReturnTicketAssignmentResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get return ticket assignment detail successfully",
                Payload = result
            });
        }

        [HttpPatch("{assignmentId:int}/start-review")]
        public async Task<IActionResult> StartReview(int assignmentId)
        {
            var managerId = GetUserId();
            var result = await _returnTicketManagerService.StartReviewAsync(managerId, assignmentId);

            return Ok(new ApiResponse<ManagerReturnTicketAssignmentResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Return ticket assignment moved to InReview",
                Payload = result
            });
        }

        [HttpPatch("{assignmentId:int}/items/{itemId:int}/approve")]
        public async Task<IActionResult> ApproveItem(int assignmentId, int itemId, [FromBody] ApproveReturnTicketItemRequestDto request)
        {
            var managerId = GetUserId();
            var result = await _returnTicketManagerService.ApproveItemAsync(managerId, assignmentId, itemId, request);

            return Ok(new ApiResponse<ReturnTicketItemResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Return ticket item approved successfully",
                Payload = result
            });
        }

        [HttpPatch("{assignmentId:int}/items/{itemId:int}/reject")]
        public async Task<IActionResult> RejectItem(int assignmentId, int itemId, [FromBody] RejectReturnTicketItemRequestDto request)
        {
            var managerId = GetUserId();
            var result = await _returnTicketManagerService.RejectItemAsync(managerId, assignmentId, itemId, request);

            return Ok(new ApiResponse<ReturnTicketItemResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Return ticket item rejected successfully",
                Payload = result
            });
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Unable to identify user from token");

            return userId;
        }
    }
}
