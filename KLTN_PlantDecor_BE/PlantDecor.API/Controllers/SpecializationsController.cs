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
    /// API quản lý Specialization (chuyên môn nhân viên)
    /// </summary>
    [ApiController]
    public class SpecializationsController : ControllerBase
    {
        private readonly ISpecializationService _specializationService;

        public SpecializationsController(ISpecializationService specializationService)
        {
            _specializationService = specializationService;
        }

        // ─── Public ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy danh sách chuyên môn đang hoạt động (public)
        /// </summary>
        [HttpGet("api/specializations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllActive()
        {
            var result = await _specializationService.GetAllActiveAsync();
            return Ok(new ApiResponse<List<SpecializationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get active specializations successfully",
                Payload = result
            });
        }

        // ─── Admin ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy tất cả chuyên môn (Admin)
        /// </summary>
        [HttpGet("api/admin/specializations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _specializationService.GetAllAsync();
            return Ok(new ApiResponse<List<SpecializationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get all specializations successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy chuyên môn theo ID (Admin)
        /// </summary>
        [HttpGet("api/admin/specializations/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _specializationService.GetByIdAsync(id);
            return Ok(new ApiResponse<SpecializationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get specialization successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Tạo chuyên môn mới (Admin)
        /// </summary>
        [HttpPost("api/admin/specializations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] SpecializationRequestDto request)
        {
            var result = await _specializationService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<SpecializationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Specialization created successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Cập nhật chuyên môn (Admin)
        /// </summary>
        [HttpPut("api/admin/specializations/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSpecializationRequestDto request)
        {
            var result = await _specializationService.UpdateAsync(id, request);
            return Ok(new ApiResponse<SpecializationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Specialization updated successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Xoá mềm chuyên môn (Admin)
        /// </summary>
        [HttpDelete("api/admin/specializations/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _specializationService.DeleteAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Specialization deleted successfully",
                Payload = null
            });
        }

        // ─── Manager – Staff Specializations ───────────────────────────────────────

        /// <summary>
        /// Gán chuyên môn cho nhân viên trong nursery của manager
        /// </summary>
        [HttpPost("api/manager/nurseries/my-nursery/staff/{staffId:int}/specializations")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> AssignToStaff(int staffId, [FromBody] AssignStaffSpecializationDto request)
        {
            var managerId = GetCurrentUserId();
            var result = await _specializationService.AssignToStaffAsync(managerId, staffId, request.SpecializationId);
            return Ok(new ApiResponse<StaffWithSpecializationsResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Specialization assigned to staff successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Xoá chuyên môn khỏi nhân viên trong nursery của manager
        /// </summary>
        [HttpDelete("api/manager/nurseries/my-nursery/staff/{staffId:int}/specializations/{specializationId:int}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> RemoveFromStaff(int staffId, int specializationId)
        {
            var managerId = GetCurrentUserId();
            var result = await _specializationService.RemoveFromStaffAsync(managerId, staffId, specializationId);
            return Ok(new ApiResponse<StaffWithSpecializationsResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Specialization removed from staff successfully",
                Payload = result
            });
        }

        // ─── Manager – Package eligible caretakers ─────────────────────────────────

        /// <summary>
        /// Lấy danh sách caretaker đủ điều kiện cho gói dịch vụ (có đủ chuyên môn yêu cầu)
        /// </summary>
        [HttpGet("api/nursery-care-services/my/caretakers-by-spec/{packageId:int}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetEligibleCaretakers(int packageId)
        {
            var managerId = GetCurrentUserId();
            var result = await _specializationService.GetEligibleCaretakersForPackageAsync(managerId, packageId);
            return Ok(new ApiResponse<List<StaffWithSpecializationsResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get eligible caretakers successfully",
                Payload = result
            });
        }

        #region private method
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Unable to identify user from token");
            return userId;
        }
        #endregion
    }
}
