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
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Tạo URL thanh toán VNPay cho một đơn hàng
        /// </summary>
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentRequestDto request)
        {
            var userId = GetUserId();
            var result = await _paymentService.CreatePaymentUrlAsync(userId, request, HttpContext);
            return Ok(new ApiResponse<CreatePaymentUrlResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Payment URL created successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Callback từ VNPay sau khi thanh toán - chỉ để redirect user về app (không cập nhật DB)
        /// </summary>
        [HttpGet("Checkout/PaymentCallbackVnpay")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var result = await _paymentService.ProcessVnpayCallbackAsync(Request.Query);

            if (!result.Success)
            {
                return Ok(new ApiResponse<PaymentResponse>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Payment failed or invalid signature",
                    Payload = result
                });
            }

            return Ok(new ApiResponse<PaymentResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Payment processed successfully",
                Payload = result
            });
        }

        /// <summary>
        /// IPN từ VNPay server - cập nhật DB sau khi thanh toán (server-to-server)
        /// </summary>
        [HttpGet("Checkout/IpnVnpay")]
        [AllowAnonymous]
        public async Task<IActionResult> IpnVnpay()
        {
            var result = await _paymentService.ProcessVnpayIpnAsync(Request.Query);
            return Ok(result);
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
