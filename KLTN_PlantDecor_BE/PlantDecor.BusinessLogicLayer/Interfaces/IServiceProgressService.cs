using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IServiceProgressService
    {
        Task<ServiceProgressResponseDto> GetByIdAsync(int progressId, int userId);
        Task<List<ServiceProgressResponseDto>> GetTodayScheduleAsync(int caretakerId);
        Task<List<ServiceProgressResponseDto>> GetByRegistrationIdAsync(int registrationId, int userId);
        Task<ServiceProgressResponseDto> CheckInAsync(int caretakerId, int progressId);
        Task<ServiceProgressResponseDto> SubmitIncidentReportAsync(int caretakerId, int progressId, SubmitIncidentReportRequestDto request, IFormFile incidentImage);
        Task<ServiceProgressResponseDto> CheckOutAsync(int caretakerId, int progressId, CheckOutRequestDto request, IFormFile? evidenceImage);
        Task<ServiceProgressResponseDto> ReassignCaretakerAsync(int managerId, int progressId, int newCaretakerId);
        Task<List<StaffWithSpecializationsResponseDto>> GetEligibleCaretakersForProgressAsync(int managerId, int progressId);
        Task<List<ServiceProgressResponseDto>> GetNurseryScheduleAsync(int managerId, DateOnly date);
        Task<List<ServiceProgressResponseDto>> GetCaretakerScheduleAsync(int managerId, int caretakerId, DateOnly from, DateOnly to);
        Task<List<ServiceProgressResponseDto>> GetMyScheduleAsync(int caretakerId, DateOnly from, DateOnly to);
    }
}
