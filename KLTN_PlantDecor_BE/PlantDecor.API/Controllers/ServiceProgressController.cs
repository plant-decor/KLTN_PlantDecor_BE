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
    /// API quản lý tiến trình chăm sóc cây
    /// </summary>
    [Route("api/service-progress")]
    [ApiController]
    [Authorize]
    public class ServiceProgressController : ControllerBase
    {
        private readonly IServiceProgressService _serviceProgressService;

        public ServiceProgressController(IServiceProgressService serviceProgressService)
        {
            _serviceProgressService = serviceProgressService;
        }

        /// <summary>
        /// [Customer/Caretaker/Manager] Lấy chi tiết một tiến trình chăm sóc
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var result = await _serviceProgressService.GetByIdAsync(id, userId);
            return Ok(new ApiResponse<ServiceProgressResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get service progress successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Caretaker] Lấy lịch chăm sóc hôm nay
        /// </summary>
        [HttpGet("today")]
        [Authorize(Roles = "Caretaker")]
        public async Task<IActionResult> GetTodaySchedule()
        {
            var caretakerId = GetUserId();
            var result = await _serviceProgressService.GetTodayScheduleAsync(caretakerId);
            return Ok(new ApiResponse<List<ServiceProgressResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get today schedule successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer/Caretaker] Lấy danh sách tiến trình theo đăng ký dịch vụ
        /// </summary>
        [HttpGet("by-registration/{registrationId}")]
        public async Task<IActionResult> GetByRegistrationId(int registrationId)
        {
            var userId = GetUserId();
            var result = await _serviceProgressService.GetByRegistrationIdAsync(registrationId, userId);
            return Ok(new ApiResponse<List<ServiceProgressResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get progress list successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Caretaker] Check-in bắt đầu ca chăm sóc
        /// </summary>
        [HttpPost("{id}/check-in")]
        [Authorize(Roles = "Caretaker")]
        public async Task<IActionResult> CheckIn(int id)
        {
            var caretakerId = GetUserId();
            var result = await _serviceProgressService.CheckInAsync(caretakerId, id);
            return Ok(new ApiResponse<ServiceProgressResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Check-in successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Caretaker] Check-out kết thúc ca chăm sóc
        /// </summary>
        [HttpPost("{id}/check-out")]
        [Authorize(Roles = "Caretaker")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CheckOut(int id, [FromForm] CheckOutRequestDto request, IFormFile? evidenceImage)
        {
            var caretakerId = GetUserId();
            var result = await _serviceProgressService.CheckOutAsync(caretakerId, id, request, evidenceImage);
            return Ok(new ApiResponse<ServiceProgressResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Check-out successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Chuyển caretaker cho một phiên chăm sóc
        /// </summary>
        [HttpPut("{id}/reassign")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ReassignCaretaker(int id, [FromBody] ReassignCaretakerRequestDto request)
        {
            var managerId = GetUserId();
            var result = await _serviceProgressService.ReassignCaretakerAsync(managerId, id, request.NewCaretakerId);
            return Ok(new ApiResponse<ServiceProgressResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Caretaker reassigned successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Lịch chăm sóc toàn vựa theo ngày
        /// GET /api/service-progress/nursery-schedule?date=yyyy-MM-dd
        /// </summary>
        [HttpGet("nursery-schedule")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetNurserySchedule([FromQuery] DateOnly? date)
        {
            var managerId = GetUserId();
            var queryDate = date ?? DateOnly.FromDateTime(DateTime.Today);
            var result = await _serviceProgressService.GetNurseryScheduleAsync(managerId, queryDate);
            return Ok(new ApiResponse<List<ServiceProgressResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get nursery schedule successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Lịch của một caretaker theo khoảng ngày
        /// GET /api/service-progress/nursery-schedule/caretaker/{caretakerId}?from=&amp;to=
        /// </summary>
        [HttpGet("nursery-schedule/caretaker/{caretakerId}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetCaretakerSchedule(int caretakerId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        {
            var managerId = GetUserId();
            var fromDate = from ?? DateOnly.FromDateTime(DateTime.Today);
            var toDate = to ?? fromDate.AddDays(6);
            var result = await _serviceProgressService.GetCaretakerScheduleAsync(managerId, caretakerId, fromDate, toDate);
            return Ok(new ApiResponse<List<ServiceProgressResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get caretaker schedule successfully",
                Payload = result
            });
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Unable to identify user from token");
            return userId;
        }
    }
}
