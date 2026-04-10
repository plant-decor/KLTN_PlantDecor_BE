using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý catalog gói dịch vụ chăm sóc cây (Admin quản lý)
    /// </summary>
    [Route("api/care-service-packages")]
    [ApiController]
    public class CareServicePackagesController : ControllerBase
    {
        private readonly ICareServicePackageService _careServicePackageService;

        public CareServicePackagesController(ICareServicePackageService careServicePackageService)
        {
            _careServicePackageService = careServicePackageService;
        }

        /// <summary>
        /// [Public] Lấy danh sách các gói dịch vụ đang active
        /// </summary>
        [HttpGet]
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

    }
}
