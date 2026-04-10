using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
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
        /// Lấy chi tiết một PlantInstance theo ID
        /// GET /api/manager/plant-instances/{instanceId}
        /// </summary>
        [HttpGet("plant-instances/{instanceId}")]
        public async Task<IActionResult> GetPlantInstanceById(int instanceId)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.GetByIdAsync(instanceId, managerId);
            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy chi tiết plant instance thành công",
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

        /// <summary>
        /// Upload ảnh thumbnail cho PlantInstance
        /// POST /api/manager/plant-instances/{instanceId}/thumbnail
        /// </summary>
        [HttpPost("plant-instances/{instanceId}/thumbnail")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPlantInstanceThumbnail(int instanceId, IFormFile file)
        {
            if (file == null)
            {
                throw new BadRequestException("No file was uploaded");
            }

            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.UploadPlantInstanceThumbnailAsync(instanceId, managerId, file);
            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Upload plant instance thumbnail successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Upload ảnh cho PlantInstance
        /// POST /api/manager/plant-instances/{instanceId}/images
        /// </summary>
        [HttpPost("plant-instances/{instanceId}/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPlantInstanceImages(int instanceId, List<IFormFile> files)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.UploadPlantInstanceImagesAsync(instanceId, managerId, files);
            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Upload plant instance images successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Đặt ảnh primary cho PlantInstance
        /// PATCH /api/manager/plant-instances/{instanceId}/images/{imageId}/set-primary
        /// </summary>
        [HttpPatch("plant-instances/{instanceId}/images/{imageId}/set-primary")]
        public async Task<IActionResult> SetPrimaryPlantInstanceImage(int instanceId, int imageId)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.SetPrimaryInstanceImageAsync(instanceId, managerId, imageId);
            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Set primary plant instance image successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Thay thế ảnh cho PlantInstance
        /// PUT /api/manager/plant-instances/{instanceId}/images/{imageId}
        /// </summary>
        [HttpPut("plant-instances/{instanceId}/images/{imageId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReplacePlantInstanceImage(int instanceId, int imageId, IFormFile file)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.ReplaceInstanceImageAsync(instanceId, managerId, imageId, file);
            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Replace plant instance image successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Xóa ảnh của PlantInstance
        /// DELETE /api/manager/plant-instances/{instanceId}/images/{imageId}
        /// </summary>
        [HttpDelete("plant-instances/{instanceId}/images/{imageId}")]
        public async Task<IActionResult> DeletePlantInstanceImage(int instanceId, int imageId)
        {
            var managerId = GetCurrentUserId();
            var result = await _plantInstanceService.DeleteInstanceImageAsync(instanceId, managerId, imageId);
            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Delete plant instance image successfully",
                Payload = result
            });
        }

        #endregion

        #region Shop - Search Available Plant Instances
        /// <summary>
        /// [Shop] Tìm kiếm cây định danh đang available (toàn hệ thống hoặc theo vựa)
        /// POST /api/shop/plant-instances/search
        /// </summary>
        [HttpPost("/api/shop/plant-instances/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchAvailablePlantInstancesForShop([FromBody] ShopPlantInstanceSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var result = await _plantInstanceService.SearchAvailableForShopAsync(pagination, request?.NurseryId, request?.PlantId);
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
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Unable to identify user from token");
            return userId;
        }

        #endregion
    }
}
