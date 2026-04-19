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
    /// API quản lý Vật tư trong Vựa (NurseryMaterial) cho Manager
    /// </summary>
    [Route("api/manager/nursery-materials")]
    [ApiController]
    [Authorize]
    public class NurseryMaterialsController : ControllerBase
    {
        private readonly INurseryMaterialService _nurseryMaterialService;
        private readonly IMaterialService _materialService;

        public NurseryMaterialsController(INurseryMaterialService nurseryMaterialService, IMaterialService materialService)
        {
            _nurseryMaterialService = nurseryMaterialService;
            _materialService = materialService;
        }

        #region Manager Operations

        /// <summary>
        /// Lấy danh sách vật tư trong vựa của Manager
        /// </summary>
        [HttpGet("my-materials")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetMyNurseryMaterials([FromQuery] Pagination pagination)
        {
            var managerId = GetCurrentUserId();
            var materials = await _nurseryMaterialService.GetMyNurseryMaterialsAsync(managerId, pagination);
            return Ok(new ApiResponse<PaginatedResult<NurseryMaterialListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách vật tư thành công",
                Payload = materials
            });
        }

        /// <summary>
        /// Lấy danh sách material hệ thống đang active để Manager nhập về vựa
        /// </summary>
        [HttpPost("/api/manager/materials/active/search")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> SearchActiveSystemMaterials([FromBody] PaginationSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var materials = await _materialService.GetActiveMaterialsAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<MaterialListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách vật tư active của hệ thống thành công",
                Payload = materials
            });
        }

        /// <summary>
        /// Nhập vật tư vào vựa của Manager
        /// </summary>
        [HttpPost("import")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ImportToMyNursery([FromBody] ImportMaterialRequestDto request)
        {
            var managerId = GetCurrentUserId();
            var material = await _nurseryMaterialService.ImportToMyNurseryAsync(managerId, request);
            return Ok(new ApiResponse<NurseryMaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Nhập vật tư thành công",
                Payload = material
            });
        }

        /// <summary>
        /// Cập nhật số lượng vật tư trong vựa
        /// </summary>
        [HttpPatch("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateNurseryMaterial(int id, [FromBody] NurseryMaterialUpdateDto request)
        {
            var material = await _nurseryMaterialService.UpdateNurseryMaterialAsync(id, request);
            return Ok(new ApiResponse<NurseryMaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật vật tư thành công",
                Payload = material
            });
        }

        /// <summary>
        /// Bật/tắt trạng thái active của vật tư trong vựa
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ToggleActiveNurseryMaterial(int id)
        {
            var material = await _nurseryMaterialService.ToggleActiveAsync(id);
            return Ok(new ApiResponse<NurseryMaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = material.IsActive ? "Đã bật vật tư" : "Đã tắt vật tư",
                Payload = material
            });
        }

        #endregion

        #region Admin Operations

        /// <summary>
        /// [Admin] Lấy tất cả vật tư trong các vựa
        /// </summary>
        [HttpGet("/api/admin/nursery-materials")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllNurseryMaterials([FromQuery] Pagination pagination)
        {
            var materials = await _nurseryMaterialService.GetAllNurseryMaterialsAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<NurseryMaterialListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách vật tư thành công",
                Payload = materials
            });
        }

        /// <summary>
        /// [Admin] Lấy vật tư theo ID
        /// </summary>
        [HttpGet("/api/nursery-materials/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetNurseryMaterialById(int id)
        {
            var material = await _nurseryMaterialService.GetNurseryMaterialByIdAsync(id);

            return Ok(new ApiResponse<NurseryMaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy thông tin vật tư thành công",
                Payload = material
            });
        }

        /// <summary>
        /// [Admin] Lấy vật tư theo Nursery ID
        /// </summary>
        [HttpGet("/api/admin/nursery-materials/by-nursery/{nurseryId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByNurseryId(int nurseryId, [FromQuery] Pagination pagination)
        {
            var materials = await _nurseryMaterialService.GetByNurseryIdAsync(nurseryId, pagination);
            return Ok(new ApiResponse<PaginatedResult<NurseryMaterialListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Lấy danh sách vật tư theo Nursery ID {nurseryId} thành công",
                Payload = materials
            });
        }

        /// <summary>
        /// [Admin] Lấy vật tư theo Material ID
        /// </summary>
        [HttpGet("/api/admin/nursery-materials/by-material/{materialId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByMaterialId(int materialId, [FromQuery] Pagination pagination)
        {
            var materials = await _nurseryMaterialService.GetByMaterialIdAsync(materialId, pagination);
            return Ok(new ApiResponse<PaginatedResult<NurseryMaterialListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Lấy danh sách vật tư theo Material ID {materialId} thành công",
                Payload = materials
            });
        }

        /// <summary>
        /// [Admin] Nhập vật tư vào vựa cụ thể
        /// </summary>
        [HttpPost("/api/admin/nursery-materials/nursery/{nurseryId}/import")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportMaterial(int nurseryId, [FromBody] ImportMaterialRequestDto request)
        {
            var material = await _nurseryMaterialService.ImportMaterialAsync(nurseryId, request);
            return Ok(new ApiResponse<NurseryMaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Nhập vật tư thành công",
                Payload = material
            });
        }

        /// <summary>
        /// [Admin] Tạo NurseryMaterial mới
        /// </summary>
        [HttpPost("/api/admin/nursery-materials")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNurseryMaterial([FromBody] NurseryMaterialRequestDto request)
        {
            var material = await _nurseryMaterialService.CreateNurseryMaterialAsync(request);
            return CreatedAtAction(nameof(GetNurseryMaterialById), new { id = material.Id }, new ApiResponse<NurseryMaterialResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo NurseryMaterial thành công",
                Payload = material
            });
        }

        #endregion

        #region Shop Operations

        /// <summary>
        /// Tìm kiếm vật tư trong các vựa (Shop)
        /// </summary>
        [HttpPost("/api/shop/nursery-materials/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchNurseryMaterialsForShop([FromBody] NurseryMaterialShopSearchRequestDto request)
        {
            var searchRequest = request ?? new NurseryMaterialShopSearchRequestDto();
            var pagination = searchRequest.Pagination ?? new Pagination();

            var result = await _nurseryMaterialService.SearchNurseryMaterialsForShopAsync(searchRequest, pagination);
            return Ok(new ApiResponse<PaginatedResult<NurseryMaterialListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tìm kiếm vật tư thành công",
                Payload = result
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
