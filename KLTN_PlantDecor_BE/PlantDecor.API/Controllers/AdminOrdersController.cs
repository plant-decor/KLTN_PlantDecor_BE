using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API dashboard đơn hàng cho hệ thống
    /// </summary>
    [Route("api/admin/orders")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly INurseryOrderService _nurseryOrderService;

        public AdminOrdersController(INurseryOrderService nurseryOrderService)
        {
            _nurseryOrderService = nurseryOrderService;
        }

        /// <summary>
        /// Lấy số lượng đơn theo trạng thái trong hệ thống theo khoảng thời gian
        /// </summary>
        [HttpGet("status-summary")]
        public async Task<IActionResult> GetOrderStatusSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.GetSystemOrderStatusSummaryAsync(currentUserId, from, to);

            return Ok(new ApiResponse<OrderStatusSummaryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved system order status summary successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy tổng số đơn giao thất bại trong hệ thống theo khoảng thời gian
        /// </summary>
        [HttpGet("failed")]
        public async Task<IActionResult> GetFailedOrdersSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.GetSystemFailedOrdersSummaryAsync(currentUserId, from, to);

            return Ok(new ApiResponse<FailedOrderSummaryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved system failed orders summary successfully",
                Payload = result
            });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Unable to identify user from token");

            return userId;
        }
    }
}
