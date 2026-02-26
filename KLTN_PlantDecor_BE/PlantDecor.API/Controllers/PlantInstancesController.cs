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
    /// API quản lý Plant Instance (Admin)
    /// </summary>
    [Route("api/admin/plant-instances")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class PlantInstancesController : ControllerBase
    {
        private readonly IPlantInstanceService _plantInstanceService;

        public PlantInstancesController(IPlantInstanceService plantInstanceService)
        {
            _plantInstanceService = plantInstanceService;
        }

        /// <summary>
        /// Lấy tất cả plant instances
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllInstances([FromQuery] Pagination pagination)
        {
            var instances = await _plantInstanceService.GetAllInstancesAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantInstanceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get all plant instances successfully",
                Payload = instances
            });
        }

        /// <summary>
        /// Lấy plant instances theo PlantId
        /// </summary>
        [HttpGet("by-plant/{plantId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInstancesByPlantId(int plantId, [FromQuery] Pagination pagination)
        {
            var instances = await _plantInstanceService.GetInstancesByPlantIdAsync(plantId, pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantInstanceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant instances by plant successfully",
                Payload = instances
            });
        }

        /// <summary>
        /// Lấy plant instance theo ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInstanceById(int id)
        {
            var instance = await _plantInstanceService.GetInstanceByIdAsync(id);
            if (instance == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Plant instance with ID {id} not found"
                });

            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant instance successfully",
                Payload = instance
            });
        }

        /// <summary>
        /// Tạo plant instance mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateInstance([FromBody] PlantInstanceRequestDto request)
        {
            var instance = await _plantInstanceService.CreateInstanceAsync(request);
            return CreatedAtAction(nameof(GetInstanceById), new { id = instance.Id }, new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Create plant instance successfully",
                Payload = instance
            });
        }

        /// <summary>
        /// Cập nhật plant instance (partial update)
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateInstance(int id, [FromBody] PlantInstanceUpdateDto request)
        {
            var instance = await _plantInstanceService.UpdateInstanceAsync(id, request);
            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Update plant instance successfully",
                Payload = instance
            });
        }

        /// <summary>
        /// Xóa plant instance
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInstance(int id)
        {
            await _plantInstanceService.DeleteInstanceAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Delete plant instance successfully"
            });
        }

        /// <summary>
        /// Cập nhật trạng thái plant instance
        /// </summary>
        /// <param name="id">ID của instance</param>
        /// <param name="status">Trạng thái mới: 1=Available, 2=Sold, 3=Reserved, 4=Damaged, 5=Inavailable</param>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] int status)
        {
            await _plantInstanceService.UpdateStatusAsync(id, status);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Update status to '{status}' successfully"
            });
        }
    }
}
