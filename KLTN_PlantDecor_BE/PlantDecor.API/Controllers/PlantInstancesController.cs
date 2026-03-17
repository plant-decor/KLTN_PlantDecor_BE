using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý PlantInstance cho Manager
    /// </summary>
    [Route("api/manager")]
    [ApiController]
    //[Authorize(Roles = "Manager")]
    public class PlantInstancesController : ControllerBase
    {
        private readonly IPlantInstanceService _plantInstanceService;

        public PlantInstancesController(IPlantInstanceService plantInstanceService)
        {
            _plantInstanceService = plantInstanceService;
        }

        #region Manager - Nursery Plant Instances

        /// <summary>
        /// Batch tạo nhiều PlantInstance cho một nursery
        /// POST /api/manager/nurseries/{nurseryId}/plant-instances/batch
        /// </summary>
        [HttpPost("nurseries/{nurseryId}/plant-instances/batch")]
        public async Task<IActionResult> BatchCreatePlantInstances(int nurseryId, [FromBody] BatchCreatePlantInstanceRequestDto request)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.BatchCreateAsync(nurseryId, managerId, request);
            return CreatedAtAction(nameof(GetPlantInstancesByNursery), new { nurseryId }, new ApiResponse<BatchCreatePlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = $"Tạo thành công {result.TotalCreated} plant instance(s)",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy danh sách PlantInstance theo nursery
        /// GET /api/manager/nurseries/{nurseryId}/plant-instances
        /// </summary>
        [HttpGet("nurseries/{nurseryId}/plant-instances")]
        public async Task<IActionResult> GetPlantInstancesByNursery(int nurseryId, [FromQuery] Pagination pagination, [FromQuery] int? status = null)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.GetByNurseryIdAsync(nurseryId, managerId, pagination, status);
            return Ok(new ApiResponse<PaginatedResult<PlantInstanceListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách plant instances thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy tổng hợp thông tin plant theo nursery  
        /// GET /api/manager/nurseries/{nurseryId}/plants-summary
        /// </summary>
        [HttpGet("nurseries/{nurseryId}/plants-summary")]
        public async Task<IActionResult> GetPlantsSummary(int nurseryId)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.GetPlantsSummaryByNurseryAsync(nurseryId, managerId);
            return Ok(new ApiResponse<List<NurseryPlantSummaryDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy tổng hợp plants thành công",
                Payload = result
            });
        }

        #endregion

        #region Manager - Instance Status Management

        /// <summary>
        /// Cập nhật status một PlantInstance
        /// PATCH /api/manager/plant-instances/{instanceId}/status
        /// </summary>
        [HttpPatch("plant-instances/{instanceId}/status")]
        public async Task<IActionResult> UpdateInstanceStatus(int instanceId, [FromBody] UpdatePlantInstanceStatusDto request)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.UpdateStatusAsync(instanceId, managerId, request);
            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật status thành công",
                Payload = result
            });
        }

        /// <summary>
        /// Batch cập nhật status nhiều PlantInstance
        /// PATCH /api/manager/plant-instances/batch-status
        /// </summary>
        [HttpPatch("plant-instances/batch-status")]
        public async Task<IActionResult> BatchUpdateStatus([FromBody] BatchUpdatePlantInstanceStatusDto request)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.BatchUpdateStatusAsync(managerId, request);
            return Ok(new ApiResponse<BatchUpdateStatusResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Cập nhật status thành công cho {result.TotalUpdated} instance(s)",
                Payload = result
            });
        }

        #endregion

        #region Shop Operations

        /// <summary>
        /// [Shop] Tìm kiếm cây định danh đang available (toàn hệ thống hoặc theo vựa)
        /// POST /api/shop/plant-instances/search
        /// </summary>
        [HttpPost("/api/shop/plant-instances/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchAvailablePlantInstancesForShop([FromBody] ShopPlantInstanceSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var result = await _plantInstanceService.SearchAvailableForShopAsync(pagination, request?.NurseryId);
            return Ok(new ApiResponse<PaginatedResult<PlantInstanceListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tìm kiếm cây định danh cho shop thành công",
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
                return 1;
            }
            return userId;
        }

        #endregion
    }
}
