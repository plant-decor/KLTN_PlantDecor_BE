using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý đăng ký dịch vụ thiết kế
    /// </summary>
    [Route("api/design-registrations")]
    [ApiController]
    [Authorize]
    public class DesignRegistrationsController : ControllerBase
    {
        private readonly IDesignRegistrationService _designRegistrationService;

        public DesignRegistrationsController(IDesignRegistrationService designRegistrationService)
        {
            _designRegistrationService = designRegistrationService;
        }

        /// <summary>
        /// [Customer] Tạo đăng ký thiết kế
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] CreateDesignRegistrationRequestDto request)
        {
            var userId = GetUserId();
            var result = await _designRegistrationService.CreateAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<DesignRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Design registration created successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer] Lấy danh sách đăng ký thiết kế của tôi
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyRegistrations([FromQuery] Pagination pagination, [FromQuery] int? status = null)
        {
            var userId = GetUserId();
            var result = await _designRegistrationService.GetMyRegistrationsAsync(userId, pagination, status);
            return Ok(new ApiResponse<PaginatedResult<DesignRegistrationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design registrations successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer/Caretaker] Lấy chi tiết đăng ký thiết kế
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Customer,Caretaker")]
        public async Task<IActionResult> GetById(int id)
        {
            var requesterId = GetUserId();
            var result = await _designRegistrationService.GetByIdAsync(id, requesterId);
            return Ok(new ApiResponse<DesignRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design registration successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Giao caretaker cho đăng ký thiết kế và cập nhật toàn bộ task chưa hoàn tất
        /// </summary>
        [HttpPut("{id}/assign-caretaker")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> AssignCaretaker(int id, [FromBody] AssignDesignRegistrationCaretakerRequestDto request)
        {
            var managerId = GetUserId();
            var result = await _designRegistrationService.AssignCaretakerAsync(managerId, id, request.CaretakerId, request.StartDate);
            return Ok(new ApiResponse<DesignRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design registration caretaker assigned successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Caretaker] Cập nhật thông tin khảo sát thực tế và ảnh hiện trạng cho đăng ký thiết kế
        /// </summary>
        [HttpPut("{id}/survey-info")]
        [Authorize(Roles = "Caretaker")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateSurveyInfo(int id, [FromForm] UpdateDesignRegistrationSurveyInfoRequestDto request, IFormFile? currentStateImage = null)
        {
            var caretakerId = GetUserId();
            var result = await _designRegistrationService.UpdateSurveyInfoAsync(caretakerId, id, request, currentStateImage);
            return Ok(new ApiResponse<DesignRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design registration survey info updated successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Lấy danh sách đăng ký thiết kế chờ duyệt của vựa
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetPendingForNursery([FromQuery] Pagination pagination)
        {
            var managerId = GetUserId();
            var result = await _designRegistrationService.GetPendingForNurseryAsync(managerId, pagination);
            return Ok(new ApiResponse<PaginatedResult<DesignRegistrationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get pending design registrations successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Lấy danh sách đăng ký thiết kế theo vựa
        /// </summary>
        [HttpGet("nursery")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetAllForNursery([FromQuery] Pagination pagination, [FromQuery] int? status = null)
        {
            var managerId = GetUserId();
            var result = await _designRegistrationService.GetAllForNurseryAsync(managerId, pagination, status);
            return Ok(new ApiResponse<PaginatedResult<DesignRegistrationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get nursery design registrations successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Lấy chi tiết đăng ký thiết kế thuộc vựa
        /// </summary>
        [HttpGet("nursery/{id}")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetByIdAsOperator(int id)
        {
            var managerId = GetUserId();
            var result = await _designRegistrationService.GetByIdAsOperatorAsync(managerId, id);
            return Ok(new ApiResponse<DesignRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design registration successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Phê duyệt đăng ký thiết kế
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> Approve(int id)
        {
            var managerId = GetUserId();
            var result = await _designRegistrationService.ApproveAsync(managerId, id);
            return Ok(new ApiResponse<DesignRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design registration approved successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Từ chối đăng ký thiết kế
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> Reject(int id, [FromQuery] string? rejectReason = null)
        {
            var managerId = GetUserId();
            var result = await _designRegistrationService.RejectAsync(managerId, id, rejectReason);
            return Ok(new ApiResponse<DesignRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design registration rejected successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Lấy danh sách caretaker đủ điều kiện cho đăng ký thiết kế
        /// </summary>
        [HttpGet("{id}/eligible-caretakers")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetEligibleCaretakers(int id)
        {
            var managerId = GetUserId();
            var result = await _designRegistrationService.GetEligibleCaretakersForRegistrationAsync(managerId, id);
            return Ok(new ApiResponse<List<StaffWithSpecializationsResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get eligible caretakers successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Lấy danh sách caretaker phù hợp với kiểm tra khả dụng (xung đột lịch)
        /// </summary>
        [HttpGet("{id}/eligible-caretakers-with-availability")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetEligibleCaretakersWithAvailability(int id, [FromQuery] DateOnly? startDate = null)
        {
            var managerId = GetUserId();
            var result = await _designRegistrationService.GetEligibleCaretakersForRegistrationWithAvailabilityAsync(managerId, id, startDate);
            return Ok(new ApiResponse<List<EligibleCaretakerWithAvailabilityDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get eligible caretakers with availability successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer] Hủy đăng ký thiết kế
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id, [FromQuery] string? cancelReason = null)
        {
            var userId = GetUserId();
            var result = await _designRegistrationService.CancelAsync(userId, id, cancelReason);
            return Ok(new ApiResponse<DesignRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design registration cancelled successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Hủy đăng ký thiết kế
        /// </summary>
        [HttpPost("{id}/manager-cancel")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> ManagerCancel(int id, [FromQuery] string? cancelReason = null)
        {
            var managerId = GetUserId();
            var result = await _designRegistrationService.ManagerCancelAsync(managerId, id, cancelReason);
            return Ok(new ApiResponse<DesignRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design registration cancelled by nursery",
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
