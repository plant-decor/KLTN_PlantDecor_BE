using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý Inventory (Admin)
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class InventoriesController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoriesController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        #region CRUD Operations

        /// <summary>
        /// Lấy tất cả inventories (Admin)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllInventories([FromQuery] Pagination pagination)
        {
            var inventories = await _inventoryService.GetAllInventoriesAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<InventoryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get all inventories successfully",
                Payload = inventories
            });
        }

        /// <summary>
        /// Lấy inventories đang active
        /// </summary>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveInventories([FromQuery] Pagination pagination)
        {
            var inventories = await _inventoryService.GetActiveInventoriesAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<InventoryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get active inventories successfully",
                Payload = inventories
            });
        }

        /// <summary>
        /// Lấy inventory theo ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInventoryById(int id)
        {
            var inventory = await _inventoryService.GetInventoryByIdAsync(id);
            if (inventory == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Inventory with ID {id} not found"
                });

            return Ok(new ApiResponse<InventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get inventory successfully",
                Payload = inventory
            });
        }

        /// <summary>
        /// Tạo inventory mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateInventory([FromBody] InventoryRequestDto request)
        {
            var inventory = await _inventoryService.CreateInventoryAsync(request);
            return CreatedAtAction(nameof(GetInventoryById), new { id = inventory.Id }, new ApiResponse<InventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Create inventory successfully",
                Payload = inventory
            });
        }

        /// <summary>
        /// Cập nhật inventory
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] InventoryUpdateDto request)
        {
            var inventory = await _inventoryService.UpdateInventoryAsync(id, request);
            return Ok(new ApiResponse<InventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Update inventory successfully",
                Payload = inventory
            });
        }

        /// <summary>
        /// Xóa inventory
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventory(int id)
        {
            await _inventoryService.DeleteInventoryAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Delete inventory successfully"
            });
        }

        /// <summary>
        /// Bật/tắt trạng thái active của inventory
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var isActive = await _inventoryService.ToggleActiveAsync(id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = isActive ? "Inventory has been activated" : "Inventory has been deactivated",
                Payload = isActive
            });
        }

        #endregion

        #region Category & Tag Assignment

        /// <summary>
        /// Gắn categories cho inventory
        /// </summary>
        [HttpPost("assign-categories")]
        public async Task<IActionResult> AssignCategories([FromBody] AssignInventoryCategoriesDto request)
        {
            var inventory = await _inventoryService.AssignCategoriesToInventoryAsync(request);
            return Ok(new ApiResponse<InventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Assign categories to inventory successfully",
                Payload = inventory
            });
        }

        /// <summary>
        /// Gắn tags cho inventory
        /// </summary>
        [HttpPost("assign-tags")]
        public async Task<IActionResult> AssignTags([FromBody] AssignInventoryTagsDto request)
        {
            var inventory = await _inventoryService.AssignTagsToInventoryAsync(request);
            return Ok(new ApiResponse<InventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Assign tags to inventory successfully",
                Payload = inventory
            });
        }

        /// <summary>
        /// Gỡ category khỏi inventory
        /// </summary>
        [HttpDelete("{inventoryId}/categories/{categoryId}")]
        public async Task<IActionResult> RemoveCategory(int inventoryId, int categoryId)
        {
            var inventory = await _inventoryService.RemoveCategoryFromInventoryAsync(inventoryId, categoryId);
            return Ok(new ApiResponse<InventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Remove category from inventory successfully",
                Payload = inventory
            });
        }

        /// <summary>
        /// Gỡ tag khỏi inventory
        /// </summary>
        [HttpDelete("{inventoryId}/tags/{tagId}")]
        public async Task<IActionResult> RemoveTag(int inventoryId, int tagId)
        {
            var inventory = await _inventoryService.RemoveTagFromInventoryAsync(inventoryId, tagId);
            return Ok(new ApiResponse<InventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Remove tag from inventory successfully",
                Payload = inventory
            });
        }

        #endregion

        #region Stock Management

        /// <summary>
        /// Cập nhật số lượng tồn kho
        /// </summary>
        [HttpPatch("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromQuery] int quantity)
        {
            var inventory = await _inventoryService.UpdateStockAsync(id, quantity);
            return Ok(new ApiResponse<InventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Update stock quantity successfully",
                Payload = inventory
            });
        }

        #endregion
    }
}
