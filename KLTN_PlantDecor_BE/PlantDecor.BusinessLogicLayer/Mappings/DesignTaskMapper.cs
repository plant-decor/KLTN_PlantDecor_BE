using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class DesignTaskMapper
    {
        public static DesignTaskResponseDto ToResponse(this DesignTask task)
        {
            return new DesignTaskResponseDto
            {
                Id = task.Id,
                DesignRegistrationId = task.DesignRegistrationId,
                AssignedStaffId = task.AssignedStaffId,
                ScheduledDate = task.ScheduledDate,
                TaskType = task.TaskType,
                TaskTypeName = Enum.IsDefined(typeof(TaskTypeEnum), task.TaskType)
                    ? ((TaskTypeEnum)task.TaskType).ToString()
                    : $"Unknown({task.TaskType})",
                ReportImageUrl = task.ReportImageUrl,
                CreatedAt = task.CreatedAt,
                Status = task.Status,
                StatusName = Enum.IsDefined(typeof(DesignTaskStatusEnum), task.Status)
                    ? ((DesignTaskStatusEnum)task.Status).ToString()
                    : $"Unknown({task.Status})",
                AssignedStaff = task.AssignedStaff == null ? null : task.AssignedStaff.ToUserSummary(),
                Registration = task.DesignRegistration == null ? null : new DesignRegistrationTaskSummaryDto
                {
                    Id = task.DesignRegistration.Id,
                    UserId = task.DesignRegistration.UserId,
                    AssignedCaretakerId = task.DesignRegistration.AssignedCaretakerId,
                    NurseryId = task.DesignRegistration.NurseryId,
                    Status = task.DesignRegistration.Status,
                    StatusName = Enum.IsDefined(typeof(DesignRegistrationStatus), task.DesignRegistration.Status)
                        ? ((DesignRegistrationStatus)task.DesignRegistration.Status).ToString()
                        : $"Unknown({task.DesignRegistration.Status})",
                    Address = task.DesignRegistration.Address,
                    Phone = task.DesignRegistration.Phone
                },
                TaskMaterialUsages = task.TaskMaterialUsages
                    .Select(u => new TaskMaterialUsageResponseDto
                    {
                        Id = u.Id,
                        MaterialId = u.MaterialId,
                        MaterialName = u.Material?.Name,
                        ActualQuantity = u.ActualQuantity,
                        Note = u.Note
                    })
                    .ToList()
            };
        }
    }
}