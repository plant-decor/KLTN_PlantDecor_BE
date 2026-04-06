using Microsoft.AspNetCore.Http;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class AnalyzeRoomOnlyUploadRequest
    {
        public IFormFile Image { get; set; } = null!;
    }
}
