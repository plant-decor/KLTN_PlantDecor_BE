using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface INurseryService
    {
        // CRUD Operations
        Task<PaginatedResult<NurseryListResponseDto>> GetAllNurseriesAsync(Pagination pagination);
        Task<PaginatedResult<NurseryListResponseDto>> GetActiveNurseriesAsync(Pagination pagination);
        Task<NurseryResponseDto?> GetNurseryByIdAsync(int id);
        Task<NurseryResponseDto?> GetMyNurseryAsync(int managerId);
        Task<NurseryResponseDto> CreateNurseryAsync(NurseryRequestDto request);
        Task<NurseryResponseDto> UpdateNurseryAsync(int id, NurseryUpdateDto request);
        Task<bool> ToggleActiveAsync(int id);
        Task<NurseryResponseDto> AssignManagerAsync(int nurseryId, int managerId);

        // Manager Operations
        Task<NurseryResponseDto> UpdateMyNurseryAsync(int managerId, NurseryUpdateDto request);
        Task<List<NurseryMaterialExpiryAlertDto>> GetMyNurseryExpiringMaterialsAsync(int managerId, int daysAhead = 30);
        Task<List<NurseryLowStockProductAlertDto>> GetMyNurseryLowStockProductsAsync(int managerId, int threshold = 5);
        Task<NurseryMaterialSummaryResponseDto> GetMyNurseryMaterialSummaryAsync(int managerId, int lowStockThreshold = 5, int expiringInDays = 30);

        // Nearby
        Task<List<NurseryNearbyResponseDto>> GetNearbyNurseriesAsync(decimal lat, decimal lng, decimal radiusKm, int? packageId);

        // Staff
        Task<List<StaffWithSpecializationsResponseDto>> GetNurseryStaffAsync(int managerId);
        Task<StaffWithSpecializationsResponseDto> GetNurseryStaffDetailAsync(int managerId, int staffId);
        Task<List<StaffWithSpecializationsResponseDto>> GetNurseryTeamForManagerAsync(int managerId);
        Task<StaffWithSpecializationsResponseDto> GetNurseryTeamDetailForManagerAsync(int managerId, int staffId);
    }
}
