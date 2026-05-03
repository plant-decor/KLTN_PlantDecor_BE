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
        /// Lay ra danh sach nursery order cua shipper hien tai (co phan trang va loc theo trang thai)
        /// </summary>
        /// <param name="status">Lọc theo trạng thái:  3=Assigned, 4=Shipping, 5=Delivered, 7=DeliveryFailed</param>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyNurseryOrders([FromQuery] int? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var currentUserId = GetCurrentUserId();
            var pagination = new Pagination(pageNumber, pageSize);
            var result = await _nurseryOrderService.GetMyNurseryOrdersAsync(currentUserId, status, pagination);

            return Ok(new ApiResponse<PaginatedResult<NurseryOrderResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved shipper nursery orders successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy chi tiết một nursery order của shipper hiện tại
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetNurseryOrderDetail(int id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.GetNurseryOrderDetailForShipperAsync(currentUserId, id);

            return Ok(new ApiResponse<NurseryOrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved shipper nursery order details successfully",
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
                Message = "Confirmed pickup successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Xác nhận đã giao -> Delivered
        /// </summary>
        [HttpPut("{id}/mark-delivered")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> MarkDelivered(int id, [FromForm] MarkDeliveredRequestDto request)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.MarkDeliveredAsync(currentUserId, id, request);

            return Ok(new ApiResponse<NurseryOrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Confirmed delivery successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Xác nhận giao hàng thất bại -> DeliveryFailed
        /// </summary>
        [HttpPut("{id}/mark-delivery-failed")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> MarkDeliveryFailed(int id, [FromForm] MarkDeliveryFailedRequestDto request)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.MarkDeliveryFailedAsync(currentUserId, id, request);

            return Ok(new ApiResponse<NurseryOrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Confirmed delivery failure successfully",
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
