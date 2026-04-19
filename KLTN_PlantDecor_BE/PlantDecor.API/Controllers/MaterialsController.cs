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
    /// API quản lý Materials (Admin)
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize]
    public class MaterialsController : ControllerBase
    {
        private readonly IMaterialService _materialService;

        public MaterialsController(IMaterialService materialService)
        {
            _materialService = materialService;
        }

        #region CRUD Operations

        /// <summary>
        /// [System] Tìm kiếm danh sách tất cả materials (phân trang)
        /// </summary>
        [HttpPost("/api/system/materials/search")]
        [Authorize(Roles = "Manager, Admin")]
        public async Task<IActionResult> SearchAllMaterials([FromBody] PaginationSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var materials = request?.IsActive == true
                ? await _materialService.GetActiveMaterialsAsync(pagination)
                : await _materialService.GetAllMaterialsAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<MaterialListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Search all materials successfully",
                Payload = materials
            });
        }

        /// <summary>
        /// [Shop] Tìm kiếm danh sách vật tư cho shop
        /// </summary>
        [HttpPost("/api/shop/materials/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchMaterialsForShop([FromBody] PaginationSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var materials = await _materialService.GetMaterialsForShopAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<MaterialListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Search shop materials successfully",
                Payload = materials
            });
        }

        /// <summary>
        /// [Shop] Lấy danh sách các vựa có bán material cụ thể
        /// </summary>
        [HttpGet("/api/shop/materials/{materialId}/nurseries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNurseriesByMaterial(int materialId)
        {
            var nurseries = await _materialService.GetNurseriesByMaterialAsync(materialId);
            return Ok(new ApiResponse<List<NurseryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Get nurseries for material {materialId} successfully",
                Payload = nurseries
            });
        }

        /// <summary>
        /// Lấy material theo ID
        /// </summary>
        [HttpGet("/api/material/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMaterialById(int id)
        {
            var material = await _materialService.GetMaterialByIdAsync(id);

            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get material successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Tạo material mới
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMaterial([FromBody] MaterialRequestDto request)
        {
            var material = await _materialService.CreateMaterialAsync(request);
            return CreatedAtAction(nameof(GetMaterialById), new { id = material.Id }, new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Create material successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Cập nhật material
        /// </summary>
        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMaterial(int id, [FromBody] MaterialUpdateDto request)
        {
            var material = await _materialService.UpdateMaterialAsync(id, request);
            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Update material successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Upload ảnh thumbnail cho material
        /// </summary>
        [HttpPost("{id}/thumbnail")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadMaterialThumbnail(int id, IFormFile file)
        {
            if (file == null)
            {
                throw new BadRequestException("No file was uploaded");
            }

            var material = await _materialService.UploadMaterialThumbnailAsync(id, file);
            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Upload material thumbnail successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Upload ảnh cho material
        /// </summary>
        [HttpPost("{id}/images")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadMaterialImages(int id, List<IFormFile> files)
        {
            var material = await _materialService.UploadMaterialImagesAsync(id, files);
            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Upload material images successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Thay thế ảnh material theo imageId
        /// </summary>
        [HttpPut("{id}/images/{imageId}")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReplaceMaterialImage(int id, int imageId, IFormFile file)
        {
            var material = await _materialService.ReplaceImageAsync(id, imageId, file);
            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Replace material image successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Đặt ảnh primary cho material theo imageId
        /// </summary>
        [HttpPatch("{id}/images/{imageId}/set-primary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetPrimaryMaterialImage(int id, int imageId)
        {
            var material = await _materialService.SetPrimaryMaterialImageAsync(id, imageId);
            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Set primary material image successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Xóa ảnh material theo imageId
        /// </summary>
        [HttpDelete("{id}/images/{imageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMaterialImage(int id, int imageId)
        {
            var material = await _materialService.DeleteMaterialImageAsync(id, imageId);
            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Delete material image successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Bật/tắt trạng thái active của material
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var isActive = await _materialService.ToggleActiveAsync(id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = isActive ? "Material has been activated" : "Material has been deactivated",
                Payload = isActive
            });
        }

        #endregion

        #region Category & Tag Assignment

        /// <summary>
        /// Gắn categories cho material
        /// </summary>
        [HttpPost("assign-categories")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignCategories([FromBody] AssignMaterialCategoriesDto request)
        {
            var material = await _materialService.AssignCategoriesToMaterialAsync(request);
            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Assign categories to material successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Gắn tags cho material
        /// </summary>
        [HttpPost("assign-tags")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignTags([FromBody] AssignMaterialTagsDto request)
        {
            var material = await _materialService.AssignTagsToMaterialAsync(request);
            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Assign tags to material successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Gỡ category khỏi material
        /// </summary>
        [HttpDelete("{materialId}/categories/{categoryId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveCategory(int materialId, int categoryId)
        {
            var material = await _materialService.RemoveCategoryFromMaterialAsync(materialId, categoryId);
            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Remove category from material successfully",
                Payload = material
            });
        }

        /// <summary>
        /// Gỡ tag khỏi material
        /// </summary>
        [HttpDelete("{materialId}/tags/{tagId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveTag(int materialId, int tagId)
        {
            var material = await _materialService.RemoveTagFromMaterialAsync(materialId, tagId);
            return Ok(new ApiResponse<MaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Remove tag from material successfully",
                Payload = material
            });
        }

        #endregion
    }
}
