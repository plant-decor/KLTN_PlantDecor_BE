using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using Microsoft.AspNetCore.Http;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IDesignRegistrationService
    {
        Task<DesignRegistrationResponseDto> CreateAsync(int userId, CreateDesignRegistrationRequestDto request);
        Task<PaginatedResult<DesignRegistrationResponseDto>> GetMyRegistrationsAsync(int userId, Pagination pagination, int? status = null);
        Task<DesignRegistrationResponseDto> GetByIdAsync(int id, int requesterId);
        Task<PaginatedResult<DesignRegistrationResponseDto>> GetAllForNurseryAsync(int managerId, Pagination pagination, int? status = null);
        Task<DesignRegistrationResponseDto> GetByIdAsOperatorAsync(int managerId, int id);
        Task<DesignRegistrationResponseDto> ApproveAsync(int managerId, int id);
        Task<DesignRegistrationResponseDto> RejectAsync(int managerId, int id, string? rejectReason);
        Task<DesignRegistrationResponseDto> AssignCaretakerAsync(int managerId, int id, int caretakerId);
        Task<DesignRegistrationResponseDto> UpdateSurveyInfoAsync(int caretakerId, int id, UpdateDesignRegistrationSurveyInfoRequestDto request, IFormFile? currentStateImage = null);
        Task<DesignRegistrationResponseDto> CancelAsync(int userId, int id, string? cancelReason);
        Task<DesignRegistrationResponseDto> ManagerCancelAsync(int managerId, int id, string? cancelReason);
    }
}
