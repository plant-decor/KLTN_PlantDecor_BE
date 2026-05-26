using Microsoft.AspNetCore.Http;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class AnalyzeRoomOnlyUploadRequest
    {
        public List<IFormFile> Images { get; set; } = new();
    }
}
