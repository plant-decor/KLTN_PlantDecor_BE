using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Extensions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Net.Mail;
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
        private readonly ISecurityStampCacheService _stampCacheService;
        private readonly IEmailService _emailService;


        public AuthenticationService(
            IHttpClientFactory httpClientFactory,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IEmailService emailService,
            ISecurityStampCacheService stampCacheService)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _stampCacheService = stampCacheService;
            _secretKey = configuration["JwtSettings:Key"] ?? throw new ArgumentNullException("JWT Key not configured");
            _issuer = configuration["JwtSettings:Issuer"] ?? throw new ArgumentNullException("JWT Issuer not configured");
            _audience = configuration["JwtSettings:Audience"] ?? throw new ArgumentNullException("JWT Audience not configured");
            _expiryMinutes = int.Parse(configuration["JwtSettings:ExpiryMinutes"] ?? "30");
            _emailService = emailService;
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
                throw new BadRequestException("Email and password must not be empty");
            }


            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            var isVerified = await _unitOfWork.UserRepository.IsVerifiedAsync(user.Id);
            if (!isVerified)
            {
                throw new UnauthorizedException("Account email has not been verified");
            }

            var isValidPassword = await _unitOfWork.UserRepository.VerifyPasswordAsync(user, request.Password);
            if (!isValidPassword)
            {
                throw new BadRequestException("Incorrect password");
            }
            if (user.Status != (int)UserStatusEnum.Active)
            {
                throw new ForbiddenException("Account is disabled");
            }

            var roleName = user.Role?.Name;
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ForbiddenException("User role not found");
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
                throw new Exception("Failed to update RefreshToken");
            }

            // Sau khi login thành công, trước khi return để optimize performance
            // Ở đây nó optimize vì mỗi request sẽ validate security stamp,
            // nếu cache miss sẽ query DB, nên set cache ngay sau khi login thành công để tránh cache miss ở request đầu tiên
            await _stampCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp);

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

        public async Task<AuthenticationResponse?> RegisterAsync(UserRequest request)
        {
            // Validate input Null or Empty
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                throw new BadRequestException("Email, password, username and full name are required");
            }

            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                throw new BadRequestException("Invalid email format");
            }

            // Validate phone format
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) &&
                !System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^(0|\+84)(\d{9})$"))
            {
                throw new BadRequestException("Invalid phone number format");
            }
            var phoneExists = await _unitOfWork.UserRepository.GetByPhoneAsync(request.PhoneNumber);
            if (phoneExists != null)
            {
                throw new BadRequestException("Phone number is already in use");
            }

            //Validate password complexity
            ValidatePassword(request.Password);


            // Validate password confirmation
            if (request.Password != request.ConfirmPassword)
            {
                throw new BadRequestException("Password and confirmation password do not match");
            }

            // Check if user already exists
            var existingUser = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new BadRequestException("This email is already registered");
            }

            // Check if role exists and Enum is valid
            var userRoleEnum = (int)RoleEnum.Customer;

            var role = await _unitOfWork.RoleRepository.GetByIdAsync(userRoleEnum);
            if (role == null)
            {
                throw new BadRequestException("Invalid role");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Create new user
                // UserMapper để chuyển từ DTO Request sang Entity nhưng chưa mã hóa password
                // Mã hóa password bằng BCrypt ở đây vì ở đây là tầng quản lý nghiệp vụ và có thể kiểm soát transaction 
                var newUser = UserMapper.ToEntity(request);
                newUser.RoleId = userRoleEnum;
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Tạo SecurityStamp mới cho user
                newUser.UpdateSecurityStamp();

                // PrepareCreate để tránh gọi SaveChanges nhiều lần
                // PrepareCreate chỉ thêm entity vào context chứ không lưu vào db ngay lập tức như CreateAsync
                _unitOfWork.UserRepository.PrepareCreate(newUser);
                await _unitOfWork.SaveAsync();

                // Get the created user to have the ID
                var createdUser = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
                if (createdUser == null)
                {
                    // Trường hợp lẽ ra không xảy ra nhưng vẫn kiểm tra để chắc chắn
                    // Nếu không lấy được user vừa tạo thì rollback transaction và trả về lỗi
                    throw new Exception("Failed to retrieve created user");
                }

                await _unitOfWork.CommitTransactionAsync();

                return new AuthenticationResponse
                {
                    User = UserMapper.ToResponse(createdUser)
                };

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        /*
        "test@example.com"  // true
        "user@domain"       // vẫn true
        "user@@domain.com"  // false (2 ký tự @)
        "user@domain.com "  // false (có space cuối)
        ""                  // false
        */
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (password.Length < 8)
            {
                errors.Add("Password must be at least 8 characters.");
            }

            if (!password.Any(char.IsUpper))
            {
                errors.Add("Password must contain at least one uppercase letter.");
            }

            if (!password.Any(char.IsLower))
            {
                errors.Add("Password must contain at least one lowercase letter.");
            }

            if (!password.Any(char.IsDigit))
            {
                errors.Add("Password must contain at least one digit.");
            }

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
            {
                errors.Add("Password must contain at least one special character.");
            }

            if (errors.Count > 0)
            {
                throw new BadRequestException(string.Join(" ", errors));
            }
        }

        public async Task<AuthenticationResponse?> CreateManagerAsync(CreateManagerRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                throw new BadRequestException("Email, password, username and full name are required");
            }

            if (!IsValidEmail(request.Email))
            {
                throw new BadRequestException("Invalid email format");
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) &&
                !System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^(0|\+84)(\d{9})$"))
            {
                throw new BadRequestException("Invalid phone number format");
            }

            var phoneExists = await _unitOfWork.UserRepository.GetByPhoneAsync(request.PhoneNumber);
            if (phoneExists != null)
            {
                throw new BadRequestException("Phone number is already in use");
            }

            ValidatePassword(request.Password);

            if (request.Password != request.ConfirmPassword)
            {
                throw new BadRequestException("Password and confirmation password do not match");
            }

            var existingUser = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new BadRequestException("This email is already registered");
            }

            var managerRoleId = (int)RoleEnum.Manager;
            var role = await _unitOfWork.RoleRepository.GetByIdAsync(managerRoleId);
            if (role == null)
            {
                throw new BadRequestException("Manager role not found");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var userRequest = new UserRequest
                {
                    Email = request.Email,
                    Password = request.Password,
                    ConfirmPassword = request.ConfirmPassword,
                    Username = request.Username,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber
                };

                var newUser = UserMapper.ToEntity(userRequest);
                newUser.RoleId = managerRoleId;
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                newUser.UpdateSecurityStamp();

                _unitOfWork.UserRepository.PrepareCreate(newUser);
                await _unitOfWork.SaveAsync();

                var createdUser = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
                if (createdUser == null)
                {
                    throw new Exception("Failed to retrieve created manager account");
                }

                await _unitOfWork.CommitTransactionAsync();

                return new AuthenticationResponse
                {
                    User = UserMapper.ToResponse(createdUser)
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public Task<ClaimsPrincipal?> ValidateToken(string token)
        {
            throw new NotImplementedException();
        }
    }
}
