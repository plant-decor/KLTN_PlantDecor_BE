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
    /// API quản lý cây đại trà (CommonPlant) cho Manager
    /// </summary>
    [Route("api/manager/nurseries/{nurseryId}/common-plants")]
    [ApiController]
    [Authorize(Roles = "Manager")]
    public class CommonPlantsController : ControllerBase
    {
        private readonly ICommonPlantService _commonPlantService;

        public CommonPlantsController(ICommonPlantService commonPlantService)
        {
            _commonPlantService = commonPlantService;
        }

        /// <summary>
        /// Nhập cây đại trà mới cho vựa
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCommonPlant(int nurseryId, [FromBody] CommonPlantRequestDto request)
        {
            var managerId = GetCurrentUserId();
            var result = await _commonPlantService.CreateForNurseryAsync(nurseryId, managerId, request);
            return CreatedAtAction(nameof(GetCommonPlants), new { nurseryId }, new ApiResponse<CommonPlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Nhập cây đại trà thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy danh sách cây đại trà của vựa (Manager)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCommonPlants(int nurseryId, [FromQuery] Pagination pagination)
        {
            var managerId = GetCurrentUserId();
            var result = await _commonPlantService.GetByNurseryForManagerAsync(nurseryId, managerId, pagination);
            return Ok(new ApiResponse<PaginatedResult<CommonPlantListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách cây đại trà thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy danh sách plant đang active mà vựa chưa có để nhập kho
        /// </summary>
        [HttpGet("available-import-plants")]
        public async Task<IActionResult> GetAvailableImportPlants(int nurseryId, [FromQuery] Pagination pagination)
        {
            var managerId = GetCurrentUserId();
            var result = await _commonPlantService.GetPlantsNotInNurseryForManagerAsync(nurseryId, managerId, pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách plant chưa có trong vựa thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Cập nhật thông tin cây đại trà (số lượng, trạng thái)
        /// </summary>
        [HttpPatch("{commonPlantId}")]
        public async Task<IActionResult> UpdateCommonPlant(int nurseryId, int commonPlantId, [FromBody] CommonPlantUpdateDto request)
        {
            var managerId = GetCurrentUserId();
            var result = await _commonPlantService.UpdateForManagerAsync(nurseryId, commonPlantId, managerId, request);
            return Ok(new ApiResponse<CommonPlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật cây đại trà thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Bật/tắt trạng thái active của cây đại trà
        /// </summary>
        [HttpPatch("{commonPlantId}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int nurseryId, int commonPlantId)
        {
            var managerId = GetCurrentUserId();
            var result = await _commonPlantService.ToggleActiveAsync(nurseryId, commonPlantId, managerId);
            return Ok(new ApiResponse<CommonPlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = result.IsActive ? "Đã bật cây đại trà" : "Đã tắt cây đại trà",
                Payload = result
            });
        }

        /// <summary>
        /// [Shop] Tìm kiếm danh sách cây đại trà đang bán
        /// </summary>
        [HttpPost("/api/shop/common-plants/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchCommonPlantsForShop([FromBody] CommonPlantShopSearchRequestDto request)
        {
            var searchRequest = request ?? new CommonPlantShopSearchRequestDto();
            var pagination = searchRequest.Pagination ?? new Pagination();
            var result = await _commonPlantService.SearchCommonPlantsForShopAsync(searchRequest, pagination);

            return Ok(new ApiResponse<PaginatedResult<CommonPlantListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Search shop common plants successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Shop] Lấy danh sách vựa đang có cây đại trà theo PlantId
        /// </summary>
        [HttpGet("/api/shop/plants/{plantId}/common-nurseries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNurseriesWithCommonPlant(int plantId)
        {
            var result = await _commonPlantService.GetNurseriesWithCommonPlantAsync(plantId);

            return Ok(new ApiResponse<List<PlantNurseryAvailabilityDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách vựa có cây đại trà thành công",
                Payload = result
            });
        }

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
