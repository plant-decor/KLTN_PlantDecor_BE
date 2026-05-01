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
    /// API dashboard sản phẩm cho vựa
    /// </summary>
    [Route("api/manager/nursery-products")]
    [ApiController]
    [Authorize(Roles = "Manager")]
    public class NurseryProductsManagerController : ControllerBase
    {
        private readonly INurseryOrderService _nurseryOrderService;

        public NurseryProductsManagerController(INurseryOrderService nurseryOrderService)
        {
            _nurseryOrderService = nurseryOrderService;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm bán chạy của vựa theo khoảng thời gian
        /// </summary>
        [HttpGet("top")]
        public async Task<IActionResult> GetMyNurseryTopProducts([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int limit = 10)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _nurseryOrderService.GetMyNurseryTopProductsAsync(currentUserId, from, to, limit);

            return Ok(new ApiResponse<List<TopProductResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved nursery top products successfully",
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
