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
    /// API quản lý mẫu thiết kế được cung cấp theo vườn ươm
    /// </summary>
    [Route("api")]
    [ApiController]
    [Authorize]
    public class NurseryDesignTemplatesController : ControllerBase
    {
        private readonly INurseryDesignTemplateService _nurseryDesignTemplateService;

        public NurseryDesignTemplatesController(INurseryDesignTemplateService nurseryDesignTemplateService)
        {
            _nurseryDesignTemplateService = nurseryDesignTemplateService;
        }

        /// <summary>
        /// [Public] Lấy danh sách mẫu thiết kế đang được vườn ươm cung cấp
        /// </summary>
        [HttpGet("public/nursery-design-templates")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveByNursery([FromQuery] int nurseryId)
        {
            var result = await _nurseryDesignTemplateService.GetActiveByNurseryIdAsync(nurseryId);
            return Ok(new ApiResponse<List<NurseryDesignTemplateResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get nursery design templates successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Public] Lấy danh sách vườn ươm đang cung cấp một mẫu thiết kế
        /// </summary>
        [HttpGet("public/nursery-design-templates/by-template")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveByTemplate([FromQuery] int designTemplateId)
        {
            var result = await _nurseryDesignTemplateService.GetActiveByTemplateIdAsync(designTemplateId);
            return Ok(new ApiResponse<List<NurseryDesignTemplateResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get nursery offerings by template successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Lấy danh sách mapping mẫu thiết kế của vườn ươm hiện tại
        /// </summary>
        [HttpGet("manager/nursery-design-templates/my")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetMine([FromQuery] bool activeOnly = false)
        {
            var managerId = GetUserId();
            var result = await _nurseryDesignTemplateService.GetByManagerAsync(managerId, activeOnly);
            return Ok(new ApiResponse<List<NurseryDesignTemplateResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get my nursery design templates successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Lấy danh sách mẫu thiết kế chưa được vườn ươm cung cấp
        /// </summary>
        [HttpGet("manager/nursery-design-templates/not-offered-templates")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetNotOfferedTemplates()
        {
            var managerId = GetUserId();
            var result = await _nurseryDesignTemplateService.GetNotOfferedByManagerAsync(managerId);
            return Ok(new ApiResponse<List<DesignTemplateOptionResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get not-offered design templates successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Thêm mẫu thiết kế vào danh sách cung cấp của vườn ươm
        /// </summary>
        [HttpPost("manager/nursery-design-templates")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Add([FromBody] CreateNurseryDesignTemplateRequestDto request)
        {
            var managerId = GetUserId();
            var result = await _nurseryDesignTemplateService.AddToMyNurseryAsync(managerId, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<NurseryDesignTemplateResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Design template added to nursery successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Bật/tắt trạng thái hoạt động của mapping mẫu thiết kế
        /// </summary>
        [HttpPatch("manager/nursery-design-templates/{id:int}/toggle")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Toggle(int id)
        {
            var managerId = GetUserId();
            var result = await _nurseryDesignTemplateService.ToggleActiveAsync(managerId, id);
            return Ok(new ApiResponse<NurseryDesignTemplateResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Nursery design template {(result.IsActive ? "activated" : "deactivated")} successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Xóa mẫu thiết kế khỏi danh sách cung cấp của vườn ươm
        /// </summary>
        [HttpDelete("manager/nursery-design-templates/{id:int}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Remove(int id)
        {
            var managerId = GetUserId();
            await _nurseryDesignTemplateService.RemoveFromMyNurseryAsync(managerId, id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design template removed from nursery successfully"
            });
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            return userId;
        }
    }
}
