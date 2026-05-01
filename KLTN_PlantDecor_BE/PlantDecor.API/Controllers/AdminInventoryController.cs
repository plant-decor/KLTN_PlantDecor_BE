using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API dashboard tồn kho cho hệ thống
    /// </summary>
    [Route("api/admin/inventory")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminInventoryController : ControllerBase
    {
        private readonly INurseryService _nurseryService;

        public AdminInventoryController(INurseryService nurseryService)
        {
            _nurseryService = nurseryService;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm sắp hết hàng toàn hệ thống
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockProducts([FromQuery] int threshold = 5)
        {
            var result = await _nurseryService.GetSystemLowStockProductsAsync(threshold);

            return Ok(new ApiResponse<List<SystemLowStockProductAlertDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved system low stock products successfully",
                Payload = result
            });
        }
    }
}
