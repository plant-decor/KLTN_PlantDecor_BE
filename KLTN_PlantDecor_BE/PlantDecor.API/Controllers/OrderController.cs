using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API về đơn hàng của user
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICareServicePackageService _careServicePackageService;

        public OrderController(IOrderService orderService, ICareServicePackageService careServicePackageService)
        {
            _orderService = orderService;
            _careServicePackageService = careServicePackageService;
        }

        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            var userId = GetUserId();
            var result = await _orderService.CreateOrderAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<OrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Order created successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy chi tiết một đơn hàng của user hiện tại
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var userId = GetUserId();
            var result = await _orderService.GetOrderByIdAsync(id, userId);
            return Ok(new ApiResponse<OrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get order successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy danh sách tất cả đơn hàng của user hiện tại
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyOrders([FromQuery] OrderStatusEnum? orderStatus = null)
        {
            var userId = GetUserId();
            var result = await _orderService.GetMyOrdersAsync(userId, orderStatus);
            return Ok(new ApiResponse<List<OrderResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get orders successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin/Consultant] Lấy danh sách đơn hàng theo email
        /// </summary>
        [HttpGet("by-email")]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> GetOrdersByEmail([FromQuery] string email)
        {
            var result = await _orderService.GetOrdersByEmailAsync(email);
            return Ok(new ApiResponse<List<OrderResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get orders by email successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Lấy danh sách Order của dịch vụ Design, CareService theo status có phân trang
        /// </summary>
        [HttpGet("service-orders")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetDesignOrders([FromQuery] Pagination pagination, [FromQuery] OrderStatusEnum? status = null)
        {
            var userId = GetUserId();
            var result = await _orderService.GetDesignOrdersForOperatorAsync(userId, pagination, status);
            return Ok(new ApiResponse<PaginatedResult<OrderResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design orders successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Hủy đơn hàng (chỉ khi Pending hoặc DepositPaid)
        /// </summary>
        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = GetUserId();
            var result = await _orderService.CancelOrderAsync(id, userId);
            return Ok(new ApiResponse<OrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Order cancelled successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Consultant] Tư vấn gói dịch vụ chăm sóc dựa trên cây trong đơn hàng
        /// </summary>
        [HttpGet("{id}/recommended-packages-by-plant")]
        [Authorize(Roles = "Consultant")]
        public async Task<IActionResult> GetRecommendedPackagesByPlant(int id)
        {
            var consultantId = GetUserId();
            var recommendations = await _careServicePackageService.RecommendByOrderAsync(consultantId, id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get recommended packages successfully",
                Payload = new { recommendations }
            });
        }

        /// <summary>
        /// Đánh dấu đơn hàng đã giao thành công (dành cho shipper/admin)
        /// </summary>
        [HttpPatch("{id}/delivered")]
        public async Task<IActionResult> MarkOrderAsDelivered(int id)
        {
            var result = await _orderService.MarkOrderAsDeliveredAsync(id);
            return Ok(new ApiResponse<OrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Order marked as delivered successfully",
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
