using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Extensions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
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
        public Task<bool> Deactive(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<PaginatedResult<UserResponse>> GetAllAsyncWithPagination(Pagination pagination)
        {
            throw new NotImplementedException();
        }

        public async Task<UserResponse> GetByIdAsync(int id)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
            return user?.ToResponse();
        }

        public Task<bool> GetUserByPhoneAsync(string phone)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsEmailExistsForOtherUserAsync(string email, int currentUserId)
        {
            return _unitOfWork.UserRepository.IsEmailExistsForOtherUserAsync(email, currentUserId);
        }

        public Task<bool> IsPhoneExistsForOtherUserAsync(string phone, int currentUserId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetActive(int userId)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
