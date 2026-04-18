using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class DesignTemplatesController : ControllerBase
    {
        private readonly IDesignTemplateService _designTemplateService;

        public DesignTemplatesController(IDesignTemplateService designTemplateService)
        {
            _designTemplateService = designTemplateService;
        }

        [HttpGet("public/design-templates")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _designTemplateService.GetAllAsync();
            return Ok(new ApiResponse<List<DesignTemplateResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design templates successfully",
                Payload = result
            });
        }

        [HttpGet("public/design-templates/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _designTemplateService.GetByIdAsync(id);
            return Ok(new ApiResponse<DesignTemplateResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design template successfully",
                Payload = result
            });
        }

        [HttpPost("admin/design-templates")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateDesignTemplateRequestDto request)
        {
            var result = await _designTemplateService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<DesignTemplateResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Design template created successfully",
                Payload = result
            });
        }

        [HttpPut("admin/design-templates/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDesignTemplateRequestDto request)
        {
            var result = await _designTemplateService.UpdateAsync(id, request);
            return Ok(new ApiResponse<DesignTemplateResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design template updated successfully",
                Payload = result
            });
        }

        [HttpPut("admin/design-templates/{id:int}/specializations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSpecializations(int id, [FromBody] SetDesignTemplateSpecializationsRequestDto request)
        {
            var result = await _designTemplateService.UpdateSpecializationsAsync(id, request.SpecializationIds);
            return Ok(new ApiResponse<DesignTemplateResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design template specializations updated successfully",
                Payload = result
            });
        }

        [HttpDelete("admin/design-templates/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _designTemplateService.DeleteAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design template deleted successfully"
            });
        }
    }
}
