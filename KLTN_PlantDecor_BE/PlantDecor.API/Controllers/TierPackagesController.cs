using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý Tier Package (Admin)
    /// </summary>
    [Route("api")]
    [ApiController]
    public class TierPackagesController : ControllerBase
    {
        private readonly ITierPackageService _tierPackageService;

        public TierPackagesController(ITierPackageService tierPackageService)
        {
            _tierPackageService = tierPackageService;
        }

        [HttpGet("public/tier-packages")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllActive()
        {
            var result = await _tierPackageService.GetAllActiveAsync();
            return Ok(new ApiResponse<List<TierPackageResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tier packages retrieved successfully",
                Payload = result
            });
        }

        [HttpGet("public/tier-packages/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var result = await _tierPackageService.GetByIdAsync(id);
            return Ok(new ApiResponse<TierPackageResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tier package retrieved successfully",
                Payload = result
            });
        }

        [HttpGet("admin/tier-packages")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _tierPackageService.GetAllAsync();
            return Ok(new ApiResponse<List<TierPackageResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "All tier packages retrieved successfully",
                Payload = result
            });
        }

        [HttpPost("admin/tier-packages")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateTierPackageDto dto)
        {
            var result = await _tierPackageService.CreateAsync(dto);
            return Ok(new ApiResponse<TierPackageResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tier package created successfully",
                Payload = result
            });
        }

        [HttpPut("admin/tier-packages/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTierPackageDto dto)
        {
            var result = await _tierPackageService.UpdateAsync(id, dto);
            return Ok(new ApiResponse<TierPackageResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tier package updated successfully",
                Payload = result
            });
        }

        [HttpDelete("admin/tier-packages/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate([FromRoute] int id)
        {
            await _tierPackageService.DeactivateAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tier package deactivated successfully"
            });
        }
    }
}
