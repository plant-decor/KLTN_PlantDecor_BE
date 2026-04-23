using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using Microsoft.Extensions.Logging;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class RoomImageService : IRoomImageService
    {
        private const int MAX_UPLOAD_ROOM_IMAGES = 4;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<RoomImageService> _logger;

        public RoomImageService(
            IUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            ILogger<RoomImageService> logger)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        public async Task<UploadRoomImagesResponseDto> UploadRoomImagesAsync(UploadRoomImagesRequest request, int userId)
        {
            if (request == null)
            {
                const string message = "Request body is required";
                await SaveRoomUploadModerationAsync(null, RoomUploadModerationStatusEnum.Rejected, message);
                throw new BadRequestException(message);
            }

            if (request.Images == null || request.Images.Count == 0)
            {
                const string message = "At least one room image file is required";
                await SaveRoomUploadModerationAsync(null, RoomUploadModerationStatusEnum.Rejected, message);
                throw new BadRequestException(message);
            }

            if (request.Images.Count > MAX_UPLOAD_ROOM_IMAGES)
            {
                var message = $"Maximum {MAX_UPLOAD_ROOM_IMAGES} room images are allowed";
                await SaveRoomUploadModerationAsync(null, RoomUploadModerationStatusEnum.Rejected, message);
                throw new BadRequestException(message);
            }

            if (request.ViewAngles == null || request.ViewAngles.Count != request.Images.Count)
            {
                const string message = "ViewAngles must be provided and match the number of uploaded images";
                await SaveRoomUploadModerationAsync(null, RoomUploadModerationStatusEnum.Rejected, message);
                throw new BadRequestException(message);
            }

            if (request.ViewAngles.Distinct().Count() != request.ViewAngles.Count)
            {
                const string message = "ViewAngles must be unique within one upload";
                await SaveRoomUploadModerationAsync(null, RoomUploadModerationStatusEnum.Rejected, message);
                throw new BadRequestException(message);
            }

            foreach (var image in request.Images)
            {
                if (image == null || image.Length == 0)
                {
                    const string message = "Room image file is required";
                    await SaveRoomUploadModerationAsync(null, RoomUploadModerationStatusEnum.Rejected, message);
                    throw new BadRequestException(message);
                }

                var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(image);
                if (!isValid)
                {
                    var message = string.IsNullOrWhiteSpace(errorMessage)
                        ? "Invalid room image file"
                        : errorMessage;
                    await SaveRoomUploadModerationAsync(null, RoomUploadModerationStatusEnum.Rejected, message);
                    throw new BadRequestException(message);
                }
            }

            var uploadedRoomImages = new List<RoomImage>();

            try
            {
                for (var i = 0; i < request.Images.Count; i++)
                {
                    var image = request.Images[i];
                    var uploadedImage = await _cloudinaryService.UploadFileAsync(image, "RoomImages");

                    var roomImage = new RoomImage
                    {
                        UserId = userId,
                        ImageUrl = uploadedImage.SecureUrl,
                        ViewAngle = (int)request.ViewAngles[i],
                        UploadedAt = DateTime.UtcNow
                    };

                    _unitOfWork.RoomImageRepository.PrepareCreate(roomImage);
                    uploadedRoomImages.Add(roomImage);
                }

                await _unitOfWork.SaveAsync();

                foreach (var roomImage in uploadedRoomImages)
                {
                    await SaveRoomUploadModerationAsync(
                        roomImage.Id,
                        RoomUploadModerationStatusEnum.Approved,
                        "Image validated successfully");
                }

                return new UploadRoomImagesResponseDto
                {
                    RoomImages = uploadedRoomImages
                        .Where(image => image.Id > 0)
                        .Select(image => new UploadedRoomImageDto
                        {
                            RoomImageId = image.Id,
                            ImageUrl = image.ImageUrl ?? string.Empty,
                            ViewAngle = Enum.IsDefined(typeof(RoomViewAngleEnum), image.ViewAngle ?? 0)
                                ? (RoomViewAngleEnum)image.ViewAngle!.Value
                                : RoomViewAngleEnum.Front,
                            ModerationStatus = RoomUploadModerationStatusEnum.Approved,
                            ModerationReason = "Image validated successfully",
                            UploadedAt = image.UploadedAt ?? DateTime.UtcNow
                        })
                        .ToList()
                };
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while uploading room images");

                if (uploadedRoomImages.Any(image => image.Id > 0))
                {
                    foreach (var roomImage in uploadedRoomImages.Where(image => image.Id > 0))
                    {
                        await SaveRoomUploadModerationAsync(roomImage.Id, RoomUploadModerationStatusEnum.Rejected, ex.Message);
                    }
                }
                else
                {
                    await SaveRoomUploadModerationAsync(null, RoomUploadModerationStatusEnum.Rejected, ex.Message);
                }

                throw;
            }
        }

        private async Task SaveRoomUploadModerationAsync(
            int? roomImageId,
            RoomUploadModerationStatusEnum status,
            string? reason)
        {
            try
            {
                var defaultReason = status == RoomUploadModerationStatusEnum.Approved
                    ? "Image validated successfully"
                    : "Invalid room image";

                var moderation = new RoomUploadModeration
                {
                    RoomImageId = roomImageId,
                    Status = (int)status,
                    Reason = TrimAndLimit(string.IsNullOrWhiteSpace(reason) ? defaultReason : reason, 255),
                    ReviewedAt = DateTime.UtcNow
                };

                _unitOfWork.RoomUploadModerationRepository.PrepareCreate(moderation);
                await _unitOfWork.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save room upload moderation. RoomImageId={RoomImageId}", roomImageId);
            }
        }

        private static string? TrimAndLimit(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength
                ? trimmed
                : trimmed[..maxLength];
        }
    }
}
