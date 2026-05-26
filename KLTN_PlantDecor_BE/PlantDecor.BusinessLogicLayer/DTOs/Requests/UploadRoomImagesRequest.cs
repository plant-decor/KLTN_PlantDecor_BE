using Microsoft.AspNetCore.Http;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class UploadRoomImagesRequest
    {
        public List<IFormFile> Images { get; set; } = new();
        // Order priorities for each uploaded image. Lower number means higher priority. Must match image count.
        public List<int> OrderIndexes { get; set; } = new();
    }
}
