using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Extensions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Libraries;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

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
        private readonly IOtpCacheService _otpCacheService;


        public AuthenticationService(
            IHttpClientFactory httpClientFactory,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IEmailService emailService,
            ISecurityStampCacheService stampCacheService,
            IOtpCacheService otpCacheService)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _stampCacheService = stampCacheService;
            _emailService = emailService;
            _otpCacheService = otpCacheService;
            _secretKey = configuration["JwtSettings:Key"] ?? throw new ArgumentNullException("JWT Key not configured");
            _issuer = configuration["JwtSettings:Issuer"] ?? throw new ArgumentNullException("JWT Issuer not configured");
            _audience = configuration["JwtSettings:Audience"] ?? throw new ArgumentNullException("JWT Audience not configured");
            _expiryMinutes = int.Parse(configuration["JwtSettings:ExpiryMinutes"] ?? "30");
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

            if (string.IsNullOrEmpty(request.DeviceId))
            {
                throw new BadRequestException("DeviceId is required for login");
            }


            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            //var isVerified = await _unitOfWork.UserRepository.IsVerifiedAsync(user.Id);
            if (!user.IsVerified)
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
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Revoke tất cả refresh token cũ của user
            if (!string.IsNullOrWhiteSpace(request.DeviceId))
            {
                var oldDeviceTokens = await _unitOfWork.UserRepository.GetOldRefreshTokenByDeviceIdAsync(user.Id, request.DeviceId);
                if (oldDeviceTokens != null)
                {
                    foreach (var token in oldDeviceTokens)
                    {
                        token.IsRevoked = true;
                    }
                }
            }

            // Tạo refresh token mới
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                IsRevoked = false,
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = refreshTokenExpiry,
                DeviceId = request.DeviceId
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

        public async Task<AuthenticationResponse?> RefreshTokenAsync(string refreshToken)
        {
            // 1. Tìm user theo refresh token (chỉ trả về nếu token chưa bị revoke và user Active)
            var user = await _unitOfWork.UserRepository.GetByRefreshTokenAsync(refreshToken);
            if (user == null)
            {
                throw new UnauthorizedException("Invalid or revoked refresh token");
            }

            // 2. Lấy thông tin refresh token để kiểm tra hạn
            var existingToken = await _unitOfWork.UserRepository.GetRefreshTokenByRefreshTokenAsync(user.Id, refreshToken);
            if (existingToken == null || existingToken.ExpiryDate < DateTime.UtcNow)
            {
                throw new UnauthorizedException("Refresh token has expired");
            }

            // 3. Kiểm tra trạng thái tài khoản
            if (user.Status != (int)UserStatusEnum.Active)
            {
                throw new ForbiddenException("Account is disabled");
            }

            // 4. Lấy role name
            var roleName = user.Role?.Name;
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ForbiddenException("User role not found");
            }

            // 5. Token Rotation: revoke token cũ
            existingToken.IsRevoked = true;

            // 6. Tạo cặp token mới
            var newAccessToken = GenerateAccessToken(user, roleName);
            var newRefreshToken = GenerateRefreshToken();

            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                IsRevoked = false,
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                DeviceId = existingToken.DeviceId  // Giữ nguyên DeviceId từ token cũ
            };

            user.RefreshTokens.Add(newRefreshTokenEntity);
            var success = await _unitOfWork.SaveAsync();

            if (success == 0)
            {
                throw new Exception("Failed to save new refresh token");
            }

            // 7. Cập nhật cache security stamp để tránh cache miss ở request tiếp theo
            await _stampCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp);

            return new AuthenticationResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
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
            catch (Exception)
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

            Nursery? nursery = null;
            if (request.NurseryId.HasValue)
            {
                nursery = await _unitOfWork.NurseryRepository.GetByIdAsync(request.NurseryId.Value);
                if (nursery == null)
                    throw new NotFoundException($"Nursery with ID {request.NurseryId.Value} not found");

                if (nursery.ManagerId.HasValue)
                    throw new BadRequestException("Nursery this has already been assigned to another Manager");
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
                newUser.IsVerified = true; // Manager được tạo bởi Admin nên mặc định là đã verified
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                newUser.UpdateSecurityStamp();

                _unitOfWork.UserRepository.PrepareCreate(newUser);
                await _unitOfWork.SaveAsync();

                if (newUser.Id <= 0)
                {
                    throw new Exception("Failed to retrieve created manager account");
                }

                if (nursery != null)
                {
                    nursery.ManagerId = newUser.Id;
                    _unitOfWork.NurseryRepository.PrepareUpdate(nursery);
                    await _unitOfWork.SaveAsync();
                }

                await _unitOfWork.CommitTransactionAsync();

                return new AuthenticationResponse
                {
                    User = UserMapper.ToResponse(newUser)
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<AuthenticationResponse?> CreateStaffAsync(int managerId, CreateStaffRequest request)
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

            // Kiểm tra manager tồn tại và có nursery
            var manager = await _unitOfWork.UserRepository.GetByIdAsync(managerId);
            if (manager == null)
            {
                throw new NotFoundException("Manager not found");
            }

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
            {
                throw new BadRequestException("Manager does not have an assigned nursery");
            }

            var staffRoleId = (int)RoleEnum.Staff;
            var role = await _unitOfWork.RoleRepository.GetByIdAsync(staffRoleId);
            if (role == null)
            {
                throw new BadRequestException("Staff role not found");
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
                newUser.RoleId = staffRoleId;
                newUser.NurseryId = nursery.Id;
                newUser.IsVerified = true; // Staff được tạo bởi Manager nên mặc định là đã verified
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                newUser.UpdateSecurityStamp();

                _unitOfWork.UserRepository.PrepareCreate(newUser);
                await _unitOfWork.SaveAsync();

                var createdUser = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
                if (createdUser == null)
                {
                    throw new Exception("Failed to retrieve created staff account");
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

        public async Task<AuthenticationResponse?> CreateShipperAsync(int managerId, CreateStaffRequest request)
        {
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

            var manager = await _unitOfWork.UserRepository.GetByIdAsync(managerId);
            if (manager == null)
            {
                throw new NotFoundException("Manager not found");
            }

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
            {
                throw new BadRequestException("Manager does not have an assigned nursery");
            }

            var shipperRoleId = (int)RoleEnum.Shipper;
            var role = await _unitOfWork.RoleRepository.GetByIdAsync(shipperRoleId);
            if (role == null)
            {
                throw new BadRequestException("Shipper role not found");
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
                newUser.RoleId = shipperRoleId;
                newUser.NurseryId = nursery.Id;
                newUser.IsVerified = true;
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                newUser.UpdateSecurityStamp();

                _unitOfWork.UserRepository.PrepareCreate(newUser);
                await _unitOfWork.SaveAsync();

                var createdUser = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
                if (createdUser == null)
                {
                    throw new Exception("Failed to retrieve created shipper account");
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

        public async Task<AuthenticationResponse?> CreateConsultantAsync(CreateStaffRequest request)
        {
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

            var consultantRoleId = (int)RoleEnum.Consultant;
            var role = await _unitOfWork.RoleRepository.GetByIdAsync(consultantRoleId);
            if (role == null)
            {
                throw new BadRequestException("Consultant role not found");
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
                newUser.RoleId = consultantRoleId;
                newUser.IsVerified = true;
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                newUser.UpdateSecurityStamp();

                _unitOfWork.UserRepository.PrepareCreate(newUser);
                await _unitOfWork.SaveAsync();

                var createdUser = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
                if (createdUser == null)
                {
                    throw new Exception("Failed to retrieve created consultant account");
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

        public async Task<StaffWithSpecializationsResponseDto> CreateCaretakerAsync(int managerId, CreateCaretakerWithSpecializationsRequestDto request)
        {
            if (request == null)
                throw new BadRequestException("Request is required");

            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                throw new BadRequestException("Email, password, username and full name are required");
            }

            if (!IsValidEmail(request.Email))
                throw new BadRequestException("Invalid email format");

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) &&
                !System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^(0|\+84)(\d{9})$"))
            {
                throw new BadRequestException("Invalid phone number format");
            }

            if (request.Password != request.ConfirmPassword)
                throw new BadRequestException("Password and confirmation password do not match");

            ValidatePassword(request.Password);

            if (request.SpecializationIds == null || request.SpecializationIds.Count == 0)
                throw new BadRequestException("At least one specialization is required");

            var manager = await _unitOfWork.UserRepository.GetByIdAsync(managerId);
            if (manager == null)
                throw new NotFoundException("Manager not found");

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new BadRequestException("Manager does not have an assigned nursery");

            var existingUser = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
                throw new BadRequestException("This email is already registered");

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var phoneExists = await _unitOfWork.UserRepository.GetByPhoneAsync(request.PhoneNumber);
                if (phoneExists != null)
                    throw new BadRequestException("Phone number is already in use");
            }

            var caretakerRoleId = (int)RoleEnum.Caretaker;
            var role = await _unitOfWork.RoleRepository.GetByIdAsync(caretakerRoleId);
            if (role == null)
                throw new BadRequestException("Caretaker role not found");

            var specializationIds = request.SpecializationIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (specializationIds.Count == 0)
                throw new BadRequestException("At least one valid specialization is required");

            foreach (var specializationId in specializationIds)
            {
                var specialization = await _unitOfWork.SpecializationRepository.GetByIdAsync(specializationId);
                if (specialization == null)
                    throw new NotFoundException($"Specialization {specializationId} not found");
                if (!specialization.IsActive)
                    throw new BadRequestException($"Specialization {specializationId} is not active");
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
                newUser.RoleId = caretakerRoleId;
                newUser.NurseryId = nursery.Id;
                newUser.IsVerified = true;
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                newUser.UpdateSecurityStamp();

                _unitOfWork.UserRepository.PrepareCreate(newUser);
                await _unitOfWork.SaveAsync();

                if (newUser.Id <= 0)
                    throw new Exception("Failed to create caretaker account");

                await _unitOfWork.SpecializationRepository.ReplaceStaffSpecializationsAsync(newUser.Id, specializationIds);
                await _unitOfWork.SaveAsync();

                var created = await _unitOfWork.UserRepository.GetCaretakerByIdWithSpecializationsAsync(newUser.Id, nursery.Id);
                if (created == null)
                    throw new Exception("Failed to load created caretaker with specializations");

                await _unitOfWork.CommitTransactionAsync();
                return MapCaretakerWithSpecializations(created);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private static StaffWithSpecializationsResponseDto MapCaretakerWithSpecializations(User user)
        {
            return new StaffWithSpecializationsResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                Specializations = user.StaffSpecializations
                    .Select(ss => new SpecializationSummaryDto
                    {
                        Id = ss.Specialization.Id,
                        Name = ss.Specialization.Name,
                        Description = ss.Specialization.Description
                    })
                    .ToList()
            };
        }

        public async Task<ClaimsPrincipal?> ValidateToken(string token)
        {
            var tokenHandler = new JsonWebTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // hàm logout cho phép token hết hạn
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };
            //trả về TokenValidationResult
            //TokenValidationResult có các thông tin quan trọng như IsValid, ClaimsIdentity, Exception, SecurityToken
            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            // Nếu token hợp lệ, trả về ClaimsPrincipal
            //IsValid: xác định xem token có hợp lệ hay không
            //ClaimsIdentity: chứa các thông tin về người dùng được mã hóa trong token
            if (result.IsValid && result.ClaimsIdentity != null)
            {
                return new ClaimsPrincipal(result.ClaimsIdentity);
            }

            return null;
        }

        public async Task<AuthenticationResponse?> LogoutAsync(LogoutRequest request)
        {
            // Validate input - ít nhất một trong hai token phải có
            if (string.IsNullOrWhiteSpace(request.AccessToken) && string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new BadRequestException("Access token or refresh token must be provided");
            }

            User? user = null;


            // Nếu có refresh token, tìm user bằng refresh token trước
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                user = await _unitOfWork.UserRepository.GetByRefreshTokenAsync(request.RefreshToken);
            }

            // Nếu không tìm thấy user bằng refresh token và có access token
            if (user == null && !string.IsNullOrWhiteSpace(request.AccessToken))
            {
                // Validate access token và lấy user ID từ claims
                var principal = await ValidateToken(request.AccessToken);
                if (principal != null)
                {
                    // lấy ra ID user từ claim "sub"
                    var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                    }
                }
                else
                {
                    throw new BadRequestException("Invalid access token");
                }
            }

            // Nếu không tìm thấy user
            if (user == null)
            {
                throw new BadRequestException("Invalid token or user not found");
            }


            // Chỉ revoke đúng token được gửi lên
            // không cần xóa cache vì nếu xóa thì đằng nào cũng phải lấy từ DB lên để validate

            if (!string.IsNullOrWhiteSpace(request.DeviceId))
            {
                var deviceRefreshToken = await _unitOfWork.UserRepository.GetRefreshTokenByDeviceIdAsync(user.Id, request.DeviceId);
                if (deviceRefreshToken != null)
                    deviceRefreshToken.IsRevoked = true;
            }
            else if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                // fallback: revoke đúng token được gửi lên nếu không có DeviceId
                var refreshTokenToRevoke = await _unitOfWork.UserRepository.GetRefreshTokenByRefreshTokenAsync(user.Id, request.RefreshToken);
                if (refreshTokenToRevoke != null)
                    refreshTokenToRevoke.IsRevoked = true;
            }


            // Update user trong database
            var updateResult = await _unitOfWork.UserRepository.UpdateAsync(user);
            if (updateResult == 0)
            {
                throw new Exception("Failed to update user during logout");
            }

            return new AuthenticationResponse
            {
            };

        }

        public async Task<AuthenticationResponse?> LogoutAllAsync(LogoutRequest request)
        {
            // Validate input - ít nhất một trong hai token phải có
            if (string.IsNullOrWhiteSpace(request.AccessToken) && string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new BadRequestException("Access token or refresh token must be provided");
            }

            User? user = null;

            // Nếu có refresh token, tìm user bằng refresh token trước
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                user = await _unitOfWork.UserRepository.GetByRefreshTokenAsync(request.RefreshToken);
            }

            // Nếu không tìm thấy user bằng refresh token và có access token
            if (user == null && !string.IsNullOrWhiteSpace(request.AccessToken))
            {
                // Validate access token và lấy user ID từ claims
                var principal = await ValidateToken(request.AccessToken);
                if (principal != null)
                {
                    // lấy ra ID user từ claim "sub"
                    var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                    }
                }
                else
                {
                    throw new BadRequestException("Invalid access token");
                }
            }

            // Nếu không tìm thấy user
            if (user == null)
            {
                throw new BadRequestException("Invalid token or user not found");
            }


            // Clear refresh token và expiry date
            // Revoke tất cả refresh token cũ của user
            // Trong hàm InvalidateAllTokensAsync đã có hàm revoke all token cũ rồi nên ở đây không cần revoke lại nữa, tránh gọi SaveChanges nhiều lần

            //var oldTokens = await _unitOfWork.UserRepository.GetRefreshTokenAsync(user.Id);
            //foreach (var token in oldTokens)
            //{
            //    token.IsRevoked = true;
            //}


            // Update SecurityStamp để invalidate access token hiện tại và vô hiệu hóa tất cả token cũ
            // Đồng thời xóa cache cũ → lần validate tiếp sẽ fail, buộc user phải đăng nhập lại trên tất cả thiết bị
            await user.InvalidateAllTokensAsync(_stampCacheService);


            // Update user trong database
            var updateResult = await _unitOfWork.UserRepository.UpdateAsync(user);
            if (updateResult == 0)
            {
                throw new Exception("Failed to update user during logout");
            }

            return new AuthenticationResponse
            {
            };
        }

        public async Task<bool> VerifyEmailAsync(ResendVerifyRequest request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null || user.IsVerified == true)
            {
                return false;
            }

            // Tạo JWT token thay vì DataProtection
            var verificationToken = GenerateEmailVerificationToken(user.Id, user.SecurityStamp);

            var encodedToken = HttpUtility.UrlEncode(verificationToken);
            var confirmUrl = $"{_configuration["Appsettings:BaseUrl"]}/confirm-email?email={user.Email!}&token={encodedToken}";


            await _emailService.SendEmailAsync(new EmailRequest
            {
                To = user.Email!,
                Subject = "Confirm your email for register",
                Body = EmailTemplateReader.ConfirmationTemplate(user.Username!, confirmUrl)
            }, cancellationToken);

            return true;
        }

        // Thêm các helper methods
        private string GenerateEmailVerificationToken(int userId, string securityStamp)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", userId.ToString()),
                    new Claim("type", "email_verification"),
                    new Claim("SecurityStamp", securityStamp),
                    new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(24), // 24 giờ hết hạn
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = signingCredentials
            };

            var handler = new JsonWebTokenHandler();
            return handler.CreateToken(tokenDescriptor);
        }

        public async Task<AuthenticationResponse> ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            if (user.IsVerified == true)
            {
                throw new BadRequestException("Email is already verified");
            }


            try
            {
                var decodedToken = HttpUtility.UrlDecode(request.Token);
                var (userId, securityStamp) = await ValidateEmailVerificationToken(decodedToken);

                if (userId != user.Id)
                {
                    throw new BadRequestException("Invalid token");
                }

                // Kiểm tra SecurityStamp
                if (!user.IsSecurityStampValid(securityStamp))
                {
                    throw new SecurityStampMismatchException("Token is invalid due to security stamp mismatch. Please request a new verification email.");
                }

                await _unitOfWork.BeginTransactionAsync();
                // Xác thực email thành công
                user.IsVerified = true;
                await user.InvalidateAllTokensAsync(_stampCacheService);
                _unitOfWork.UserRepository.PrepareUpdate(user);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new AuthenticationResponse
                {
                    Message = "Verify Email Successfully!"
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AuthenticationResponse
                {
                };
            }
        }

        private async Task<(int userId, string securityStamp)> ValidateEmailVerificationToken(string token)
        {
            var tokenHandler = new JsonWebTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            if (!result.IsValid || result.ClaimsIdentity == null)
            {
                throw new BadRequestException("Invalid token");
            }

            var typeClaim = result.ClaimsIdentity.FindFirst("type")?.Value;
            if (typeClaim != "email_verification")
            {
                throw new BadRequestException("Invalid token type");
            }

            var userIdClaim = result.ClaimsIdentity.FindFirst("sub")?.Value;
            var securityStampClaim = result.ClaimsIdentity.FindFirst("SecurityStamp")?.Value;
            if (!int.TryParse(userIdClaim, out int userId) || string.IsNullOrEmpty(securityStampClaim))
            {
                throw new BadRequestException("Invalid token claims");
            }


            return (userId, securityStampClaim);
        }

        public async Task<AuthenticationResponse> LoginWithGoogle(GoogleAccessTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.DeviceId))
                throw new BadRequestException("DeviceId is required for login");

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

                var response = await client.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                if (!response.IsSuccessStatusCode)
                {
                    throw new BadRequestException("Invalid Google access token");
                }

                var content = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content);

                if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                {
                    throw new UnauthorizedException("Failed to retrieve user info from Google");
                }

                var user = await _unitOfWork.UserRepository.GetByEmailAsync(userInfo.Email);
                if (user == null)
                {
                    //  Tạo mới user với avatar từ Google
                    user = new User
                    {
                        Username = userInfo.Name,
                        Email = userInfo.Email,
                        Status = (int)UserStatusEnum.Active,
                        RoleId = (int)RoleEnum.Customer,
                        CreatedAt = DateTime.UtcNow,
                        AvatarUrl = userInfo.Picture,
                        IsVerified = true,
                        PasswordHash = string.Empty  // Google users have no password
                    };

                    user.UpdateSecurityStamp();
                    _unitOfWork.UserRepository.PrepareCreate(user);
                    await _unitOfWork.SaveAsync();
                }
                else
                {
                    //  Nếu user chưa có avatar thì cập nhật từ Google
                    if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(userInfo.Picture))
                    {
                        user.AvatarUrl = userInfo.Picture;
                        _unitOfWork.UserRepository.PrepareUpdate(user);
                        await _unitOfWork.SaveAsync();
                    }

                    if (user.Status == (int)UserStatusEnum.Inactive)
                    {
                        throw new ForbiddenException("Account is disabled");
                    }
                }

                // Lấy thông tin gói đăng ký hiện tại của customer
                string roleName = user.Role?.Name ?? "Customer";

                // Generate tokens
                var accessToken = GenerateAccessToken(user, roleName);
                var refreshToken = GenerateRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                // Persist refresh token like normal login

                // Revoke tất cả refresh token cũ của user
                if (!string.IsNullOrWhiteSpace(request.DeviceId))
                {
                    var oldDeviceTokens = await _unitOfWork.UserRepository.GetOldRefreshTokenByDeviceIdAsync(user.Id, request.DeviceId);
                    if (oldDeviceTokens != null)
                    {
                        foreach (var token in oldDeviceTokens)
                        {
                            token.IsRevoked = true;
                        }
                    }
                }

                // Tạo refresh token mới
                var newRefreshToken = new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    IsRevoked = false,
                    CreatedDate = DateTime.UtcNow,
                    ExpiryDate = refreshTokenExpiry,
                    DeviceId = request.DeviceId
                };

                user.RefreshTokens.Add(newRefreshToken);
                var success = await _unitOfWork.SaveAsync();

                if (success == 0)
                {
                    throw new Exception("Failed to update RefreshToken");
                }

                await _stampCacheService.SetSecurityStampAsync(user.Id, user.SecurityStamp);

                return new AuthenticationResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Google login failed: " + ex.Message);
            }
        }

        public async Task<AuthenticationResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            // Business Rule: Chỉ cho phép reset khi đã verified
            if (user.IsVerified != true)
            {
                throw new BadRequestException("Email has not been verified. Please verify your email before resetting password.");
            }


            try
            {
                var decodedToken = request.Token;
                var (userId, securityStamp) = await ValidatePasswordResetToken(decodedToken);

                if (userId != user.Id)
                {
                    throw new BadRequestException("Invalid token");
                }

                // Kiểm tra SecurityStamp
                if (!user.IsSecurityStampValid(securityStamp))
                {
                    throw new BadRequestException("Token is invalid due to security stamp mismatch. Please request a new password reset email.");
                }

                // Validate password complexity
                ValidatePassword(request.NewPassword);

                // Validate password confirmation
                if (request.NewPassword != request.ConfirmNewPassword)
                {
                    throw new BadRequestException("Password and confirmation password do not match");
                }

                //Validate old and new password
                //if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.Password))
                //{
                //    return new AuthenticationResponse
                //    {
                //        Success = false,
                //        Message = "Mật khẩu mới không được trùng với mật khẩu cũ"
                //    };
                //}

                await _unitOfWork.BeginTransactionAsync();

                // Cập nhật mật khẩu mới đã được mã hóa
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                // Xóa refresh token cũ để buộc đăng nhập lại
                await user.InvalidateAllTokensAsync(_stampCacheService);

                _unitOfWork.UserRepository.PrepareUpdate(user);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new AuthenticationResponse
                {
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Password reset failed: " + ex.Message);

            }
        }

        private async Task<(int userId, string securityStamp)> ValidatePasswordResetToken(string token)
        {
            var tokenHandler = new JsonWebTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            if (!result.IsValid || result.ClaimsIdentity == null)
            {
                throw new BadRequestException("Invalid token");
            }

            var typeClaim = result.ClaimsIdentity.FindFirst("type")?.Value;
            if (typeClaim != "password_reset")
            {
                throw new BadRequestException("Invalid token type");
            }

            var userIdClaim = result.ClaimsIdentity.FindFirst("sub")?.Value;
            var securityStampClaim = result.ClaimsIdentity.FindFirst("SecurityStamp")?.Value;
            if (!int.TryParse(userIdClaim, out int userId) || string.IsNullOrEmpty(securityStampClaim))
            {
                throw new BadRequestException("Invalid token claims");
            }

            return (userId, securityStampClaim);
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return false;
            }

            // BUSINESS RULE: Chỉ cho phép reset password khi đã verified
            if (user.IsVerified != true)
            {
                return false; // Không gửi email nếu chưa verify
            }


            // Tạo JWT token cho reset password
            var resetToken = GeneratePasswordResetToken(user.Id, user.SecurityStamp);

            var encodedToken = HttpUtility.UrlEncode(resetToken);
            var resetUrl = $"{_configuration["Appsettings:BaseUrl"]}/reset-password?email={HttpUtility.UrlEncode(user.Email!)}&token={encodedToken}";

            await _emailService.SendEmailAsync(new EmailRequest
            {
                To = user.Email!,
                Subject = "Reset Password",
                Body = EmailResetPasswordTemplate.ResetConfirmationTemplate(user.Username!, resetUrl)
            }, cancellationToken);

            return true;
        }

        private string GeneratePasswordResetToken(int userId, string securityStamp)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", userId.ToString()),
                    new Claim("type", "password_reset"),
                    new Claim("SecurityStamp", securityStamp),
                    new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(30), // 30 phút hết hạn
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = signingCredentials
            };

            var handler = new JsonWebTokenHandler();
            return handler.CreateToken(tokenDescriptor);
        }

        // OTP Methods for Email Verification
        public async Task<OtpResponse> SendOtpEmailVerificationAsync(SendOtpEmailVerificationRequest request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return new OtpResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            if (user.IsVerified)
            {
                return new OtpResponse
                {
                    Success = false,
                    Message = "Email is already verified"
                };
            }

            try
            {
                // Generate 6-digit OTP
                var otpCode = GenerateOtpCode();
                var expiresAt = DateTime.UtcNow.AddMinutes(10); // 10 minutes

                // Save OTP to cache
                var saved = await _otpCacheService.SaveOtpAsync(
                    request.Email,
                    otpCode,
                    "EmailVerification",
                    user.Id,
                    expiryMinutes: 10
                );

                if (!saved)
                {
                    return new OtpResponse
                    {
                        Success = false,
                        Message = "Failed to generate OTP"
                    };
                }

                // Send OTP via email
                await _emailService.SendEmailAsync(new EmailRequest
                {
                    To = user.Email!,
                    Subject = "Verify your email address",
                    Body = EmailTemplateReader.OtpEmailVerificationTemplate(user.Username!, otpCode, expiresAt)
                }, cancellationToken);

                return new OtpResponse
                {
                    Success = true,
                    Message = "OTP sent successfully to your email",
                    ExpiresAt = expiresAt
                };
            }
            catch
            {
                return new OtpResponse
                {
                    Success = false,
                    Message = "Failed to send OTP"
                };
            }
        }

        public async Task<OtpResponse> VerifyOtpEmailVerificationAsync(VerifyOtpEmailVerificationRequest request)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return new OtpResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Validate OTP from cache
            var isValid = await _otpCacheService.ValidateOtpAsync(request.Email, request.OtpCode.Trim(), "EmailVerification");

            if (!isValid)
            {
                return new OtpResponse
                {
                    Success = false,
                    Message = "Invalid or expired OTP"
                };
            }

            // Verify email
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                user.IsVerified = true;
                user.InvalidateAllTokensAsync(_stampCacheService);
                _unitOfWork.UserRepository.PrepareUpdate(user);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new OtpResponse
                {
                    Success = true,
                    Message = "Email verified successfully"
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new OtpResponse
                {
                    Success = false,
                    Message = "Failed to verify email"
                };
            }
        }

        // OTP Methods for Password Reset
        public async Task<OtpResponse> SendOtpPasswordResetAsync(SendOtpPasswordResetRequest request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return new OtpResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            if (!user.IsVerified)
            {
                return new OtpResponse
                {
                    Success = false,
                    Message = "Email is not verified. Please verify your email first."
                };
            }

            try
            {
                // Generate 6-digit OTP
                var otpCode = GenerateOtpCode();
                var expiresAt = DateTime.UtcNow.AddMinutes(10); // 10 minutes

                // Save OTP to cache
                var saved = await _otpCacheService.SaveOtpAsync(
                    request.Email,
                    otpCode,
                    "PasswordReset",
                    user.Id,
                    expiryMinutes: 10
                );

                if (!saved)
                {
                    return new OtpResponse
                    {
                        Success = false,
                        Message = "Failed to generate OTP"
                    };
                }

                // Send OTP via email
                await _emailService.SendEmailAsync(new EmailRequest
                {
                    To = user.Email!,
                    Subject = "Reset your password",
                    Body = EmailTemplateReader.OtpPasswordResetTemplate(user.Username!, otpCode, expiresAt)
                }, cancellationToken);

                return new OtpResponse
                {
                    Success = true,
                    Message = "OTP sent successfully to your email",
                    ExpiresAt = expiresAt
                };
            }
            catch
            {
                return new OtpResponse
                {
                    Success = false,
                    Message = "Failed to send OTP"
                };
            }
        }

        public async Task<OtpResponse> VerifyOtpPasswordResetAsync(VerifyOtpPasswordResetRequest request)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return new OtpResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Validate OTP from cache
            var isValid = await _otpCacheService.ValidateOtpAsync(request.Email, request.OtpCode.Trim(), "PasswordReset");

            if (!isValid)
            {
                return new OtpResponse
                {
                    Success = false,
                    Message = "Invalid or expired OTP"
                };
            }

            return new OtpResponse
            {
                Success = true,
                Message = "OTP verified successfully. You can now reset your password."
            };
        }

        public async Task<AuthenticationResponse> ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            if (!user.IsVerified)
            {
                throw new BadRequestException("Email has not been verified. Please verify your email first.");
            }

            ValidatePassword(request.NewPassword);
            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new BadRequestException("Password and confirmation password do not match");
            }

            // Validate OTP from cache
            var isValid = await _otpCacheService.ValidateOtpAsync(request.Email, request.OtpCode.Trim(), "PasswordReset");

            if (!isValid)
            {
                throw new BadRequestException("Invalid or expired OTP");
            }

            // Reset password
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.UpdateSecurityStamp();
                await user.InvalidateAllTokensAsync(_stampCacheService);

                _unitOfWork.UserRepository.PrepareUpdate(user);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new AuthenticationResponse
                {
                    Message = "Password reset successfully. Please login with your new password."
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Failed to reset password. Please try again.");
            }
        }

        private string GenerateOtpCode()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var number = BitConverter.ToUInt32(bytes, 0);
                // Generate 6-digit number (100000 - 999999)
                return (100000 + (number % 900000)).ToString();
            }
        }
    }
}

