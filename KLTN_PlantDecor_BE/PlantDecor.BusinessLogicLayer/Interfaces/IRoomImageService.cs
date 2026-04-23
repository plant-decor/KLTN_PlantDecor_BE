using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IRoomImageService
    {
        Task<UploadRoomImagesResponseDto> UploadRoomImagesAsync(UploadRoomImagesRequest request, int userId);
    }
}
