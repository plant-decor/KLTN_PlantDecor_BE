using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class UploadedRoomImageDto
    {
        public int RoomImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public RoomViewAngleEnum ViewAngle { get; set; }
        public RoomUploadModerationStatusEnum ModerationStatus { get; set; }
        public string? ModerationReason { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
