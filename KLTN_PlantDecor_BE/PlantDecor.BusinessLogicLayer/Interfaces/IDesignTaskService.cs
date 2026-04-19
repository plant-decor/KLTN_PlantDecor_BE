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
        Task<List<DesignTaskPackageMaterialResponseDto>> GetPackageMaterialsForTaskAsync(int userId, int taskId);
        Task<PaginatedResult<DesignTaskResponseDto>> GetMyTasksAsync(
            int userId,
            Pagination pagination,
            int? status = null,
            DateOnly? from = null,
            DateOnly? to = null);
        Task<DesignTaskResponseDto> AssignTaskAsync(int managerId, int taskId, AssignDesignTaskRequestDto request);
        Task<DesignTaskResponseDto> ReportMaterialUsageAsync(int userId, int taskId, ReportDesignTaskMaterialUsageRequestDto request);
        Task<DesignTaskResponseDto> UpdateStatusAsync(int userId, int taskId, UpdateDesignTaskStatusRequestDto request, IFormFile? reportImage = null);
    }
}
