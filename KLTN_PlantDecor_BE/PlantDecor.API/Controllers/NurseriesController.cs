using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý Vựa (Nursery) cho Manager
    /// </summary>
    [Route("api/manager/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Manager")]
    public class NurseriesController : ControllerBase
    {
        private readonly INurseryService _nurseryService;
        private readonly IPlantInstanceService _plantInstanceService;
        private readonly ICommonPlantService _commonPlantService;

        public NurseriesController(INurseryService nurseryService, IPlantInstanceService plantInstanceService, ICommonPlantService commonPlantService)
        {
            _nurseryService = nurseryService;
            _plantInstanceService = plantInstanceService;
            _commonPlantService = commonPlantService;
        }

        #region Manager Operations

        /// <summary>
        /// Lấy thông tin vựa của Manager đang đăng nhập
        /// </summary>
        [HttpGet("my-nursery")]
        public async Task<IActionResult> GetMyNursery()
        {
            var managerId = GetCurrentUserId();
            var nursery = await _nurseryService.GetMyNurseryAsync(managerId);
            
            if (nursery == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Bạn chưa có vựa nào"
                });

            return Ok(new ApiResponse<NurseryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy thông tin vựa thành công",
                Payload = nursery
            });
        }

        /// <summary>
        /// Tạo vựa mới cho Manager
        /// </summary>
        [HttpPost("/api/admin/nurseries")]
        public async Task<IActionResult> CreateNursery([FromBody] NurseryRequestDto request)
        {
            var nursery = await _nurseryService.CreateNurseryAsync(request);
            return CreatedAtAction(nameof(GetMyNursery), new ApiResponse<NurseryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo vựa thành công",
                Payload = nursery
            });
        }

        /// <summary>
        /// Cập nhật thông tin vựa của Manager
        /// </summary>
        [HttpPatch("/api/admin/my-nursery")]
        public async Task<IActionResult> UpdateMyNursery([FromBody] NurseryUpdateDto request)
        {
            var managerId = GetCurrentUserId();
            var nursery = await _nurseryService.UpdateMyNurseryAsync(managerId, request);
            return Ok(new ApiResponse<NurseryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật vựa thành công",
                Payload = nursery
            });
        }

        /// <summary>
        /// Lấy danh sách vật tư sắp hết hạn của vựa hiện tại
        /// </summary>
        [HttpGet("my-nursery/materials/expiring-soon")]
        public async Task<IActionResult> GetMyNurseryExpiringMaterials([FromQuery] int daysAhead = 30)
        {
            var managerId = GetCurrentUserId();
            var result = await _nurseryService.GetMyNurseryExpiringMaterialsAsync(managerId, Math.Max(daysAhead, 1));
            return Ok(new ApiResponse<List<NurseryMaterialExpiryAlertDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách vật tư sắp hết hạn thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy danh sách sản phẩm sắp hết hàng của vựa hiện tại
        /// </summary>
        [HttpGet("my-nursery/products/low-stock")]
        public async Task<IActionResult> GetMyNurseryLowStockProducts([FromQuery] int threshold = 5)
        {
            var managerId = GetCurrentUserId();
            var result = await _nurseryService.GetMyNurseryLowStockProductsAsync(managerId, Math.Max(threshold, 0));
            return Ok(new ApiResponse<List<NurseryLowStockProductAlertDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách sản phẩm sắp hết hàng thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy summary chung của vựa: cây đại trà, cây định danh và vật tư
        /// </summary>
        [HttpGet("my-nursery/material-summary")]
        public async Task<IActionResult> GetMyNurseryMaterialSummary([FromQuery] int lowStockThreshold = 5, [FromQuery] int expiringInDays = 30)
        {
            var managerId = GetCurrentUserId();
            var result = await _nurseryService.GetMyNurseryMaterialSummaryAsync(
                managerId,
                Math.Max(lowStockThreshold, 0),
                Math.Max(expiringInDays, 1));

            return Ok(new ApiResponse<NurseryMaterialSummaryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy summary kho vựa thành công",
                Payload = result
            });
        }

        #endregion

        #region Admin Operations

        /// <summary>
        /// [System] Tìm kiếm tất cả vựa (phân trang)
        /// </summary>
        [HttpPost("/api/system/nurseries/search")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchAllNurseries([FromBody] PaginationSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var nurseries = await _nurseryService.GetAllNurseriesAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<NurseryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tìm kiếm danh sách vựa thành công",
                Payload = nurseries
            });
        }

        /// <summary>
        /// [Admin] Lấy vựa theo ID
        /// </summary>
        [HttpGet("/api/nurseries/{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetNurseryById(int id)
        {
            var nursery = await _nurseryService.GetNurseryByIdAsync(id);
            if (nursery == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Vựa với ID {id} không tồn tại"
                });

            return Ok(new ApiResponse<NurseryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy thông tin vựa thành công",
                Payload = nursery
            });
        }

        /// <summary>
        /// [Admin] Cập nhật vựa theo ID
        /// </summary>
        [HttpPatch("/api/admin/nurseries/{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateNursery(int id, [FromBody] NurseryUpdateDto request)
        {
            var nursery = await _nurseryService.UpdateNurseryAsync(id, request);
            return Ok(new ApiResponse<NurseryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật vựa thành công",
                Payload = nursery
            });
        }

        /// <summary>
        /// Bật/tắt trạng thái active của nursery
        /// </summary>
        [HttpPatch("/api/admin/nurseries/{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var isActive = await _nurseryService.ToggleActiveAsync(id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = isActive ? "Nursery has been activated" : "Nursery has been deactivated",
                Payload = isActive
            });
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// [Shop] Tìm kiếm danh sách vựa đang hoạt động
        /// </summary>
        [HttpPost("/api/shop/nurseries/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchActiveNurseriesForShop([FromBody] PaginationSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var nurseries = await _nurseryService.GetActiveNurseriesAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<NurseryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tìm kiếm danh sách vựa cho shop thành công",
                Payload = nurseries
            });
        }

        /// <summary>
        /// [Shop] Tìm kiếm danh sách PlantInstance available tại một vựa
        /// </summary>
        [HttpPost("/api/shop/nurseries/{nurseryId}/plant-instances/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchAvailablePlantInstancesByNursery(int nurseryId, [FromBody] ShopPlantInstanceSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var result = await _plantInstanceService.GetAvailableByNurseryIdAsync(nurseryId, pagination, request?.PlantId);
            return Ok(new ApiResponse<PaginatedResult<PlantInstanceListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tìm kiếm danh sách cây tại vựa thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy chi tiết một PlantInstance (Shop)
        /// </summary>
        [HttpGet("/api/shop/plant-instances/{instanceId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlantInstanceDetail(int instanceId)
        {
            var result = await _plantInstanceService.GetInstanceDetailAsync(instanceId);
            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy chi tiết cây thành công",
                Payload = result
            });
        }

        /// <summary>
        /// [Shop] Lấy chi tiết một CommonPlant
        /// </summary>
        [HttpGet("/api/shop/common-plants/{commonPlantId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCommonPlantDetail(int commonPlantId)
        {
            var result = await _commonPlantService.GetCommonPlantByIdAsync(commonPlantId);
            if (result == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Không tìm thấy cây đại trà với ID {commonPlantId}"
                });
            }

            return Ok(new ApiResponse<CommonPlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy chi tiết cây đại trà thành công",
                Payload = result
            });
        }

        /// <summary>
        /// [Shop] Tìm kiếm danh sách cây đại trà available tại một vựa
        /// </summary>
        [HttpPost("/api/shop/nurseries/{nurseryId}/common-plants/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchAvailableCommonPlantsByNursery(int nurseryId, [FromBody] PaginationSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var result = await _commonPlantService.GetActiveByNurseryIdAsync(nurseryId, pagination);
            return Ok(new ApiResponse<PaginatedResult<CommonPlantListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tìm kiếm danh sách cây đại trà tại vựa thành công",
                Payload = result
            });
        }

        #endregion

        #region Private Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                // For testing, return a default manager ID
                return 1;
            }
            return userId;
        }

        #endregion
    }
}
