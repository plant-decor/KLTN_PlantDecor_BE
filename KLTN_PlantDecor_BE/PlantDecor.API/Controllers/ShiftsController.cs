using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    [ApiController]
    public class ShiftsController : ControllerBase
    {
        private readonly IShiftService _shiftService;

        public ShiftsController(IShiftService shiftService)
        {
            _shiftService = shiftService;
        }

        /// <summary>
        /// Lấy danh sách tất cả ca làm việc (public)
        /// </summary>
        [HttpGet("api/shifts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _shiftService.GetAllAsync();
            return Ok(new ApiResponse<List<ShiftResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get shifts successfully",
                Payload = result
            });
        }
    }
}
