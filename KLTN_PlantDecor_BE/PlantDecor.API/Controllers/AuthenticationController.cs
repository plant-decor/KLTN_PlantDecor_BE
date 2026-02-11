using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth-strict")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;


        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                throw new ArgumentException("Dữ liệu không hợp lệ");
            }

            var result = await _authenticationService.LoginAsync(request);

            return Ok(new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Đăng nhập thành công!",
                Payload = result
            });
        }
    }
}
