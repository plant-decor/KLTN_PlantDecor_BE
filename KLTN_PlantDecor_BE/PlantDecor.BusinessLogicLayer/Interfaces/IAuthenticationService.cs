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
        //Task<AuthenticationResponse?> LogoutAsync(LogoutRequest request);
        //Task<AuthenticationResponse> LoginWithGoogle(GoogleAccessTokenRequest request);

        //Task<bool> VerifyEmailAsync(ResendVerifyRequest request, CancellationToken cancellationToken);
        //Task<AuthenticationResponse> ConfirmEmailAsync(ConfirmEmailRequest request);

        //Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
        //Task<AuthenticationResponse> ResetPasswordAsync(ResetPasswordRequest request);


    }
}
