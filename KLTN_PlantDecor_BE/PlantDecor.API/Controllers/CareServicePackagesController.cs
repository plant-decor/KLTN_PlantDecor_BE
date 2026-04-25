using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý catalog gói dịch vụ chăm sóc cây (Admin quản lý)
    /// </summary>
    [Route("api/care-service-packages")]
    [ApiController]
    [Authorize]
    public class CareServicePackagesController : ControllerBase
    {
        private readonly ICareServicePackageService _careServicePackageService;

        public CareServicePackagesController(ICareServicePackageService careServicePackageService)
        {
            _careServicePackageService = careServicePackageService;
        }

        /// <summary>
        /// [Public] Lấy danh sách gói dịch vụ kèm các vựa đang cung cấp (trả về NurseryCareServiceId)
        /// </summary>
        [HttpGet("with-nurseries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPackagesWithNurseries()
        {
            var result = await _careServicePackageService.GetPackagesWithNurseriesAsync();
            return Ok(new ApiResponse<List<CareServicePackageWithNurseriesResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get care service packages with nurseries successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Public] Lấy danh sách các gói dịch vụ đang active
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllActive()
        {
            var result = await _careServicePackageService.GetAllActiveAsync();
            return Ok(new ApiResponse<List<CareServicePackageResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get care service packages successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Lấy tất cả gói dịch vụ kể cả inactive
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _careServicePackageService.GetAllAsync();
            return Ok(new ApiResponse<List<CareServicePackageResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get all care service packages successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Public] Lấy chi tiết một gói dịch vụ
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _careServicePackageService.GetByIdAsync(id);
            return Ok(new ApiResponse<CareServicePackageResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get care service package successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Public] Lấy chi tiết một gói dịch vụ kèm các vựa đang cung cấp
        /// </summary>
        [HttpGet("{id}/with-nurseries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByIdWithNurseries(int id)
        {
            var result = await _careServicePackageService.GetByIdWithNurseriesAsync(id);
            return Ok(new ApiResponse<CareServicePackageWithNurseriesResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get care service package with nurseries successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Consultant] Gợi ý gói dịch vụ chăm sóc dựa trên dữ liệu cây trong đơn hàng
        /// </summary>
        [HttpGet("recommendations/orders/{orderId}")]
        [Authorize(Roles = "Consultant")]
        public async Task<IActionResult> RecommendByOrder(int orderId, [FromQuery] int top = 5)
        {
            var consultantId = GetUserId();
            var result = await _careServicePackageService.RecommendByOrderAsync(consultantId, orderId, top);

            return Ok(new ApiResponse<List<CareServicePackageRecommendationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get care service package recommendations successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Tạo gói dịch vụ mới
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCareServicePackageRequestDto request)
        {
            var result = await _careServicePackageService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<CareServicePackageResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Care service package created successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Cập nhật gói dịch vụ
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCareServicePackageRequestDto request)
        {
            var result = await _careServicePackageService.UpdateAsync(id, request);
            return Ok(new ApiResponse<CareServicePackageResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Care service package updated successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Vô hiệu hóa gói dịch vụ (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _careServicePackageService.DeleteAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Care service package deactivated successfully"
            });
        }

        /// <summary>
        /// [Admin] Cập nhật (thay thế toàn bộ) danh sách chuyên môn của gói dịch vụ
        /// </summary>
        [HttpPut("{id}/specializations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSpecializations(int id, [FromBody] SetSpecializationsDto request)
        {
            var result = await _careServicePackageService.UpdateSpecializationsAsync(id, request.SpecializationIds);
            return Ok(new ApiResponse<CareServicePackageResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Package specializations updated successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Cập nhật (thay thế toàn bộ) danh sách điều kiện phù hợp của gói dịch vụ
        /// </summary>
        [HttpPut("{id}/suitability-rules")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSuitabilityRules(int id, [FromBody] SetSuitabilityRulesDto request)
        {
            var result = await _careServicePackageService.UpdateSuitabilityRulesAsync(id, request.SuitabilityRules);
            return Ok(new ApiResponse<CareServicePackageResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Package suitability rules updated successfully",
                Payload = result
            });
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Unable to identify user from token");

            return userId;
        }

    }
}
