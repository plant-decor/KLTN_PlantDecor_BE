using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/admin/revenue")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RevenueAdminController : ControllerBase
    {
        private readonly INurseryOrderService _nurseryOrderService;

        public RevenueAdminController(INurseryOrderService nurseryOrderService)
        {
            _nurseryOrderService = nurseryOrderService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSystemRevenueSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.GetSystemRevenueSummaryAsync(currentUserId, from, to);

            return Ok(new ApiResponse<RevenueSummaryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy doanh thu toàn hệ thống thành công",
                Payload = result
            });
        }

        [HttpGet("by-nursery")]
        public async Task<IActionResult> GetSystemRevenueByNursery([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.GetSystemRevenueByNurseryAsync(currentUserId, from, to);

            return Ok(new ApiResponse<List<NurseryRevenueItemResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy doanh thu theo từng vườn thành công",
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
