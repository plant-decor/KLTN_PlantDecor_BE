using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;
using System.Security.Claims;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IAuthenticationService
    {
        string GenerateAccessToken(User user, string roleName);
        string GenerateRefreshToken();
        Task<ClaimsPrincipal?> ValidateToken(string token);

        Task<AuthenticationResponse?> LoginAsync(LoginRequest request);
        Task<AuthenticationResponse?> RefreshTokenAsync(string refreshToken);
        Task<AuthenticationResponse?> RegisterAsync(UserRequest request);
        Task<AuthenticationResponse?> CreateManagerAsync(CreateManagerRequest request);
        Task<AuthenticationResponse?> CreateStaffAsync(int managerId, CreateStaffRequest request);
        Task<AuthenticationResponse?> LogoutAsync(LogoutRequest request);
        Task<AuthenticationResponse?> LogoutAllAsync(LogoutRequest request);
        Task<AuthenticationResponse> LoginWithGoogle(GoogleAccessTokenRequest request);
        Task<bool> VerifyEmailAsync(ResendVerifyRequest request, CancellationToken cancellationToken);
        Task<AuthenticationResponse> ConfirmEmailAsync(ConfirmEmailRequest request);

        Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
        Task<AuthenticationResponse> ResetPasswordAsync(ResetPasswordRequest request);

        // OTP methods for Email Verification
        Task<OtpResponse> SendOtpEmailVerificationAsync(SendOtpEmailVerificationRequest request, CancellationToken cancellationToken);
        Task<OtpResponse> VerifyOtpEmailVerificationAsync(VerifyOtpEmailVerificationRequest request);

        // OTP methods for Password Reset
        Task<OtpResponse> SendOtpPasswordResetAsync(SendOtpPasswordResetRequest request, CancellationToken cancellationToken);
        Task<OtpResponse> VerifyOtpPasswordResetAsync(VerifyOtpPasswordResetRequest request);
        Task<AuthenticationResponse> ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request);


    }
}
