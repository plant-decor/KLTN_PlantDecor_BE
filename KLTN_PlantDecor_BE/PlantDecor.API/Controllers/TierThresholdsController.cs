using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class TierThresholdsController : ControllerBase
    {
        private readonly ITierThresholdService _tierThresholdService;

        public TierThresholdsController(ITierThresholdService tierThresholdService)
        {
            _tierThresholdService = tierThresholdService;
        }

        [HttpGet("public/tier-thresholds")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllActive()
        {
            var result = await _tierThresholdService.GetAllActiveAsync();
            return Ok(new ApiResponse<List<TierThresholdResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tier thresholds retrieved successfully",
                Payload = result
            });
        }

        [HttpGet("admin/tier-thresholds")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _tierThresholdService.GetAllAsync();
            return Ok(new ApiResponse<List<TierThresholdResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "All tier thresholds retrieved successfully",
                Payload = result
            });
        }

        [HttpPost("admin/tier-thresholds")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateTierThresholdDto dto)
        {
            var result = await _tierThresholdService.CreateAsync(dto);
            return Ok(new ApiResponse<TierThresholdResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tier threshold created successfully",
                Payload = result
            });
        }

        [HttpPut("admin/tier-thresholds/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTierThresholdDto dto)
        {
            var result = await _tierThresholdService.UpdateAsync(id, dto);
            return Ok(new ApiResponse<TierThresholdResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tier threshold updated successfully",
                Payload = result
            });
        }

        [HttpDelete("admin/tier-thresholds/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate([FromRoute] int id)
        {
            await _tierThresholdService.DeactivateAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tier threshold deactivated successfully"
            });
        }
    }
}
