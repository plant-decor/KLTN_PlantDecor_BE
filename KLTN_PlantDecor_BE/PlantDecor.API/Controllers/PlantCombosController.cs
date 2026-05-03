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
    [Authorize]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCombos([FromQuery] Pagination pagination)
        {
            var combos = await _plantComboService.GetAllCombosAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantComboListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved combos successfully",
                Payload = combos
            });
        }

        [HttpGet("active")]
        [Authorize(Roles ="Manager, Admin")]
        public async Task<IActionResult> GetActiveCombos([FromQuery] Pagination pagination)
        {
            var combos = await _plantComboService.GetActiveCombosAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantComboListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved active combos successfully",
                Payload = combos
            });
        }

        [HttpGet("/api/PlantCombos/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetComboById(int id)
        {
            var combo = await _plantComboService.GetComboByIdAsync(id);

            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved combo successfully",
                Payload = combo
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCombo([FromBody] PlantComboRequestDto request)
        {
            var combo = await _plantComboService.CreateComboAsync(request);
            return CreatedAtAction(nameof(GetComboById), new { id = combo.Id }, new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Created combo successfully",
                Payload = combo
            });
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCombo(int id, [FromBody] PlantComboUpdateDto request)
        {
            var combo = await _plantComboService.UpdateComboAsync(id, request);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Updated combo successfully",
                Payload = combo
            });
        }

        [HttpPost("{id}/thumbnail")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

        /// <summary>
        /// Thay thế ảnh combo theo imageId
        /// </summary>
        [HttpPut("{id}/images/{imageId}")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReplaceComboImage(int id, int imageId, IFormFile file)
        {
            var combo = await _plantComboService.ReplaceImageAsync(id, imageId, file);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Replace combo image successfully",
                Payload = combo
            });
        }

        /// <summary>
        /// Đặt ảnh primary cho combo theo imageId
        /// </summary>
        [HttpPatch("{id}/images/{imageId}/set-primary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetPrimaryComboImage(int id, int imageId)
        {
            var combo = await _plantComboService.SetPrimaryPlantComboImageAsync(id, imageId);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Set primary combo image successfully",
                Payload = combo
            });
        }

        /// <summary>
        /// Xóa ảnh combo theo imageId
        /// </summary>
        [HttpDelete("{id}/images/{imageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteComboImage(int id, int imageId)
        {
            var combo = await _plantComboService.DeletePlantComboImageAsync(id, imageId);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Delete combo image successfully",
                Payload = combo
            });
        }

        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var isActive = await _plantComboService.ToggleActiveAsync(id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = isActive ? "Combo has been activated" : "Combo has been deactivated",
                Payload = isActive
            });
        }

        #endregion

        #region Manager - Nursery Combo Stock

        /// <summary>
        /// [Manager] Lấy danh sách PlantCombo đang active của hệ thống
        /// GET /api/manager/plant-combos/active
        /// </summary>
        [HttpGet("/api/manager/plant-combos/active")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetManagerActiveCombos([FromQuery] Pagination pagination)
        {
            var combos = await _plantComboService.GetActiveCombosAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantComboListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved active combos successfully",
                Payload = combos
            });
        }

        /// <summary>
        /// [Manager] Lấy danh sách PlantCombo phù hợp với vựa dựa trên cây đang có
        /// GET /api/manager/plant-combos/compatible
        /// </summary>
        [HttpGet("/api/manager/plant-combos/compatible")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetCompatibleCombos()
        {
            var managerId = GetCurrentUserId();
            var result = await _plantComboService.GetCompatibleCombosForNurseryAsync(managerId);
            return Ok(new ApiResponse<List<PlantComboResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved compatible combos for nursery successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Lấy danh sách plant combo tồn kho của vựa (lấy nursery từ token)
        /// GET /api/manager/plant-combos
        /// </summary>
        [HttpGet("/api/manager/plant-combos")]
        [Authorize(Roles = "Manager, Staff")]
        public async Task<IActionResult> GetNurseryComboStock([FromQuery] Pagination pagination)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantComboService.GetNurseryComboStockAsync(managerId, pagination);
            return Ok(new ApiResponse<PaginatedResult<NurseryComboStockResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved nursery combos successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Tạo số lượng combo cho vựa bằng cách trừ kho cây đại trà
        /// POST /api/manager/plant-combos/{comboId}/assemble
        /// </summary>
        [HttpPost("/api/manager/plant-combos/{comboId}/assemble")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> AssembleComboStock(int comboId, [FromBody] AssembleNurseryComboRequestDto request)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantComboService.AssembleComboStockAsync(managerId, comboId, request);
            return Ok(new ApiResponse<NurseryComboStockOperationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Assembled combo stock successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Phân rã số lượng combo của vựa để hoàn cây về kho cây đại trà
        /// POST /api/manager/plant-combos/{comboId}/decompose
        /// </summary>
        [HttpPost("/api/manager/plant-combos/{comboId}/decompose")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DecomposeComboStock(int comboId, [FromBody] DecomposeNurseryComboRequestDto request)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantComboService.DecomposeComboStockAsync(managerId, comboId, request);
            return Ok(new ApiResponse<NurseryComboStockOperationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Decomposed combo stock successfully",
                Payload = result
            });
        }

        #endregion

        #region Combo Items Management

        [HttpPost("{comboId}/items")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddComboItem(int comboId, [FromBody] PlantComboItemRequestDto request)
        {
            var combo = await _plantComboService.AddComboItemAsync(comboId, request);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Added plant to combo successfully",
                Payload = combo
            });
        }

        [HttpPut("items/{comboItemId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateComboItem(int comboItemId, [FromBody] PlantComboItemRequestDto request)
        {
            var combo = await _plantComboService.UpdateComboItemAsync(comboItemId, request);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Updated combo item successfully",
                Payload = combo
            });
        }

        [HttpDelete("{comboId}/items/{comboItemId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveComboItem(int comboId, int comboItemId)
        {
            var combo = await _plantComboService.RemoveComboItemAsync(comboId, comboItemId);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Removed plant from combo successfully",
                Payload = combo
            });
        }

        #endregion

        #region Tag Assignment

        [HttpPost("assign-tags")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignTags([FromBody] AssignComboTagsDto request)
        {
            var combo = await _plantComboService.AssignTagsToComboAsync(request);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Assigned tags to combo successfully",
                Payload = combo
            });
        }

        [HttpDelete("{comboId}/tags/{tagId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveTag(int comboId, int tagId)
        {
            var combo = await _plantComboService.RemoveTagFromComboAsync(comboId, tagId);
            return Ok(new ApiResponse<PlantComboResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Removed tag from combo successfully",
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
                Message = "Retrieved combos for shop successfully",
                Payload = combos
            });
        }

        [HttpPost("/api/shop/plant-combos/search")]
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
                Message = "Searched combos for shop successfully",
                Payload = combos
            });
        }

        #endregion

        #region Private Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Unable to identify user from token");
            return userId;
        }

        #endregion
    }
}
