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
    /// API quản lý Plant Combo (Admin)
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class PlantCombosController : ControllerBase
    {
        private readonly IPlantComboService _plantComboService;

        public PlantCombosController(IPlantComboService plantComboService)
        {
            _plantComboService = plantComboService;
        }

        #region CRUD Operations

        [HttpGet]
        public async Task<IActionResult> GetAllCombos([FromQuery] Pagination pagination)
        {
            var combos = await _plantComboService.GetAllCombosAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantComboListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách combos thành công",
                Payload = combos
            });
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveCombos([FromQuery] Pagination pagination)
        {
            var combos = await _plantComboService.GetActiveCombosAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantComboListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách combos active thành công",
                Payload = combos
            });
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetComboById(int id)
        {
            var combo = await _plantComboService.GetComboByIdAsync(id);
            if (combo == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Combo với ID {id} không tồn tại"
                });

            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy combo thành công",
                Payload = combo
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCombo([FromBody] PlantComboRequestDto request)
        {
            var combo = await _plantComboService.CreateComboAsync(request);
            return CreatedAtAction(nameof(GetComboById), new { id = combo.Id }, new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo combo thành công",
                Payload = combo
            });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateCombo(int id, [FromBody] PlantComboUpdateDto request)
        {
            var combo = await _plantComboService.UpdateComboAsync(id, request);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật combo thành công",
                Payload = combo
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCombo(int id)
        {
            await _plantComboService.DeleteComboAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa combo thành công"
            });
        }

        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var isActive = await _plantComboService.ToggleActiveAsync(id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = isActive ? "Combo đã được kích hoạt" : "Combo đã bị vô hiệu hóa",
                Payload = isActive
            });
        }

        #endregion

        #region Combo Items Management

        [HttpPost("{comboId}/items")]
        public async Task<IActionResult> AddComboItem(int comboId, [FromBody] PlantComboItemRequestDto request)
        {
            var combo = await _plantComboService.AddComboItemAsync(comboId, request);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Thêm plant vào combo thành công",
                Payload = combo
            });
        }

        [HttpPut("items/{comboItemId}")]
        public async Task<IActionResult> UpdateComboItem(int comboItemId, [FromBody] PlantComboItemRequestDto request)
        {
            var combo = await _plantComboService.UpdateComboItemAsync(comboItemId, request);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật combo item thành công",
                Payload = combo
            });
        }

        [HttpDelete("{comboId}/items/{comboItemId}")]
        public async Task<IActionResult> RemoveComboItem(int comboId, int comboItemId)
        {
            var combo = await _plantComboService.RemoveComboItemAsync(comboId, comboItemId);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Gỡ plant khỏi combo thành công",
                Payload = combo
            });
        }

        #endregion

        #region Tag Assignment

        [HttpPost("assign-tags")]
        public async Task<IActionResult> AssignTags([FromBody] AssignComboTagsDto request)
        {
            var combo = await _plantComboService.AssignTagsToComboAsync(request);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Gắn tags cho combo thành công",
                Payload = combo
            });
        }

        [HttpDelete("{comboId}/tags/{tagId}")]
        public async Task<IActionResult> RemoveTag(int comboId, int tagId)
        {
            var combo = await _plantComboService.RemoveTagFromComboAsync(comboId, tagId);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Gỡ tag khỏi combo thành công",
                Payload = combo
            });
        }

        #endregion

        #region Shop Display

        [HttpGet("shop")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCombosForShop([FromQuery] Pagination pagination)
        {
            var combos = await _plantComboService.GetCombosForShopAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantComboListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách combos cho shop thành công",
                Payload = combos
            });
        }

        #endregion
    }
}
