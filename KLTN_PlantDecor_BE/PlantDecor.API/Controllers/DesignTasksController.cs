using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý công việc thiết kế
    /// </summary>
    [Route("api/design-tasks")]
    [ApiController]
    [Authorize]
    public class DesignTasksController : ControllerBase
    {
        private readonly IDesignTaskService _designTaskService;

        public DesignTasksController(IDesignTaskService designTaskService)
        {
            _designTaskService = designTaskService;
        }

        /// <summary>
        /// [Customer/Staff/Caretaker/Manager] Lấy chi tiết task thiết kế
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var result = await _designTaskService.GetByIdAsync(id, userId);
            return Ok(new ApiResponse<DesignTaskResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design task successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer/Staff/Caretaker/Manager] Lấy danh sách task theo đăng ký thiết kế
        /// </summary>
        [HttpGet("by-registration/{registrationId}")]
        public async Task<IActionResult> GetByRegistrationId(int registrationId)
        {
            var userId = GetUserId();
            var result = await _designTaskService.GetByRegistrationIdAsync(registrationId, userId);
            return Ok(new ApiResponse<List<DesignTaskResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design tasks successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Staff/Caretaker] Lấy danh sách task được giao (hỗ trợ lọc theo khoảng ngày ScheduledDate)
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Staff,Caretaker")]
        public async Task<IActionResult> GetMyTasks(
            [FromQuery] Pagination pagination,
            [FromQuery] int? status = null,
            [FromQuery] DateOnly? from = null,
            [FromQuery] DateOnly? to = null)
        {
            var userId = GetUserId();
            var result = await _designTaskService.GetMyTasksAsync(userId, pagination, status, from, to);
            return Ok(new ApiResponse<PaginatedResult<DesignTaskResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design tasks successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Giao nhân sự thực hiện task thiết kế
        /// </summary>
        [HttpPut("{id}/assign")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> AssignTask(int id, [FromBody] AssignDesignTaskRequestDto request)
        {
            var managerId = GetUserId();
            var result = await _designTaskService.AssignTaskAsync(managerId, id, request);
            return Ok(new ApiResponse<DesignTaskResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design task assigned successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Staff/Caretaker] Lấy danh sách vật tư đề xuất theo gói của task để báo cáo đúng vật tư
        /// </summary>
        [HttpGet("{id}/package-materials")]
        [Authorize(Roles = "Staff,Caretaker")]
        public async Task<IActionResult> GetPackageMaterials(int id)
        {
            var userId = GetUserId();
            var result = await _designTaskService.GetPackageMaterialsForTaskAsync(userId, id);
            return Ok(new ApiResponse<List<DesignTaskPackageMaterialResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get package materials successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Staff/Caretaker] Báo cáo vật tư đã sử dụng cho buổi làm và trừ kho ngay (không cần duyệt)
        /// </summary>
        [HttpPost("{id}/material-usage")]
        [Authorize(Roles = "Staff,Caretaker")]
        public async Task<IActionResult> ReportMaterialUsage(int id, [FromBody] ReportDesignTaskMaterialUsageRequestDto request)
        {
            var userId = GetUserId();
            var result = await _designTaskService.ReportMaterialUsageAsync(userId, id, request);
            return Ok(new ApiResponse<DesignTaskResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Material usage reported successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Staff/Caretaker] Hoàn thành task thiết kế
        /// </summary>
        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Staff,Caretaker")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CompleteTask(int id, IFormFile reportImage)
        {
            var userId = GetUserId();
            var result = await _designTaskService.UpdateStatusAsync(userId, id, new UpdateDesignTaskStatusRequestDto
            {
                Status = (int)DesignTaskStatusEnum.Completed
            }, reportImage);

            return Ok(new ApiResponse<DesignTaskResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design task completed successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager/Staff] Hủy task thiết kế
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> CancelTask(int id)
        {
            var managerId = GetUserId();
            var result = await _designTaskService.UpdateStatusAsync(managerId, id, new UpdateDesignTaskStatusRequestDto
            {
                Status = (int)DesignTaskStatusEnum.Cancelled
            });

            return Ok(new ApiResponse<DesignTaskResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Design task cancelled successfully",
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
