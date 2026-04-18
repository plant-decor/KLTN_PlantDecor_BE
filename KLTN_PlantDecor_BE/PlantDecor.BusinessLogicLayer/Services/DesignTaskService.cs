using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class DesignTaskService : IDesignTaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public DesignTaskService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<DesignTaskResponseDto> GetByIdAsync(int taskId, int userId)
        {
            var task = await _unitOfWork.DesignTaskRepository.GetByIdWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found");

            await EnsureCanAccessTaskAsync(userId, task);
            return MapToDto(task);
        }

        public async Task<List<DesignTaskResponseDto>> GetByRegistrationIdAsync(int registrationId, int userId)
        {
            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(registrationId)
                ?? throw new NotFoundException($"DesignRegistration {registrationId} not found");

            var canAccess = registration.UserId == userId || registration.AssignedCaretakerId == userId;
            if (!canAccess)
            {
                var operatorNursery = await TryResolveOperatorNurseryAsync(userId);
                canAccess = operatorNursery?.Id == registration.NurseryId;
            }

            if (!canAccess)
            {
                var tasksOfRegistration = registration.DesignTasks ?? new List<DesignTask>();
                canAccess = tasksOfRegistration.Any(x => x.AssignedStaffId == userId);
            }

            if (!canAccess)
                throw new ForbiddenException("You don't have access to this registration tasks");

            var tasks = await _unitOfWork.DesignTaskRepository.GetByRegistrationIdAsync(registrationId);
            return tasks.Select(MapToDto).ToList();
        }

        public async Task<PaginatedResult<DesignTaskResponseDto>> GetMyTasksAsync(int userId, Pagination pagination, int? status = null)
        {
            var result = await _unitOfWork.DesignTaskRepository.GetByAssignedStaffIdAsync(userId, pagination, status);
            return new PaginatedResult<DesignTaskResponseDto>(
                result.Items.Select(MapToDto).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<DesignTaskResponseDto> AssignTaskAsync(int managerId, int taskId, AssignDesignTaskRequestDto request)
        {
            if (request == null)
                throw new BadRequestException("Request body is required");

            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var taskDetail = await _unitOfWork.DesignTaskRepository.GetByIdWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found");

            if (taskDetail.DesignRegistration.NurseryId != nursery.Id)
                throw new ForbiddenException("This task does not belong to your nursery");

            if (taskDetail.Status == (int)DesignTaskStatusEnum.Completed || taskDetail.Status == (int)DesignTaskStatusEnum.Cancelled)
                throw new BadRequestException("Cannot assign a finalized task");

            await EnsureAssignableStaffAsync(nursery.Id, request.AssignedStaffId);

            var trackedRegistration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(taskDetail.DesignRegistrationId)
                ?? throw new NotFoundException($"DesignRegistration {taskDetail.DesignRegistrationId} not found");

            trackedRegistration.AssignedCaretakerId = request.AssignedStaffId;
            _unitOfWork.DesignRegistrationRepository.PrepareUpdate(trackedRegistration);

            var registrationTasks = await _unitOfWork.DesignTaskRepository.GetByRegistrationIdAsync(taskDetail.DesignRegistrationId);
            foreach (var registrationTask in registrationTasks)
            {
                if (registrationTask.Status == (int)DesignTaskStatusEnum.Completed
                    || registrationTask.Status == (int)DesignTaskStatusEnum.Cancelled)
                    continue;

                var trackedTaskInRegistration = await _unitOfWork.DesignTaskRepository.GetByIdAsync(registrationTask.Id);
                if (trackedTaskInRegistration == null)
                    continue;

                trackedTaskInRegistration.AssignedStaffId = request.AssignedStaffId;

                if (trackedTaskInRegistration.Status == (int)DesignTaskStatusEnum.Pending)
                {
                    trackedTaskInRegistration.Status = (int)DesignTaskStatusEnum.Assigned;
                }

                if (trackedTaskInRegistration.Id == taskId && request.ScheduledDate.HasValue)
                {
                    trackedTaskInRegistration.ScheduledDate = request.ScheduledDate;
                }

                _unitOfWork.DesignTaskRepository.PrepareUpdate(trackedTaskInRegistration);
            }

            var task = await _unitOfWork.DesignTaskRepository.GetByIdAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found");

            task.AssignedStaffId = request.AssignedStaffId;
            if (request.ScheduledDate.HasValue)
            {
                task.ScheduledDate = request.ScheduledDate;
            }

            if (task.Status == (int)DesignTaskStatusEnum.Pending)
            {
                task.Status = (int)DesignTaskStatusEnum.Assigned;
            }

            _unitOfWork.DesignTaskRepository.PrepareUpdate(task);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.DesignTaskRepository.GetByIdWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found after assign");

            return MapToDto(updated);
        }

        public async Task<DesignTaskResponseDto> UpdateStatusAsync(int userId, int taskId, UpdateDesignTaskStatusRequestDto request, IFormFile? reportImage = null)
        {
            if (request == null)
                throw new BadRequestException("Request body is required");

            if (!Enum.IsDefined(typeof(DesignTaskStatusEnum), request.Status))
                throw new BadRequestException("Invalid DesignTask status value");

            var nextStatus = (DesignTaskStatusEnum)request.Status;

            var taskDetail = await _unitOfWork.DesignTaskRepository.GetByIdWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found");

            var currentStatus = (DesignTaskStatusEnum)taskDetail.Status;

            if (nextStatus == DesignTaskStatusEnum.Cancelled)
            {
                var nursery = await ResolveOperatorNurseryAsync(userId);
                if (taskDetail.DesignRegistration.NurseryId != nursery.Id)
                    throw new ForbiddenException("This task does not belong to your nursery");
            }
            else
            {
                if (taskDetail.AssignedStaffId != userId)
                    throw new ForbiddenException("Only assigned staff can update this task status");
            }

            var task = await _unitOfWork.DesignTaskRepository.GetByIdAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found");

            switch (nextStatus)
            {
                case DesignTaskStatusEnum.Pending:
                    throw new BadRequestException("Cannot manually set task back to Pending");

                case DesignTaskStatusEnum.Assigned:
                    if (currentStatus != DesignTaskStatusEnum.Pending)
                        throw new BadRequestException("Only pending tasks can move to Assigned");

                    if (!task.AssignedStaffId.HasValue)
                        throw new BadRequestException("Task must be assigned before starting");

                    if (task.ScheduledDate.HasValue && task.ScheduledDate.Value > DateOnly.FromDateTime(DateTime.Today))
                        throw new BadRequestException("Cannot mark task as assigned before scheduled date");

                    task.Status = (int)DesignTaskStatusEnum.Assigned;
                    break;

                case DesignTaskStatusEnum.Completed:
                    if (currentStatus != DesignTaskStatusEnum.Assigned)
                        throw new BadRequestException("Only assigned tasks can be completed");

                    if (reportImage == null)
                        throw new BadRequestException("Report image is required to complete task");

                    var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(reportImage);
                    if (!isValid)
                        throw new BadRequestException(errorMessage);

                    var uploadResult = await _cloudinaryService.UploadFileAsync(reportImage, "DesignTaskReport");
                    task.ReportImageUrl = uploadResult.SecureUrl;
                    task.Status = (int)DesignTaskStatusEnum.Completed;
                    break;

                case DesignTaskStatusEnum.Cancelled:
                    if (currentStatus == DesignTaskStatusEnum.Completed || currentStatus == DesignTaskStatusEnum.Cancelled)
                        throw new BadRequestException("Finalized task cannot be cancelled");

                    task.Status = (int)DesignTaskStatusEnum.Cancelled;
                    break;

                default:
                    throw new BadRequestException("Unsupported status transition");
            }

            _unitOfWork.DesignTaskRepository.PrepareUpdate(task);
            await _unitOfWork.SaveAsync();

            await SyncRegistrationCompletionStatusAsync(task.DesignRegistrationId);

            var updated = await _unitOfWork.DesignTaskRepository.GetByIdWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found after status update");

            return MapToDto(updated);
        }

        private async Task EnsureCanAccessTaskAsync(int userId, DesignTask task)
        {
            if (task.AssignedStaffId == userId
                || task.DesignRegistration.UserId == userId
                || task.DesignRegistration.AssignedCaretakerId == userId)
                return;

            var operatorNursery = await TryResolveOperatorNurseryAsync(userId);
            if (operatorNursery?.Id == task.DesignRegistration.NurseryId)
                return;

            throw new ForbiddenException("You don't have access to this task");
        }

        private async Task EnsureAssignableStaffAsync(int nurseryId, int staffId)
        {
            var staff = await _unitOfWork.UserRepository.GetStaffOrCaretakerByIdWithSpecializationsAsync(staffId, nurseryId);
            if (staff == null)
                throw new NotFoundException($"Staff/Caretaker {staffId} not found in your nursery");

            if (staff.Status != (int)UserStatusEnum.Active || !staff.IsVerified)
                throw new BadRequestException("Assigned staff must be active and verified");
        }

        private async Task SyncRegistrationCompletionStatusAsync(int registrationId)
        {
            var tasks = await _unitOfWork.DesignTaskRepository.GetByRegistrationIdAsync(registrationId);
            if (!tasks.Any())
                return;

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(registrationId);
            if (registration == null)
                return;

            if (tasks.All(x => x.Status == (int)DesignTaskStatusEnum.Completed))
            {
                registration.Status = (int)DesignRegistrationStatus.Completed;
                _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);
                await _unitOfWork.SaveAsync();
            }
        }

        private async Task<Nursery> ResolveOperatorNurseryAsync(int operatorId)
        {
            var nursery = await TryResolveOperatorNurseryAsync(operatorId);
            if (nursery != null)
                return nursery;

            throw new ForbiddenException("You are not a manager/staff of any nursery");
        }

        private async Task<Nursery?> TryResolveOperatorNurseryAsync(int operatorId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(operatorId);
            if (nursery != null)
                return nursery;

            var user = await _unitOfWork.UserRepository.GetByIdAsync(operatorId);
            if (user?.RoleId == (int)RoleEnum.Staff && user.NurseryId.HasValue)
            {
                var staffNursery = await _unitOfWork.NurseryRepository.GetByIdAsync(user.NurseryId.Value);
                if (staffNursery != null)
                    return staffNursery;
            }

            return null;
        }

        public static DesignTaskResponseDto MapToDto(DesignTask task)
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
                AssignedStaff = task.AssignedStaff == null ? null : ServiceRegistrationService.MapUserSummary(task.AssignedStaff),
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
