using Microsoft.AspNetCore.Http;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class UploadRoomImagesRequest
    {
        public List<IFormFile> Images { get; set; } = new();
        public List<RoomViewAngleEnum> ViewAngles { get; set; } = new();
    }
}
