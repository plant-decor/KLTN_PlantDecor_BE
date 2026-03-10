using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
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
                throw new BadRequestException("Invalid Request");
            }

            var result = await _authenticationService.LoginAsync(request);

            return Ok(new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Login Successfully!",
                Payload = result
            });
        }

        [HttpPost("register")]
        //   [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> Register(UserRequest request)
        {
            //if (!ModelState.IsValid)
            //{
            //    throw new BadRequestException("Invalid Request");
            //}

            var result = await _authenticationService.RegisterAsync(request);

            if (result == null)
            {
                throw new Exception("Registration failed");
            }

            // Send Verification Email (Optional, can be triggered by user action instead)
            //var verifyRequest = new ResendVerifyRequest() { Email = request.Email };

            //var emailSent = await _authenticationService.VerifyEmailAsync(verifyRequest, CancellationToken.None);
            //if (!emailSent)
            //{
            //    throw new Exception("Failed to send verification email");
            //}

            //return CreatedAtAction(
            //            nameof(Register),   // Action name
            //            new { email = request.Email },  // route values
            //            result  // response body
            //    );
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Register Successfully! Please verify your email to use full services.",
                Payload = result
            });


        }

        [HttpPost("create-manager")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateManagerAccount([FromBody] CreateManagerRequest request)
        {
            if (!ModelState.IsValid)
            {
                throw new BadRequestException("Invalid request");
            }

            var result = await _authenticationService.CreateManagerAsync(request);

            if (result == null)
            {
                throw new Exception("Failed to create manager account");
            }

            return StatusCode(StatusCodes.Status201Created, new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Manager account created successfully!",
                Payload = result
            });
        }

        [HttpPost("logout")]
        //[Authorize]
        public async Task<IActionResult> Logout(LogoutRequest request)
        {

            // Nếu request body trống, lấy access token từ Authorization header
            if (string.IsNullOrWhiteSpace(request.AccessToken) && string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    request.AccessToken = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            // Invalidate security stamp to revoke tokens
            await _authenticationService.LogoutAsync(request);
            return Ok(new ApiResponse<string>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Logged out successfully!"
            });
        }

        [HttpPost("logout-all")]
        //[Authorize]
        public async Task<IActionResult> LogoutAll(LogoutRequest request)
        {
            // Nếu request body trống, lấy access token từ Authorization header
            if (string.IsNullOrWhiteSpace(request.AccessToken) && string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    request.AccessToken = authHeader.Substring("Bearer ".Length).Trim();
                }
            }
            // Invalidate security stamp to revoke all tokens
            await _authenticationService.LogoutAllAsync(request);
            return Ok(new ApiResponse<string>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Logged out from all devices successfully!"
            });
        }

    }
}
