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
        public async Task<IActionResult> GetAllInstances()
        {
            var instances = await _plantInstanceService.GetAllInstancesAsync();
            return Ok(new ApiResponse<IEnumerable<PlantInstanceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách plant instances thành công",
                Payload = instances
            });
        }

        /// <summary>
        /// Lấy plant instances theo PlantId
        /// </summary>
        [HttpGet("by-plant/{plantId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInstancesByPlantId(int plantId)
        {
            var instances = await _plantInstanceService.GetInstancesByPlantIdAsync(plantId);
            return Ok(new ApiResponse<IEnumerable<PlantInstanceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách plant instances theo plant thành công",
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
                    Message = $"Plant Instance với ID {id} không tồn tại"
                });

            return Ok(new ApiResponse<PlantInstanceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy plant instance thành công",
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
                Message = "Tạo plant instance thành công",
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
                Message = "Cập nhật plant instance thành công",
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
                Message = "Xóa plant instance thành công"
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
                Message = $"Cập nhật trạng thái thành '{status}' thành công"
            });
        }
    }
}
