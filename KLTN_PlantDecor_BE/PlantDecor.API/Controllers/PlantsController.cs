using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý Plant (Admin)
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class PlantsController : ControllerBase
    {
        private readonly IPlantService _plantService;

        public PlantsController(IPlantService plantService)
        {
            _plantService = plantService;
        }

        #region CRUD Operations

        /// <summary>
        /// Lấy tất cả plants (Admin)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllPlants()
        {
            var plants = await _plantService.GetAllPlantsAsync();
            return Ok(new ApiResponse<IEnumerable<PlantListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách plants thành công",
                Payload = plants
            });
        }

        /// <summary>
        /// Lấy plants đang active
        /// </summary>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActivePlants()
        {
            var plants = await _plantService.GetActivePlantsAsync();
            return Ok(new ApiResponse<IEnumerable<PlantListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách plants active thành công",
                Payload = plants
            });
        }

        /// <summary>
        /// Lấy plant theo ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlantById(int id)
        {
            var plant = await _plantService.GetPlantByIdAsync(id);
            if (plant == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Plant với ID {id} không tồn tại"
                });

            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy plant thành công",
                Payload = plant
            });
        }

        /// <summary>
        /// Tạo plant mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePlant([FromBody] PlantRequestDto request)
        {
            var plant = await _plantService.CreatePlantAsync(request);
            return CreatedAtAction(nameof(GetPlantById), new { id = plant.Id }, new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo plant thành công",
                Payload = plant
            });
        }

        /// <summary>
        /// Cập nhật plant
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlant(int id, [FromBody] PlantUpdateDto request)
        {
            var plant = await _plantService.UpdatePlantAsync(id, request);
            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật plant thành công",
                Payload = plant
            });
        }

        /// <summary>
        /// Xóa plant
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlant(int id)
        {
            await _plantService.DeletePlantAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa plant thành công"
            });
        }

        /// <summary>
        /// Bật/tắt trạng thái active của plant
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var isActive = await _plantService.ToggleActiveAsync(id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = isActive ? "Plant đã được kích hoạt" : "Plant đã bị vô hiệu hóa",
                Payload = isActive
            });
        }

        #endregion

        #region Category & Tag Assignment

        /// <summary>
        /// Gắn categories cho plant
        /// </summary>
        [HttpPost("assign-categories")]
        public async Task<IActionResult> AssignCategories([FromBody] AssignCategoriesDto request)
        {
            var plant = await _plantService.AssignCategoriesToPlantAsync(request);
            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Gắn categories cho plant thành công",
                Payload = plant
            });
        }

        /// <summary>
        /// Gắn tags cho plant
        /// </summary>
        [HttpPost("assign-tags")]
        public async Task<IActionResult> AssignTags([FromBody] AssignTagsDto request)
        {
            var plant = await _plantService.AssignTagsToPlantAsync(request);
            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Gắn tags cho plant thành công",
                Payload = plant
            });
        }

        /// <summary>
        /// Gỡ category khỏi plant
        /// </summary>
        [HttpDelete("{plantId}/categories/{categoryId}")]
        public async Task<IActionResult> RemoveCategory(int plantId, int categoryId)
        {
            var plant = await _plantService.RemoveCategoryFromPlantAsync(plantId, categoryId);
            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Gỡ category khỏi plant thành công",
                Payload = plant
            });
        }

        /// <summary>
        /// Gỡ tag khỏi plant
        /// </summary>
        [HttpDelete("{plantId}/tags/{tagId}")]
        public async Task<IActionResult> RemoveTag(int plantId, int tagId)
        {
            var plant = await _plantService.RemoveTagFromPlantAsync(plantId, tagId);
            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Gỡ tag khỏi plant thành công",
                Payload = plant
            });
        }

        #endregion
    }
}
