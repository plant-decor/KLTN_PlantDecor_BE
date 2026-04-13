using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IServiceRegistrationService
    {
        Task<ServiceRegistrationResponseDto> CreateAsync(int userId, CreateServiceRegistrationRequestDto request);
        Task<PaginatedResult<ServiceRegistrationResponseDto>> GetMyRegistrationsAsync(int userId, Pagination pagination, int? status);
        Task<PaginatedResult<ServiceRegistrationResponseDto>> GetPendingForNurseryAsync(int managerId, Pagination pagination);
        Task<PaginatedResult<ServiceRegistrationResponseDto>> GetAllForNurseryAsync(int managerId, Pagination pagination, int? status = null);
        Task<ServiceRegistrationResponseDto> GetByIdAsync(int id, int userId);
        Task<ServiceRegistrationResponseDto> GetByIdAsManagerAsync(int managerId, int id);
        Task<ServiceRegistrationResponseDto> ApproveAsync(int managerId, int id);
        Task<ServiceRegistrationResponseDto> RejectAsync(int managerId, int id, string? rejectReason);
        Task<ServiceRegistrationResponseDto> AssignCaretakerAsync(int managerId, int id, int caretakerId);
        Task<ServiceRegistrationResponseDto> CancelAsync(int userId, int id, string? cancelReason);
        Task<PaginatedResult<ServiceRegistrationResponseDto>> GetMyTasksAsync(int caretakerId, Pagination pagination, int? status);
        Task<ServiceRegistrationResponseDto> ManagerCancelAsync(int managerId, int id, string? cancelReason);
        Task<List<StaffWithSpecializationsResponseDto>> GetEligibleCaretakersForRegistrationAsync(int managerId, int registrationId);
    }
}
