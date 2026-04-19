using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý hạng mục tầng của mẫu thiết kế
    /// </summary>
    [Route("api")]
    [ApiController]
    [Authorize]
    public class DesignTemplateTiersController : ControllerBase
    {
        private readonly IDesignTemplateTierService _designTemplateTierService;

        public DesignTemplateTiersController(IDesignTemplateTierService designTemplateTierService)
        {
            _designTemplateTierService = designTemplateTierService;
        }

        /// <summary>
        /// [Public] Lấy danh sách tier theo mẫu thiết kế
        /// </summary>
        [HttpGet("public/design-template-tiers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByTemplate([FromQuery] int designTemplateId, [FromQuery] bool includeInactive = false)
        {
            var result = await _designTemplateTierService.GetByTemplateIdAsync(designTemplateId, includeInactive);
            return Ok(new ApiResponse<List<DesignTemplateTierResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design template tiers successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Public] Lấy chi tiết tier của mẫu thiết kế
        /// </summary>
        [HttpGet("public/design-template-tiers/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _designTemplateTierService.GetByIdAsync(id);
            return Ok(new ApiResponse<DesignTemplateTierResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design template tier successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Tạo tier mới cho mẫu thiết kế
        /// </summary>
        [HttpPost("admin/design-template-tiers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateDesignTemplateTierRequestDto request)
        {
            var result = await _designTemplateTierService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<DesignTemplateTierResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Design template tier created successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Cập nhật thông tin tier của mẫu thiết kế
        /// </summary>
        [HttpPut("admin/design-template-tiers/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDesignTemplateTierRequestDto request)
        {
            var result = await _designTemplateTierService.UpdateAsync(id, request);
            return Ok(new ApiResponse<DesignTemplateTierResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design template tier updated successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Cập nhật danh sách item trong tier
        /// </summary>
        [HttpPut("admin/design-template-tiers/{id:int}/items")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetItems(int id, [FromBody] SetDesignTemplateTierItemsRequestDto request)
        {
            var result = await _designTemplateTierService.SetItemsAsync(id, request.Items);
            return Ok(new ApiResponse<DesignTemplateTierResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design template tier items updated successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Admin] Hủy kích hoạt tier của mẫu thiết kế
        /// </summary>
        [HttpDelete("admin/design-template-tiers/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _designTemplateTierService.DeleteAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design template tier deactivated successfully"
            });
        }
    }
}
