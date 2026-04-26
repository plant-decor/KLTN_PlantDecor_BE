using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class RoomImageMapper
    {
        public static UploadedRoomImageDto ToUploadedRoomImageDto(this RoomImage image)
        {
            var latestModeration = image.RoomUploadModerations
                .OrderByDescending(moderation => moderation.ReviewedAt)
                .ThenByDescending(moderation => moderation.Id)
                .FirstOrDefault();

            var moderationStatus = RoomUploadModerationStatusEnum.Pending;
            if (latestModeration?.Status is int rawStatus &&
                Enum.IsDefined(typeof(RoomUploadModerationStatusEnum), rawStatus))
            {
                moderationStatus = (RoomUploadModerationStatusEnum)rawStatus;
            }

            return new UploadedRoomImageDto
            {
                RoomImageId = image.Id,
                ImageUrl = image.ImageUrl ?? string.Empty,
                ViewAngle = Enum.IsDefined(typeof(RoomViewAngleEnum), image.ViewAngle ?? 0)
                    ? (RoomViewAngleEnum)image.ViewAngle!.Value
                    : RoomViewAngleEnum.Front,
                ModerationStatus = moderationStatus,
                ModerationReason = latestModeration?.Reason,
                UploadedAt = image.UploadedAt ?? DateTime.UtcNow
            };
        }
    }
}
