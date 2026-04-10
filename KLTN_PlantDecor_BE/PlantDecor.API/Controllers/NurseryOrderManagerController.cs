using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/manager/nursery-orders")]
    [ApiController]
    [Authorize(Roles = "Manager")]
    public class NurseryOrderManagerController : ControllerBase
    {
        private readonly INurseryOrderService _nurseryOrderService;

        public NurseryOrderManagerController(INurseryOrderService nurseryOrderService)
        {
            _nurseryOrderService = nurseryOrderService;
        }

        /// <summary>
        /// L?y danh sách ??n hàng c?a v?a (có phân trang và l?c theo tr?ng thái)
        /// </summary>
        /// <param name="status">L?c theo tr?ng thái: 0=Pending, 1=Paid, 2=DepositPaid, 3=Assigned, 4=Shipping, 5=Delivered, 6=Cancelled</param>
        /// <param name="pageNumber">S? trang (m?c ??nh: 1)</param>
        /// <param name="pageSize">S? item m?i trang (m?c ??nh: 10, t?i ?a: 100)</param>
        [HttpGet]
        public async Task<IActionResult> GetNurseryOrders([FromQuery] int? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var currentUserId = GetCurrentUserId();
            var pagination = new Pagination(pageNumber, pageSize);
            var result = await _nurseryOrderService.GetNurseryOrdersAsync(currentUserId, status, pagination);

            return Ok(new ApiResponse<PaginatedResult<NurseryOrderResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "L?y danh sách ??n hàng c?a v?a thành công",
                Payload = result
            });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Unable to identify user from token");

            return userId;
        }
    }
}
