using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ICareServicePackageService
    {
        Task<List<CareServicePackageResponseDto>> GetAllActiveAsync();
        Task<List<CareServicePackageResponseDto>> GetAllAsync();
        Task<CareServicePackageResponseDto> GetByIdAsync(int id);
        Task<CareServicePackageWithNurseriesResponseDto> GetByIdWithNurseriesAsync(int id);
        Task<CareServicePackageResponseDto> CreateAsync(CreateCareServicePackageRequestDto request);
        Task<CareServicePackageResponseDto> UpdateAsync(int id, UpdateCareServicePackageRequestDto request);
        Task DeleteAsync(int id);
        /// <summary>Public: các gói dịch vụ có ít nhất 1 vựa đang kinh doanh</summary>
        Task<List<CareServicePackageWithNurseriesResponseDto>> GetPackagesWithNurseriesAsync();
        /// <summary>Manager: các gói dịch vụ vựa mình chưa kinh doanh (active)</summary>
        Task<List<CareServicePackageResponseDto>> GetNotOfferedByManagerAsync(int managerId);
        /// <summary>Consultant: gợi ý gói dịch vụ theo dữ liệu cây trong một đơn hàng</summary>
        Task<List<CareServicePackageRecommendationResponseDto>> RecommendByOrderAsync(int consultantId, int orderId, int top = 5);
        /// <summary>Admin: thay thế toàn bộ chuyên môn của gói dịch vụ</summary>
        Task<CareServicePackageResponseDto> UpdateSpecializationsAsync(int packageId, List<int> specializationIds);
        /// <summary>Admin: thay thế toàn bộ rule phù hợp của gói dịch vụ</summary>
        Task<CareServicePackageResponseDto> UpdateSuitabilityRulesAsync(int packageId, List<PackagePlantSuitabilityRuleRequestDto> rules);
    }
}
