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
    /// API quản lý Category (Admin)
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Lấy tất cả categories
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories([FromQuery] Pagination pagination)
        {
            var categories = await _categoryService.GetAllCategoriesAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<CategoryResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get all categories successfully!",
                Payload = categories
            });
        }

        /// <summary>
        /// Lấy categories gốc (có cấu trúc cây)
        /// </summary>
        [HttpGet("admin/tree")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAdminCategoryTree()
        {
            var categories = await _categoryService.GetRootCategoriesAsync();
            return Ok(new ApiResponse<IEnumerable<CategoryResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get category tree Successfully!",
                Payload = categories
            });
        }

        /// <summary>
        /// Lấy categories gốc (có cấu trúc cây)
        /// </summary>
        [HttpGet("tree")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryTree()
        {
            var categories = await _categoryService.GetRootActiveCategoriesAsync();
            return Ok(new ApiResponse<IEnumerable<CategoryResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get Category Tree Successfully!",
                Payload = categories
            });
        }

        /// <summary>
        /// Lấy category theo ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Category with ID {id} not exist"
                });

            return Ok(new ApiResponse<CategoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get Category Successfully",
                Payload = category
            });
        }

        /// <summary>
        /// Tạo category mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryRequestDto request)
        {
            var category = await _categoryService.CreateCategoryAsync(request);
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, new ApiResponse<CategoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Create Category Successfully!",
                Payload = category
            });
        }

        /// <summary>
        /// Cập nhật category (partial update)
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto request)
        {
            var category = await _categoryService.UpdateCategoryAsync(id, request);
            return Ok(new ApiResponse<CategoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Update Category Successfully!",
                Payload = category
            });
        }

        /// <summary>
        /// Xóa category
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            await _categoryService.DeleteCategoryAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Delete Category successfully!"
            });
        }

        /// <summary>
        /// Bật/tắt trạng thái active của category
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var isActive = await _categoryService.ToggleActiveAsync(id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = isActive ? "Category has been activated" : "Category has been deactivated",
                Payload = isActive
            });
        }
    }
}
