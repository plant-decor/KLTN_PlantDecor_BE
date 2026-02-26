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
    /// API quản lý Plant Inventory - Tồn kho cây tại vườn ươm (Admin)
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class PlantInventoriesController : ControllerBase
    {
        private readonly IPlantInventoryService _plantInventoryService;

        public PlantInventoriesController(IPlantInventoryService plantInventoryService)
        {
            _plantInventoryService = plantInventoryService;
        }

        #region CRUD Operations

        [HttpGet]
        public async Task<IActionResult> GetAllPlantInventories([FromQuery] Pagination pagination)
        {
            var inventories = await _plantInventoryService.GetAllPlantInventoriesAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantInventoryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách plant inventories thành công",
                Payload = inventories
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlantInventoryById(int id)
        {
            var inventory = await _plantInventoryService.GetPlantInventoryByIdAsync(id);
            if (inventory == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"PlantInventory với ID {id} không tồn tại"
                });

            return Ok(new ApiResponse<PlantInventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy plant inventory thành công",
                Payload = inventory
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlantInventory([FromBody] PlantInventoryRequestDto request)
        {
            var inventory = await _plantInventoryService.CreatePlantInventoryAsync(request);
            return CreatedAtAction(nameof(GetPlantInventoryById), new { id = inventory.Id }, new ApiResponse<PlantInventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo plant inventory thành công",
                Payload = inventory
            });
        }

        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdatePlantInventory(int id, [FromBody] PlantInventoryUpdateDto request)
        //{
        //    var inventory = await _plantInventoryService.UpdatePlantInventoryAsync(id, request);
        //    return Ok(new ApiResponse<PlantInventoryResponseDto>
        //    {
        //        Success = true,
        //        StatusCode = StatusCodes.Status200OK,
        //        Message = "Cập nhật plant inventory thành công",
        //        Payload = inventory
        //    });
        //}

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlantInventory(int id)
        {
            await _plantInventoryService.DeletePlantInventoryAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa plant inventory thành công"
            });
        }

        #endregion

        #region Query Operations

        [HttpGet("by-plant/{plantId}")]
        public async Task<IActionResult> GetByPlantId(int plantId, [FromQuery] Pagination pagination)
        {
            var inventories = await _plantInventoryService.GetByPlantIdAsync(plantId, pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantInventoryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Lấy danh sách plant inventories theo Plant ID {plantId} thành công",
                Payload = inventories
            });
        }

        [HttpGet("by-nursery/{nurseryId}")]
        public async Task<IActionResult> GetByNurseryId(int nurseryId, [FromQuery] Pagination pagination)
        {
            var inventories = await _plantInventoryService.GetByNurseryIdAsync(nurseryId, pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantInventoryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Lấy danh sách plant inventories theo Nursery ID {nurseryId} thành công",
                Payload = inventories
            });
        }

        #endregion

        #region Stock Management

        [HttpPatch("nursery/{nurseryId}/plants/{plantId}/quantity")]
        public async Task<IActionResult> UpdateQuantity(int nurseryId, int plantId, [FromBody] UpdateQuantityDto request)
        {
            var inventory = await _plantInventoryService.UpdateQuantityAsync(nurseryId, plantId, request.Quantity);
            return Ok(new ApiResponse<PlantInventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật số lượng tồn kho thành công",
                Payload = inventory
            });
        }

        #endregion
    }
}
