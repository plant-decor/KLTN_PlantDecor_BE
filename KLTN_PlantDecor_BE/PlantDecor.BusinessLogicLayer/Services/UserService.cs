using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
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
        private readonly ILogger<UserService> _logger;

        public UserService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
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
            throw new NotImplementedException();
        }

        public Task<bool> IsPhoneExistsForOtherUserAsync(string phone, int currentUserId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetActive(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<UserResponse> UpdateAsync(int id, UserUpdate user)
        {
            throw new NotImplementedException();
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
    }
}
