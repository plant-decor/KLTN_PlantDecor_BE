using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IDesignTaskService
    {
        Task<DesignTaskResponseDto> GetByIdAsync(int taskId, int userId);
        Task<List<DesignTaskResponseDto>> GetByRegistrationIdAsync(int registrationId, int userId);
        Task<PaginatedResult<DesignTaskResponseDto>> GetMyTasksAsync(int userId, Pagination pagination, int? status = null);
        Task<DesignTaskResponseDto> AssignTaskAsync(int managerId, int taskId, AssignDesignTaskRequestDto request);
        Task<DesignTaskResponseDto> UpdateStatusAsync(int userId, int taskId, UpdateDesignTaskStatusRequestDto request, IFormFile? reportImage = null);
    }
}
