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
    /// API quản lý Tag (Admin)
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagsController(ITagService tagService)
        {
            _tagService = tagService;
        }

        /// <summary>
        /// Lấy tất cả tags
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTags()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return Ok(new ApiResponse<IEnumerable<TagResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách tags thành công",
                Payload = tags
            });
        }

        /// <summary>
        /// Lấy tag theo ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTagById(int id)
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Tag với ID {id} không tồn tại"
                });

            return Ok(new ApiResponse<TagResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy tag thành công",
                Payload = tag
            });
        }

        /// <summary>
        /// Tạo tag mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] TagRequestDto request)
        {
            var tag = await _tagService.CreateTagAsync(request);
            return CreatedAtAction(nameof(GetTagById), new { id = tag.Id }, new ApiResponse<TagResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo tag thành công",
                Payload = tag
            });
        }

        /// <summary>
        /// Cập nhật tag
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] TagUpdateDto request)
        {
            var tag = await _tagService.UpdateTagAsync(id, request);
            return Ok(new ApiResponse<TagResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật tag thành công",
                Payload = tag
            });
        }

        /// <summary>
        /// Xóa tag
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            await _tagService.DeleteTagAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa tag thành công"
            });
        }
    }
}
