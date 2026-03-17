using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlantDecor.API.Extensions;
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
        private readonly IBackgroundJobClient _backgroundJobClient;


        public AuthenticationController(IAuthenticationService authenticationService, IBackgroundJobClient backgroundJobClient)
        {
            _authenticationService = authenticationService;
            _backgroundJobClient = backgroundJobClient;
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

        [HttpPost("login-google")]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleAccessTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                throw new BadRequestException("Invalid Request");
            }
            var result = await _authenticationService.LoginWithGoogle(request);
            if (result == null)
            {
                throw new BadRequestException("Google authentication failed");
            }
            return Ok(new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Login Google Successfully!",
                Payload = result
            });
        }

        [HttpPost("refreshToken")]
        public async Task<IActionResult> RefreshTokenAsync(RefreshTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new BadRequestException("Refresh token is required");
            }

            var result = await _authenticationService.RefreshTokenAsync(request.RefreshToken);

            if (result == null)
            {
                throw new Exception("Failed to refresh token");
            }

            return StatusCode(StatusCodes.Status201Created, new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Refreshtoken successfully!",
                Payload = result
            });
        }

        [HttpPost("register")]
        //   [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> Register(UserRequest request)
        {
            var result = await _authenticationService.RegisterAsync(request);

            if (result == null)
            {
                throw new Exception("Registration failed");
            }

            // Enqueue verification email as background job (auto-retry on failure)
            _backgroundJobClient.EnqueueVerificationEmail(request.Email);

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
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Logged out from all devices successfully!"
            });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> Verify(ResendVerifyRequest request, CancellationToken cancellationToken)
        {
            var result = await _authenticationService.VerifyEmailAsync(request, cancellationToken);

            if (!result)
            {
                throw new BadRequestException("Failed to send verification email");
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Please check your mailbox to verify"
            });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _authenticationService.ConfirmEmailAsync(request);
            if (result == null)
            {
                throw new Exception("Internal Server Error!");
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Token is not valid or out of date"
            });
        }

        [HttpPut("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            var result = await _authenticationService.ResetPasswordAsync(request);
            if (result == null)
            {
                throw new Exception("Failed to reset password");
            }
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Password set successfully"
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
        {

            var result = await _authenticationService.ForgotPasswordAsync(request, cancellationToken);
            if (!result)
            {
                throw new BadRequestException("Email not found or not verified. Please verify your email first.");
            }
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Please check your mailbox to reset password"
            });
        }

    }
}
