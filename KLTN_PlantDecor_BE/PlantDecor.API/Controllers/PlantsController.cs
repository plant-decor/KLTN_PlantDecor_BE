using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;

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
        private readonly IPlantInstanceService _plantInstanceService;

        public PlantsController(IPlantService plantService, IPlantInstanceService plantInstanceService)
        {
            _plantService = plantService;
            _plantInstanceService = plantInstanceService;
        }

        #region CRUD Operations

        /// <summary>
        /// [System] Tìm kiếm danh sách tất cả plants (phân trang)
        /// </summary>
        [HttpPost("/api/system/plants/search")]
        public async Task<IActionResult> SearchAllPlants([FromBody] PlantSearchRequestDto request)
        {
            var plants = await _plantService.SearchAllPlantsAsync(request);
            return Ok(new ApiResponse<PaginatedResult<PlantListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Search all plants successfully",
                Payload = plants
            });
        }

        /// <summary>
        /// [Shop] Tìm kiếm danh sách cây cho shop (gồm cây định danh và cây đại trà còn hàng)
        /// </summary>
        [HttpPost("/api/shop/plants/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPlantsForShop([FromBody] PlantSearchRequestDto request)
        {
            var plants = await _plantService.SearchPlantsForShopAsync(request);
            return Ok(new ApiResponse<PaginatedResult<PlantListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Search shop products successfully",
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
                    Message = $"Plant with ID {id} not found"
                });

            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant successfully",
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
                Message = "Create plant successfully",
                Payload = plant
            });
        }

        /// <summary>
        /// Cập nhật plant
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdatePlant(int id, [FromBody] PlantUpdateDto request)
        {
            var plant = await _plantService.UpdatePlantAsync(id, request);
            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Update plant successfully",
                Payload = plant
            });
        }

        /// <summary>
        /// Upload ảnh thumbnail cho plant
        /// </summary>
        [HttpPost("{id}/thumbnail")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPlantThumbnail(int id, IFormFile file)
        {
            if (file == null)
            {
                throw new BadRequestException("No file was uploaded");
            }

            var plant = await _plantService.UploadPlantThumbnailAsync(id, file);
            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Upload plant thumbnail successfully",
                Payload = plant
            });
        }

        /// <summary>
        /// Upload ảnh cho plant
        /// </summary>
        [HttpPost("{id}/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPlantImages(int id, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                throw new BadRequestException("No files were uploaded");
            }

            var plant = await _plantService.UploadPlantImagesAsync(id, files);
            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Upload plant images successfully",
                Payload = plant
            });
        }

        /// <summary>
        /// Đặt thumbnail cho plant theo imageId
        /// </summary>
        [HttpPatch("{id}/images/{imageId}/set-primary")]
        public async Task<IActionResult> SetPrimaryPlantImage(int id, int imageId)
        {
            var plant = await _plantService.SetPrimaryPlantImageAsync(id, imageId);
            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Set primary plant image successfully",
                Payload = plant
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
                Message = isActive ? "Plant has been activated" : "Plant has been deactivated",
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
                Message = "Assign categories to plant successfully",
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
                Message = "Assign tags to plant successfully",
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
                Message = "Remove category from plant successfully",
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
                Message = "Remove tag from plant successfully",
                Payload = plant
            });
        }

        #endregion

        #region Shop - Public Operations

        /// <summary>
        /// [Shop] Lấy chi tiết plant theo ID
        /// GET /api/shop/plants/{id}
        /// </summary>
        [HttpGet("/api/shop/plants/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlantDetailForShop(int id)
        {
            return await GetPlantById(id);
        }

        /// <summary>
        /// Lấy danh sách nursery đang có plant(plant của plantInstance) này (cho người dùng)
        /// GET /api/plants/{id}/nurseries
        /// </summary>
        [HttpGet("/api/plants/{id}/nurseries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNurseriesByPlantId(int id)
        {
            var result = await _plantInstanceService.GetNurseriesByPlantIdAsync(id);
            return Ok(new ApiResponse<List<PlantNurseryAvailabilityDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách vựa có cây thành công",
                Payload = result
            });
        }

        #endregion
    }
}
