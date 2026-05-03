using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Extensions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ISecurityStampCacheService _securityStampCacheService;
        private readonly ILogger<UserService> _logger;

        public UserService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, ISecurityStampCacheService securityStampCacheService, ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _securityStampCacheService = securityStampCacheService;
            _logger = logger;
        }
        public async Task<bool> Deactive(int userId)
        {
            if (userId <= 0)
                throw new BadRequestException("Invalid user ID");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new NotFoundException($"User with ID {userId} not found");
                }

                if (user.Status == (int)UserStatusEnum.Inactive)
                {
                    await _unitOfWork.CommitTransactionAsync();
                    return true;
                }

                await user.InvalidateAllTokensAsync(_securityStampCacheService);
                user.Status = (int)UserStatusEnum.Inactive;
                user.UpdatedAt = DateTime.UtcNow;

                var updateResult = await _unitOfWork.UserRepository.UpdateAsync(user);
                if (updateResult == 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new Exception("Failed to deactivate user");
                }

                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PaginatedResult<UserResponse>> GetAllAsyncWithPagination(Pagination pagination)
        {
            var appliedPagination = pagination ?? new Pagination();
            var result = await _unitOfWork.UserRepository.GetAllAsyncWithPagination(appliedPagination);

            var mappedItems = result.Items.Select(user => user.ToResponse()).ToList();

            return new PaginatedResult<UserResponse>(
                mappedItems,
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<PaginatedResult<UserResponse>> SearchUsersAsync(UserSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var filter = BuildUserSearchFilter(request);

            if (filter.CreatedFrom.HasValue && filter.CreatedTo.HasValue
                && filter.CreatedFrom.Value > filter.CreatedTo.Value)
            {
                throw new BadRequestException("CreatedFrom must be earlier than CreatedTo");
            }

            var result = await _unitOfWork.UserRepository.SearchAsync(filter, pagination);
            var mappedItems = result.Items.Select(user => user.ToResponse()).ToList();

            return new PaginatedResult<UserResponse>(
                mappedItems,
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<UserResponse> GetByIdAsync(int id)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
            return user == null ? null! : user.ToResponse();
        }

        public Task<bool> GetUserByPhoneAsync(string phone)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsEmailExistsForOtherUserAsync(string email, int currentUserId)
        {
            return _unitOfWork.UserRepository.IsEmailExistsForOtherUserAsync(email, currentUserId);
        }

        public async Task<bool> IsPhoneExistsForOtherUserAsync(string phone, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var existingUser = await _unitOfWork.UserRepository.GetByPhoneAsync(phone.Trim());
            return existingUser != null && existingUser.Id != currentUserId;
        }

        public async Task<bool> SetActive(int userId)
        {
            if (userId <= 0)
                throw new BadRequestException("Invalid user ID");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new NotFoundException($"User with ID {userId} not found");
                }

                if (user.Status == (int)UserStatusEnum.Active)
                {
                    await _unitOfWork.CommitTransactionAsync();
                    return true;
                }

                user.Status = (int)UserStatusEnum.Active;
                user.UpdatedAt = DateTime.UtcNow;

                var updateResult = await _unitOfWork.UserRepository.UpdateAsync(user);
                if (updateResult == 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new Exception("Failed to activate user");
                }

                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<UserResponse> UpdateUserInfoAsync(int id, UserUpdateDto userUpdate)
        {
            if (userUpdate == null)
                throw new BadRequestException("User update data cannot be null");

            if (id <= 0)
                throw new BadRequestException("Invalid user ID");

            if (string.IsNullOrEmpty(userUpdate.FullName))
            {
                throw new BadRequestException("Full name cannot be null or empty");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Get existing user
                var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(id);
                if (existingUser == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new NotFoundException($"User with ID {id} not found");
                }

                if (!string.IsNullOrWhiteSpace(userUpdate.PhoneNumber))
                {
                    var normalizedPhone = userUpdate.PhoneNumber.Trim();
                    var currentPhone = existingUser.PhoneNumber?.Trim();

                    if (!normalizedPhone.Equals(currentPhone, StringComparison.Ordinal))
                    {
                        var isPhoneExists = await IsPhoneExistsForOtherUserAsync(normalizedPhone, id);
                        if (isPhoneExists)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            throw new BadRequestException($"Phone number '{normalizedPhone}' is already in use");
                        }
                    }
                }

                // Map về lại Entity
                UserMapper.ToUpdate(userUpdate, existingUser);

                var updateResult = await _unitOfWork.UserRepository.UpdateAsync(existingUser);

                if (updateResult == 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new Exception("Failed to update user information - no changes were saved");
                }

                await _unitOfWork.CommitTransactionAsync();

                // Map về Response
                return UserMapper.ToResponse(existingUser);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> UpdateAvatar(int userId, IFormFile avatarImage)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Validate file trước khi bắt đầu transaction
            var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(avatarImage);
            if (!isValid)
            {
                throw new BadRequestException("Wrong Image Extension");
            }

            // Upload new avatar (chưa cần transaction vì chưa thao tác DB)
            var avatarUploadResult = await _cloudinaryService.UploadFileAsync(avatarImage, "AvatarImages");
            if (avatarUploadResult == null || string.IsNullOrWhiteSpace(avatarUploadResult.PublicId))
            {
                throw new BadRequestException("Avatar upload failed");
            }

            // Lưu publicId cũ TRƯỚC KHI ghi đè
            //var oldAvatarPublicId = user.avatarPublicId;

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                user.AvatarUrl = avatarUploadResult.SecureUrl;
                // user.avatarPublicId = avatarUploadResult.PublicId;

                var updateResult = await _unitOfWork.UserRepository.UpdateAsync(user);
                if (updateResult == 0)
                {
                    throw new Exception("Failed to update user avatar - no changes were saved");
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                // Dọn ảnh mới vừa upload vì DB thất bại
                try
                {
                    await _cloudinaryService.DeleteFileAsync(avatarUploadResult.PublicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup new avatar: {PublicId}", avatarUploadResult.PublicId);
                }
                throw;
            }

            // Xoá ảnh cũ SAU KHI commit thành công (best-effort)
            //if (!string.IsNullOrWhiteSpace(oldAvatarPublicId))
            //{
            //    try
            //    {
            //        await _cloudinaryService.DeleteFileAsync(oldAvatarPublicId);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogWarning(ex, "Failed to delete old avatar: {PublicId}", oldAvatarPublicId);
            //    }
            //}

            return true;
        }

        public Task<bool> UpdatePasswordAsync(int userId, PasswordUpdate passwordUpdate)
        {
            if (userId <= 0)
                throw new BadRequestException("Invalid user ID");

            if (passwordUpdate == null)
                throw new BadRequestException("Password update data cannot be null");

            if (string.IsNullOrWhiteSpace(passwordUpdate.CurrentPassword) ||
                string.IsNullOrWhiteSpace(passwordUpdate.NewPassword) ||
                string.IsNullOrWhiteSpace(passwordUpdate.ConfirmNewPassword))
                throw new BadRequestException("Current password, new password, and confirmation password are required");

            if (passwordUpdate.NewPassword != passwordUpdate.ConfirmNewPassword)
                throw new BadRequestException("Password and confirmation password do not match");

            ValidatePassword(passwordUpdate.NewPassword);

            return UpdatePasswordInternalAsync(userId, passwordUpdate);
        }

        private async Task<bool> UpdatePasswordInternalAsync(int userId, PasswordUpdate passwordUpdate)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new NotFoundException($"User with ID {userId} not found");
                }

                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    throw new BadRequestException("This account does not have a password yet. Please set a password first.");
                }

                var isCurrentPasswordValid = await _unitOfWork.UserRepository.VerifyPasswordAsync(user, passwordUpdate.CurrentPassword);
                if (!isCurrentPasswordValid)
                {
                    throw new BadRequestException("Current password is incorrect");
                }

                if (BCrypt.Net.BCrypt.Verify(passwordUpdate.NewPassword, user.PasswordHash))
                {
                    throw new BadRequestException("New password cannot be the same as current password");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordUpdate.NewPassword);
                await user.InvalidateAllTokensAsync(_securityStampCacheService);

                var updateResult = await _unitOfWork.UserRepository.UpdateAsync(user);
                if (updateResult == 0)
                {
                    throw new Exception("Failed to change password");
                }

                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> SetPasswordAsync(int userId, SetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new BadRequestException("Passwords do not match");

            ValidatePassword(dto.NewPassword);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new NotFoundException($"User with ID {userId} not found");
                }

                if (!string.IsNullOrEmpty(user.PasswordHash))
                    throw new BadRequestException("Account already has a password. Use reset-password instead.");

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

                var result = await _unitOfWork.UserRepository.UpdateAsync(user);
                if (result == 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new Exception("Failed to set password");
                }

                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private static UserSearchFilter BuildUserSearchFilter(UserSearchRequestDto? request)
        {
            return new UserSearchFilter
            {
                Keyword = request?.Keyword,
                Role = request?.Role,
                Status = request?.Status,
                IsVerified = request?.IsVerified,
                NurseryId = request?.NurseryId,
                CreatedFrom = request?.CreatedFrom,
                CreatedTo = request?.CreatedTo
            };
        }

        private void ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters.");
            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter.");
            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter.");
            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one digit.");
            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                errors.Add("Password must contain at least one special character.");

            if (errors.Count > 0)
                throw new BadRequestException(string.Join(" ", errors));
        }

        public async Task<bool> UpdateEmailAsync(int userId, EmailUpdateDto emailUpdate)
        {
            if (emailUpdate == null || string.IsNullOrEmpty(emailUpdate.Email))
                throw new BadRequestException("Email update data cannot be null");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Get existing user
                var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (existingUser == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new NotFoundException($"User with ID {userId} not found");
                }

                // Block social login users (no password set) from changing email
                if (string.IsNullOrEmpty(existingUser.PasswordHash))
                    throw new BadRequestException("Please set a password for your account before changing email");

                // Verify current password before allowing email change
                var isPasswordValid = await _unitOfWork.UserRepository.VerifyPasswordAsync(existingUser, emailUpdate.CurrentPassword);
                if (!isPasswordValid)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new BadRequestException("Current password is incorrect");
                }

                // Email input different from current email
                if (!emailUpdate.Email.Equals(existingUser.Email, StringComparison.OrdinalIgnoreCase))
                {
                    // Check if email is already used by another user
                    var emailExists = await IsEmailExistsForOtherUserAsync(emailUpdate.Email, userId);
                    if (emailExists)
                    {
                        throw new BadRequestException($"Email '{emailUpdate.Email}' is already in use ");
                    }
                }

                // check if email is actually changed, if yes then update security stamp
                bool needsSecurityStampUpdate = false;

                // Thay đổi Email
                if (!string.IsNullOrWhiteSpace(emailUpdate.Email) &&
                    !emailUpdate.Email.Equals(existingUser.Email, StringComparison.OrdinalIgnoreCase))
                {
                    needsSecurityStampUpdate = true;
                }

                // Map về lại Entity
                existingUser.Email = emailUpdate.Email;
                existingUser.IsVerified = false;

                // Cập nhật SecurityStamp nếu cần
                if (needsSecurityStampUpdate)
                {
                    await existingUser.InvalidateAllTokensAsync(_securityStampCacheService);
                }

                var updateResult = await _unitOfWork.UserRepository.UpdateAsync(existingUser);

                if (updateResult == 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new Exception("Failed to update user information - no changes were saved");
                }

                await _unitOfWork.CommitTransactionAsync();

                return true;

            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
