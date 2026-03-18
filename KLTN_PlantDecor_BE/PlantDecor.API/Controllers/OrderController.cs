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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            var userId = GetUserId();
            var result = await _orderService.CreateOrderAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<List<OrderResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Orders created successfully",
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
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();
            var result = await _orderService.GetMyOrdersAsync(userId);
            return Ok(new ApiResponse<List<OrderResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get orders successfully",
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

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Unable to identify user from token");
            return userId;
        }
    }
}
