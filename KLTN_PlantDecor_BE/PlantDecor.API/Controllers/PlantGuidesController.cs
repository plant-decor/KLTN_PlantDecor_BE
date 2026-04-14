using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý PlantGuide (Admin)
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class PlantGuidesController : ControllerBase
    {
        private readonly IPlantGuideService _plantGuideService;

        public PlantGuidesController(IPlantGuideService plantGuideService)
        {
            _plantGuideService = plantGuideService;
        }

        /// <summary>
        /// Lấy danh sách tất cả PlantGuide
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllPlantGuides([FromQuery] Pagination pagination)
        {
            var guides = await _plantGuideService.GetAllPlantGuidesAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantGuideResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get all plant guides successfully",
                Payload = guides
            });
        }

        /// <summary>
        /// Lấy PlantGuide theo ID
        /// </summary>
        [HttpGet("/api/PlantGuides/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlantGuideById(int id)
        {
            var guide = await _plantGuideService.GetPlantGuideByIdAsync(id);
            if (guide == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"PlantGuide with ID {id} not found"
                });
            }

            return Ok(new ApiResponse<PlantGuideResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant guide successfully",
                Payload = guide
            });
        }

        /// <summary>
        /// Lấy PlantGuide theo PlantId
        /// </summary>
        [HttpGet("/api/plants/{plantId}/guide")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlantGuideByPlantId(int plantId)
        {
            var guide = await _plantGuideService.GetPlantGuideByPlantIdAsync(plantId);
            if (guide == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"PlantGuide for Plant ID {plantId} not found"
                });
            }

            return Ok(new ApiResponse<PlantGuideResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant guide by plant id successfully",
                Payload = guide
            });
        }

        /// <summary>
        /// Tạo PlantGuide mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePlantGuide([FromBody] PlantGuideRequestDto request)
        {
            var guide = await _plantGuideService.CreatePlantGuideAsync(request);
            return CreatedAtAction(nameof(GetPlantGuideById), new { id = guide.Id }, new ApiResponse<PlantGuideResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Create plant guide successfully",
                Payload = guide
            });
        }

        /// <summary>
        /// Cập nhật PlantGuide
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdatePlantGuide(int id, [FromBody] PlantGuideUpdateDto request)
        {
            var guide = await _plantGuideService.UpdatePlantGuideAsync(id, request);
            return Ok(new ApiResponse<PlantGuideResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Update plant guide successfully",
                Payload = guide
            });
        }

        /// <summary>
        /// Xóa PlantGuide
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlantGuide(int id)
        {
            await _plantGuideService.DeletePlantGuideAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Delete plant guide successfully"
            });
        }
    }
}
