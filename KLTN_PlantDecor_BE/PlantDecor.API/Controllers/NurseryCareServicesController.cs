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
    /// API quản lý gói dịch vụ của từng vựa
    /// </summary>
    [Route("api/nursery-care-services")]
    [ApiController]
    [Authorize]
    public class NurseryCareServicesController : ControllerBase
    {
        private readonly INurseryCareServiceService _nurseryCareServiceService;
        private readonly ICareServicePackageService _careServicePackageService;

        public NurseryCareServicesController(
            INurseryCareServiceService nurseryCareServiceService,
            ICareServicePackageService careServicePackageService)
        {
            _nurseryCareServiceService = nurseryCareServiceService;
            _careServicePackageService = careServicePackageService;
        }

        /// <summary>
        /// [Public] Lấy danh sách NurseryCareService đang active theo gói dịch vụ
        /// </summary>
        [HttpGet("by-package")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveByPackage([FromQuery] int careServicePackageId)
        {
            var result = await _nurseryCareServiceService.GetActiveByPackageIdAsync(careServicePackageId);
            return Ok(new ApiResponse<List<NurseryCareServiceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get nursery care services by package successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Public] Lấy các gói dịch vụ đang active của một vựa
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveByNursery([FromQuery] int nurseryId)
        {
            var result = await _nurseryCareServiceService.GetActiveByNurseryIdAsync(nurseryId);
            return Ok(new ApiResponse<List<NurseryCareServiceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get nursery care services successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Lấy các gói dịch vụ vựa mình đang kinh doanh (IsActive = true)
        /// </summary>
        [HttpGet("my/active")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetActiveMine()
        {
            var managerId = GetUserId();
            var result = await _nurseryCareServiceService.GetActiveByManagerAsync(managerId);
            return Ok(new ApiResponse<List<NurseryCareServiceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get active nursery care services successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Lấy tất cả gói dịch vụ của vựa mình (kể cả inactive)
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetAllMine()
        {
            var managerId = GetUserId();
            var result = await _nurseryCareServiceService.GetAllByManagerAsync(managerId);
            return Ok(new ApiResponse<List<NurseryCareServiceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get nursery care services successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Thêm một gói dịch vụ vào vựa
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Add([FromBody] CreateNurseryCareServiceRequestDto request)
        {
            var managerId = GetUserId();
            var result = await _nurseryCareServiceService.AddToNurseryAsync(managerId, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<NurseryCareServiceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Care service package added to nursery successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Bật/tắt gói dịch vụ của vựa
        /// </summary>
        [HttpPatch("{id}/toggle")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Toggle(int id)
        {
            var managerId = GetUserId();
            var result = await _nurseryCareServiceService.ToggleActiveAsync(managerId, id);
            return Ok(new ApiResponse<NurseryCareServiceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Care service {(result.IsActive ? "activated" : "deactivated")} successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Lấy các gói dịch vụ vựa mình chưa kinh doanh (để thêm vào vựa)
        /// </summary>
        [HttpGet("not-offered-packages")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetNotOfferedPackages()
        {
            var managerId = GetUserId();
            var result = await _careServicePackageService.GetNotOfferedByManagerAsync(managerId);
            return Ok(new ApiResponse<List<CareServicePackageResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get not-offered packages successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Xóa gói dịch vụ khỏi vựa (chỉ khi chưa có đăng ký nào)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Remove(int id)
        {
            var managerId = GetUserId();
            await _nurseryCareServiceService.RemoveFromNurseryAsync(managerId, id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Care service removed from nursery successfully"
            });
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Unable to identify user from token");
            return userId;
        }
    }
}
