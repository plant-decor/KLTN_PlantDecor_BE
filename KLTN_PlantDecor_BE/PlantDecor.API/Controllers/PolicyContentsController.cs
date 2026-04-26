using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý PolicyContent phục vụ AI grounding.
    /// </summary>
    [Route("api/policy-contents")]
    [ApiController]
    public class PolicyContentsController : ControllerBase
    {
        private readonly IPolicyContentService _policyContentService;

        public PolicyContentsController(IPolicyContentService policyContentService)
        {
            _policyContentService = policyContentService;
        }

        /// <summary>
        /// [Public] Lấy toàn bộ policy đang active.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllActive()
        {
            var result = await _policyContentService.GetAllActiveAsync();
            return Ok(new ApiResponse<List<PolicyContentResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get active policy contents successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Public] Lấy policy active theo category.
        /// </summary>
        [HttpGet("categories/{category:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCategoryActive(int category)
        {
            var result = await _policyContentService.GetByCategoryActiveAsync(category);
            return Ok(new ApiResponse<List<PolicyContentResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get policy contents by category successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Public] Lấy chi tiết 1 policy đang active.
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _policyContentService.GetByIdAsync(id, includeInactive: false);
            return Ok(new ApiResponse<PolicyContentResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get policy content successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Lấy toàn bộ policy, có thể bao gồm inactive.
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminList([FromQuery] bool includeInactive = true)
        {
            var result = await _policyContentService.GetAdminListAsync(includeInactive);
            return Ok(new ApiResponse<List<PolicyContentResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get policy admin list successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Tạo policy content mới.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreatePolicyContentRequestDto request)
        {
            var result = await _policyContentService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<PolicyContentResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Policy content created successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Cập nhật policy content.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePolicyContentRequestDto request)
        {
            var result = await _policyContentService.UpdateAsync(id, request);
            return Ok(new ApiResponse<PolicyContentResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Policy content updated successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Kích hoạt/Vô hiệu hóa policy content.
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetPolicyContentStatusRequestDto request)
        {
            var result = await _policyContentService.SetActiveStatusAsync(id, request.IsActive);
            return Ok(new ApiResponse<PolicyContentResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Policy content status updated successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Xóa mềm policy content (đặt IsActive=false).
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _policyContentService.DeleteAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Policy content deactivated successfully",
                Payload = null
            });
        }
    }
}
