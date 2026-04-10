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
        /// Lấy danh sách đơn hàng của vườn (có phân trang và lọc theo trạng thái)
        /// </summary>
        /// <param name="status">Lọc theo trạng thái: 0=Pending, 1=Paid, 2=DepositPaid, 3=Assigned, 4=Shipping, 5=Delivered, 6=Cancelled, 7=DeliveryFailed</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Số item mỗi trang (mặc định: 10, tối đa: 100)</param>
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
                Message = "Lấy danh sách đơn hàng của vườn thành công",
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
