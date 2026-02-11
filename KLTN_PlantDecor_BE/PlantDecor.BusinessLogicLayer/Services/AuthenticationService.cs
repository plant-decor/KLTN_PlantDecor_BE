using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Exceptions;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpClientFactory _httpClientFactory;
        // private readonly IEmailService _emailService;


        public AuthenticationService(
            IHttpClientFactory httpClientFactory,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _secretKey = configuration["JwtSettings:Key"] ?? throw new ArgumentNullException("JWT Key not configured");
            _issuer = configuration["JwtSettings:Issuer"] ?? throw new ArgumentNullException("JWT Issuer not configured");
            _audience = configuration["JwtSettings:Audience"] ?? throw new ArgumentNullException("JWT Audience not configured");
            _expiryMinutes = int.Parse(configuration["JwtSettings:ExpiryMinutes"] ?? "30");
            //  _emailService = emailService;
        }

        public string GenerateAccessToken(User user, string roleName)
        {
            var secretKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_secretKey));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
            var email = user.Email;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub , user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Name, user.Username),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    new Claim("Role", roleName),
                    new Claim ("avatarURL", user.AvatarUrl ?? string.Empty),
                    // SecurityStamp trong AccessToken để validate mỗi request
                    new Claim("SecurityStamp", user.SecurityStamp)

                }),
                Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = signingCredentials
            };

            var handler = new JsonWebTokenHandler();
            var token = handler.CreateToken(tokenDescriptor);
            return token;
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        public async Task<AuthenticationResponse?> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Email và mật khẩu không được để trống");
            }


            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new KeyNotFoundException("Người dùng không tồn tại");
            }

            var isVerified = await _unitOfWork.UserRepository.IsVerifiedAsync(user.Id);
            if (!isVerified)
            {
                throw new UnauthorizedAccessException("Tài khoản chưa được xác thực email");
            }

            var isValidPassword = await _unitOfWork.UserRepository.VerifyPasswordAsync(user, request.Password);
            if (!isValidPassword)
            {
                throw new ArgumentException("Mật khẩu không đúng");
            }
            if (user.Status != (int)UserStatusEnum.Active)
            {
                throw new ForbiddenAccessException("Tài khoản đang bị vô hiệu hóa");
            }

            var roleName = user.Role?.Name;
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ForbiddenAccessException("Không tìm thấy vai trò của người dùng");
            }

            var accessToken = GenerateAccessToken(user, roleName);
            var refreshToken = GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(30);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Revoke tất cả refresh token cũ của user
            var oldTokens = await _unitOfWork.UserRepository.GetRefreshTokenAsync(user.Id);
            foreach (var token in oldTokens)
            {
                token.IsRevoked = true;
            }

            // Tạo refresh token mới
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                IsRevoked = false,
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = refreshTokenExpiry
            };

            user.RefreshTokens.Add(newRefreshToken);
            var success = await _unitOfWork.SaveAsync();

            if (success == 0)
            {
                throw new Exception("Cập nhật RefreshToken thất bại");
            }

            return new AuthenticationResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public Task<AuthenticationResponse?> RefreshTokenAsync(string refreshToken)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationResponse?> RegisterAsync(UserRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<ClaimsPrincipal?> ValidateToken(string token)
        {
            throw new NotImplementedException();
        }
    }
}
