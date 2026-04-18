using Microsoft.AspNetCore.Authorization;
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
    [Route("api/return-tickets")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class ReturnTicketsController : ControllerBase
    {
        private readonly IReturnTicketService _returnTicketService;

        public ReturnTicketsController(IReturnTicketService returnTicketService)
        {
            _returnTicketService = returnTicketService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReturnTicketRequestDto request)
        {
            var customerId = GetUserId();
            var result = await _returnTicketService.CreateReturnTicketAsync(customerId, request);

            return StatusCode(StatusCodes.Status201Created, new ApiResponse<ReturnTicketResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Return ticket created successfully",
                Payload = result
            });
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyTickets()
        {
            var customerId = GetUserId();
            var result = await _returnTicketService.GetMyReturnTicketsAsync(customerId);

            return Ok(new ApiResponse<List<ReturnTicketResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get return tickets successfully",
                Payload = result
            });
        }

        [HttpPost("{ticketId:int}/items/{itemId:int}/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadItemImages(int ticketId, int itemId, List<IFormFile> files)
        {
            var customerId = GetUserId();
            var result = await _returnTicketService.UploadReturnTicketItemImagesAsync(customerId, ticketId, itemId, files);

            return Ok(new ApiResponse<ReturnTicketItemResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Upload return item images successfully",
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
