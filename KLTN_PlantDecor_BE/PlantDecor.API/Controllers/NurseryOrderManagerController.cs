using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
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
        /// <param name="status">
        /// Lọc theo trạng thái:
        /// 0=Pending,
        /// 1=DepositPaid,
        /// 2=Paid,
        /// 3=Assigned,
        /// 4=Shipping,
        /// 5=Delivered,
        /// 6=RemainingPaymentPending,
        /// 7=Completed,
        /// 8=Cancelled,
        /// 9=Failed,
        /// 10=RefundRequested,
        /// 11=Refunded,
        /// 12=Rejected,
        /// 13=PendingConfirmation
        /// </param>
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
                Message = "Retrieved nursery orders successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy chi tiết đơn hàng của vườn theo nurseryOrderId (bao gồm sản phẩm, shipper, customer)
        /// </summary>
        [HttpGet("{nurseryOrderId:int}")]
        public async Task<IActionResult> GetNurseryOrderDetail([FromRoute] int nurseryOrderId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.GetNurseryOrderDetailForManagerAsync(currentUserId, nurseryOrderId);

            return Ok(new ApiResponse<NurseryOrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved nursery order details successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy danh sách shipper của vườn để gán đơn
        /// </summary>
        [HttpGet("shippers")]
        public async Task<IActionResult> GetNurseryShippers()
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.GetNurseryShippersForManagerAsync(currentUserId);

            return Ok(new ApiResponse<List<NurseryOrderShipperResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved nursery shippers successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Cập nhật shipper cho nursery order
        /// </summary>
        [HttpPut("{nurseryOrderId:int}/shipper")]
        public async Task<IActionResult> UpdateNurseryOrderShipper([FromRoute] int nurseryOrderId, [FromBody] UpdateNurseryOrderShipperRequestDto request)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.UpdateNurseryOrderShipperForManagerAsync(currentUserId, nurseryOrderId, request.ShipperId);

            return Ok(new ApiResponse<NurseryOrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Updated nursery order shipper successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy tổng doanh thu của vườn hiện tại theo khoảng thời gian
        /// </summary>
        [HttpGet("revenue/summary")]
        public async Task<IActionResult> GetMyNurseryRevenueSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.GetMyNurseryRevenueSummaryAsync(currentUserId, from, to);

            return Ok(new ApiResponse<RevenueSummaryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved nursery revenue summary successfully",
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
