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
    /// API dashboard tồn kho cho vựa
    /// </summary>
    [Route("api/manager/nursery-inventory")]
    [ApiController]
    [Authorize(Roles = "Manager")]
    public class NurseryInventoryManagerController : ControllerBase
    {
        private readonly INurseryService _nurseryService;

        public NurseryInventoryManagerController(INurseryService nurseryService)
        {
            _nurseryService = nurseryService;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm sắp hết hàng của vựa
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetMyNurseryLowStockProducts([FromQuery] int threshold = 5)
        {
            var result = await _nurseryService.GetMyNurseryLowStockProductsAsync(GetCurrentUserId(), threshold);

            return Ok(new ApiResponse<List<NurseryLowStockProductAlertDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved nursery low stock products successfully",
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
