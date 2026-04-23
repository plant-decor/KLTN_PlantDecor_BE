using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class RoomImageAnalysisInputDto
    {
        public int? RoomImageId { get; set; }
        public string ImageBase64 { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public RoomViewAngleEnum? ViewAngle { get; set; }
    }
}
