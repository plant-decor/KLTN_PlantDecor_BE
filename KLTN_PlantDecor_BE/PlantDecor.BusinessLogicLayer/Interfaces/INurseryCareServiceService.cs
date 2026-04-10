using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface INurseryCareServiceService
    {
        /// <summary>Public: lấy các gói dịch vụ đang active của một vựa</summary>
        Task<List<NurseryCareServiceResponseDto>> GetActiveByNurseryIdAsync(int nurseryId);

        /// <summary>Manager: lấy tất cả gói dịch vụ của vựa mình (kể cả inactive)</summary>
        Task<List<NurseryCareServiceResponseDto>> GetAllByManagerAsync(int managerId);

        /// <summary>Manager: thêm một gói vào vựa</summary>
        Task<NurseryCareServiceResponseDto> AddToNurseryAsync(int managerId, CreateNurseryCareServiceRequestDto request);

        /// <summary>Manager: bật/tắt gói dịch vụ</summary>
        Task<NurseryCareServiceResponseDto> ToggleActiveAsync(int managerId, int id);

        /// <summary>Manager: xóa gói khỏi vựa (chỉ khi chưa có đăng ký nào)</summary>
        Task RemoveFromNurseryAsync(int managerId, int id);
        /// <summary>Manager: các gói dịch vụ vựa mình đang kinh doanh (IsActive=true)</summary>
        Task<List<NurseryCareServiceResponseDto>> GetActiveByManagerAsync(int managerId);
    }
}
