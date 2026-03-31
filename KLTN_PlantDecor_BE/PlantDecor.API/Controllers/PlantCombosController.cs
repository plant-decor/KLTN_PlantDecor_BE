using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

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

        #region Shop

        /// <summary>
        /// [Shop] Lấy danh sách các vựa có bán plant combo cụ thể
        /// </summary>
        [HttpGet("/api/shop/plant-combos/{comboId}/nurseries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNurseriesByCombo(int comboId)
        {
            var nurseries = await _plantComboService.GetNurseriesByComboAsync(comboId);
            return Ok(new ApiResponse<List<NurseryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Get nurseries for plant combo {comboId} successfully",
                Payload = nurseries
            });
        }

        #endregion

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

        [HttpGet("/api/PlantCombos/{id}")]
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

        [HttpPost("{id}/thumbnail")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadComboThumbnail(int id, IFormFile file)
        {
            if (file == null)
            {
                throw new BadRequestException("No file was uploaded");
            }

            var combo = await _plantComboService.UploadPlantComboThumbnailAsync(id, file);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Upload combo thumbnail successfully",
                Payload = combo
            });
        }

        [HttpPost("{id}/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadComboImages(int id, List<IFormFile> files)
        {
            var combo = await _plantComboService.UploadPlantComboImagesAsync(id, files);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Upload combo images successfully",
                Payload = combo
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

        #region Manager - Nursery Combo Stock

        /// <summary>
        /// Tạo số lượng combo cho vựa bằng cách trừ kho cây đại trà
        /// POST /api/manager/nurseries/{nurseryId}/plant-combos/{comboId}/assemble
        /// </summary>
        [HttpPost("/api/manager/nurseries/{nurseryId}/plant-combos/{comboId}/assemble")]
        public async Task<IActionResult> AssembleComboStock(int nurseryId, int comboId, [FromBody] AssembleNurseryComboRequestDto request)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantComboService.AssembleComboStockAsync(nurseryId, managerId, comboId, request);
            return Ok(new ApiResponse<NurseryComboStockOperationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tạo tồn kho combo thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Phân rã số lượng combo của vựa để hoàn cây về kho cây đại trà
        /// POST /api/manager/nurseries/{nurseryId}/plant-combos/{comboId}/decompose
        /// </summary>
        [HttpPost("/api/manager/nurseries/{nurseryId}/plant-combos/{comboId}/decompose")]
        public async Task<IActionResult> DecomposeComboStock(int nurseryId, int comboId, [FromBody] DecomposeNurseryComboRequestDto request)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantComboService.DecomposeComboStockAsync(nurseryId, managerId, comboId, request);
            return Ok(new ApiResponse<NurseryComboStockOperationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Phân rã tồn kho combo thành công",
                Payload = result
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

        [HttpPost("shop/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchCombosForShop([FromBody] PlantComboShopSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var search = request ?? new PlantComboShopSearchRequestDto();
            var combos = await _plantComboService.GetSellingCombosAsync(pagination, search);
            return Ok(new ApiResponse<PaginatedResult<SellingPlantComboResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tìm kiếm combos cho shop thành công",
                Payload = combos
            });
        }

        #endregion

        #region Private Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return 1;
            }
            return userId;
        }

        #endregion
    }
}
