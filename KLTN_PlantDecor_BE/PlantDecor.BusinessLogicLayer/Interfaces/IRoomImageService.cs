using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IRoomImageService
    {
        Task<UploadRoomImagesResponseDto> UploadRoomImagesAsync(UploadRoomImagesRequest request, int userId);
        Task<UploadRoomImagesResponseDto> GetAllRoomImagesByUserIdAsync(int userId);
        Task<UploadRoomImagesResponseDto> GetAllRoomImagesByUserIdAndViewAngleAsync(int userId, RoomViewAngleEnum viewAngle);
    }
}
