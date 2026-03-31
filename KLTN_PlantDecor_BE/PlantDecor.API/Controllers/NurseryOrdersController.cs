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
    [Route("api/shipper/nursery-orders")]
    [ApiController]
    [Authorize(Roles = "Shipper")]
    public class NurseryOrdersController : ControllerBase
    {
        private readonly INurseryOrderService _nurseryOrderService;

        public NurseryOrdersController(INurseryOrderService nurseryOrderService)
        {
            _nurseryOrderService = nurseryOrderService;
        }

        /// <summary>
        ///Lấy ra danh sách nursery order của shipper hiện tại
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyNurseryOrders()
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.GetMyNurseryOrdersAsync(currentUserId);

            return Ok(new ApiResponse<List<NurseryOrderResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách đơn giao hàng thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Xác nhận đã lấy hàng -> Shipping
        /// </summary>
        [HttpPut("{id}/start-shipping")]
        public async Task<IActionResult> StartShipping(int id, [FromBody] StartShippingRequestDto request)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.StartShippingAsync(currentUserId, id, request);

            return Ok(new ApiResponse<NurseryOrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xác nhận lấy hàng thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Xác nhận đã giao -> Delivered
        /// </summary>
        [HttpPut("{id}/mark-delivered")]
        public async Task<IActionResult> MarkDelivered(int id, [FromBody] MarkDeliveredRequestDto request)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.MarkDeliveredAsync(currentUserId, id, request);

            return Ok(new ApiResponse<NurseryOrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xác nhận giao hàng thành công",
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
