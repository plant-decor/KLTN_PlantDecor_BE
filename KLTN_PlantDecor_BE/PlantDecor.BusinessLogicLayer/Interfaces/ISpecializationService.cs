using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ISpecializationService
    {
        Task<List<SpecializationResponseDto>> GetAllAsync();
        Task<List<SpecializationResponseDto>> GetAllActiveAsync();
        Task<SpecializationResponseDto> GetByIdAsync(int id);
        Task<SpecializationResponseDto> CreateAsync(SpecializationRequestDto request);
        Task<SpecializationResponseDto> UpdateAsync(int id, UpdateSpecializationRequestDto request);
        Task DeleteAsync(int id);

        // Staff specialization management (Manager)
        Task<StaffWithSpecializationsResponseDto> AssignToStaffAsync(int managerId, int staffId, int specializationId);
        Task<StaffWithSpecializationsResponseDto> RemoveFromStaffAsync(int managerId, int staffId, int specializationId);

        // Caretakers eligible for a package (have all required specializations)
        Task<List<StaffWithSpecializationsResponseDto>> GetEligibleCaretakersForPackageAsync(int managerId, int packageId);
    }
}
