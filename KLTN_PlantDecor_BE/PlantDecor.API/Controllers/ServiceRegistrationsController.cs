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
    /// API đăng ký dịch vụ chăm sóc cây
    /// </summary>
    [Route("api/service-registrations")]
    [ApiController]
    [Authorize]
    public class ServiceRegistrationsController : ControllerBase
    {
        private readonly IServiceRegistrationService _serviceRegistrationService;
        private readonly IServiceCareBackgroundJobService _serviceCareBgJob;

        public ServiceRegistrationsController(
            IServiceRegistrationService serviceRegistrationService,
            IServiceCareBackgroundJobService serviceCareBgJob)
        {
            _serviceRegistrationService = serviceRegistrationService;
            _serviceCareBgJob = serviceCareBgJob;
        }

        /// <summary>
        /// [Customer] Tạo đăng ký dịch vụ chăm sóc cây
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateServiceRegistrationRequestDto request)
        {
            var userId = GetUserId();
            var result = await _serviceRegistrationService.CreateAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<ServiceRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Service registration created successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer] Lấy danh sách đăng ký dịch vụ của tôi
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyRegistrations([FromQuery] Pagination pagination, [FromQuery] int? status = null)
        {
            var userId = GetUserId();
            var result = await _serviceRegistrationService.GetMyRegistrationsAsync(userId, pagination, status);
            return Ok(new ApiResponse<PaginatedResult<ServiceRegistrationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get registrations successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer/Caretaker] Lấy chi tiết đăng ký dịch vụ
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var result = await _serviceRegistrationService.GetByIdAsync(id, userId);
            return Ok(new ApiResponse<ServiceRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get registration successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Lấy danh sách đăng ký chờ duyệt của vựa
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetPendingForNursery([FromQuery] Pagination pagination)
        {
            var managerId = GetUserId();
            var result = await _serviceRegistrationService.GetPendingForNurseryAsync(managerId, pagination);
            return Ok(new ApiResponse<PaginatedResult<ServiceRegistrationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get pending registrations successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Lấy chi tiết đăng ký dịch vụ thuộc vựa vận hành
        /// </summary>
        [HttpGet("nursery/{id}")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetByIdAsManager(int id)
        {
            var managerId = GetUserId();
            var result = await _serviceRegistrationService.GetByIdAsManagerAsync(managerId, id);
            return Ok(new ApiResponse<ServiceRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get registration successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Lấy tất cả đăng ký dịch vụ của vựa, có thể lọc theo trạng thái
        /// </summary>
        [HttpGet("nursery")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetAllForNursery([FromQuery] Pagination pagination, [FromQuery] int? status = null)
        {
            var managerId = GetUserId();
            var result = await _serviceRegistrationService.GetAllForNurseryAsync(managerId, pagination, status);
            return Ok(new ApiResponse<PaginatedResult<ServiceRegistrationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get nursery registrations successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Phê duyệt đăng ký dịch vụ — tạo Order + Invoice
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> Approve(int id)
        {
            var managerId = GetUserId();
            var result = await _serviceRegistrationService.ApproveAsync(managerId, id);
            return Ok(new ApiResponse<ServiceRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Registration approved successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Từ chối đăng ký dịch vụ
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> Reject(int id, [FromQuery] string? rejectReason = null)
        {
            var managerId = GetUserId();
            var result = await _serviceRegistrationService.RejectAsync(managerId, id, rejectReason);
            return Ok(new ApiResponse<ServiceRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Registration rejected successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Giao caretaker chính cho đăng ký dịch vụ và cập nhật các phiên chưa hoàn tất
        /// </summary>
        [HttpPut("{id}/assign-caretaker")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> AssignCaretaker(int id, [FromBody] AssignServiceRegistrationCaretakerRequestDto request)
        {
            var managerId = GetUserId();
            var result = await _serviceRegistrationService.AssignCaretakerAsync(managerId, id, request.CaretakerId);
            return Ok(new ApiResponse<ServiceRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Caretaker assigned successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer] Hủy đăng ký dịch vụ (chỉ khi PendingApproval hoặc AwaitPayment)
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromQuery] string? cancelReason = null)
        {
            var userId = GetUserId();
            var result = await _serviceRegistrationService.CancelAsync(userId, id, cancelReason);
            return Ok(new ApiResponse<ServiceRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Registration cancelled successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Caretaker] Lấy danh sách đơn dịch vụ được giao (có thể lọc theo status)
        /// </summary>
        [HttpGet("my-tasks")]
        [Authorize(Roles = "Caretaker")]
        public async Task<IActionResult> GetMyTasks([FromQuery] Pagination pagination, [FromQuery] int? status = null)
        {
            var caretakerId = GetUserId();
            var result = await _serviceRegistrationService.GetMyTasksAsync(caretakerId, pagination, status);
            return Ok(new ApiResponse<PaginatedResult<ServiceRegistrationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get caretaker tasks successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Hủy đăng ký dịch vụ đang Active — hủy các session chưa xong và set Order = Cancelled
        /// </summary>
        [HttpPost("{id}/manager-cancel")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> ManagerCancel(int id, [FromQuery] string? cancelReason = null)
        {
            var managerId = GetUserId();
            var result = await _serviceRegistrationService.ManagerCancelAsync(managerId, id, cancelReason);
            return Ok(new ApiResponse<ServiceRegistrationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Registration cancelled by manager",
                Payload = result
            });
        }

        /// <summary>
        /// [Dev] Trigger sinh lịch dịch vụ thủ công (dùng để test khi không có VNPay)
        /// </summary>
        [HttpPost("{id}/generate-schedule")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateSchedule(int id)
        {
            await _serviceCareBgJob.GenerateServiceScheduleAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Schedule generated successfully"
            });
        }

        /// <summary>
        /// [Manager/Staff] Lấy danh sách caretaker đủ điều kiện (đúng chuyên môn + không trùng lịch) cho đăng ký dịch vụ
        /// </summary>
        [HttpGet("{id}/eligible-caretakers")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetEligibleCaretakers(int id)
        {
            var managerId = GetUserId();
            var result = await _serviceRegistrationService.GetEligibleCaretakersForRegistrationAsync(managerId, id);
            return Ok(new ApiResponse<List<StaffWithSpecializationsResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get eligible caretakers successfully",
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
