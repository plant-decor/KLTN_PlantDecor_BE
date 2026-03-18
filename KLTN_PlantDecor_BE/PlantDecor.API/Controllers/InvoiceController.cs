using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        /// <summary>
        /// Lấy chi tiết một hóa đơn theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoiceById(int id)
        {
            var userId = GetUserId();
            var result = await _invoiceService.GetInvoiceByIdAsync(id, userId);
            return Ok(new ApiResponse<InvoiceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get invoice successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Lấy danh sách tất cả hóa đơn của một đơn hàng
        /// </summary>
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetInvoicesByOrderId(int orderId)
        {
            var userId = GetUserId();
            var result = await _invoiceService.GetInvoicesByOrderIdAsync(orderId, userId);
            return Ok(new ApiResponse<List<InvoiceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get invoices successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Manager] Tạo hóa đơn thanh toán phần còn lại cho đơn hàng Deposit
        /// </summary>
        [HttpPost("order/{orderId}/remaining")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GenerateRemainingInvoice(int orderId)
        {
            var result = await _invoiceService.GenerateRemainingInvoiceAsync(orderId);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<InvoiceResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Remaining invoice generated successfully",
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
