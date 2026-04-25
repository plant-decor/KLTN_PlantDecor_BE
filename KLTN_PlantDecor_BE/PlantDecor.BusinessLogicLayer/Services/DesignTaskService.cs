using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
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
            return task.ToResponse();
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
            return tasks.Select(x => x.ToResponse()).ToList();
        }

        public async Task<List<DesignTaskPackageMaterialResponseDto>> GetPackageMaterialsForTaskAsync(int userId, int taskId)
        {
            var task = await _unitOfWork.DesignTaskRepository.GetByIdWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found");

            if (task.AssignedStaffId != userId)
                throw new ForbiddenException("Only assigned staff can view package materials for this task");

            var tierItems = await _unitOfWork.DesignTemplateTierItemRepository
                .GetByTierIdAsync(task.DesignRegistration.DesignTemplateTierId);

            var materialRequirements = tierItems
                .Where(i => i.MaterialId.HasValue && i.Quantity > 0)
                .GroupBy(i => i.MaterialId!.Value)
                .Select(g => new
                {
                    MaterialId = g.Key,
                    SuggestedQuantity = g.Sum(x => x.Quantity),
                    MaterialName = g
                        .Select(x => x.Material?.Name)
                        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
                })
                .OrderBy(x => x.MaterialId)
                .ToList();

            var response = new List<DesignTaskPackageMaterialResponseDto>();

            foreach (var item in materialRequirements)
            {
                var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository
                    .GetByMaterialAndNurseryAsync(item.MaterialId, task.DesignRegistration.NurseryId);

                var materialName = item.MaterialName;
                if (string.IsNullOrWhiteSpace(materialName))
                {
                    var material = await _unitOfWork.MaterialRepository.GetByIdAsync(item.MaterialId);
                    materialName = material?.Name;
                }

                response.Add(new DesignTaskPackageMaterialResponseDto
                {
                    MaterialId = item.MaterialId,
                    MaterialName = materialName,
                    SuggestedQuantity = item.SuggestedQuantity,
                    AvailableQuantity = nurseryMaterial?.Quantity ?? 0,
                    IsAvailableInNursery = nurseryMaterial != null,
                    IsActiveInNursery = nurseryMaterial?.IsActive == true
                });
            }

            return response;
        }

        public async Task<PaginatedResult<DesignTaskResponseDto>> GetMyTasksAsync(
            int userId,
            Pagination pagination,
            int? status = null,
            DateOnly? from = null,
            DateOnly? to = null)
        {
            if (from.HasValue && to.HasValue && to.Value < from.Value)
                throw new BadRequestException("'to' date must be greater than or equal to 'from' date");

            var result = await _unitOfWork.DesignTaskRepository
                .GetByAssignedStaffIdAsync(userId, pagination, status, from, to);
            return new PaginatedResult<DesignTaskResponseDto>(
                result.Items.Select(x => x.ToResponse()).ToList(),
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

            return updated.ToResponse();
        }

        public async Task<DesignTaskResponseDto> ReportMaterialUsageAsync(int userId, int taskId, ReportDesignTaskMaterialUsageRequestDto request)
        {
            if (request == null)
                throw new BadRequestException("Request body is required");

            var taskDetail = await _unitOfWork.DesignTaskRepository.GetByIdWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found");

            if (taskDetail.AssignedStaffId != userId)
                throw new ForbiddenException("Only assigned staff can report material usage for this task");

            if (taskDetail.Status == (int)DesignTaskStatusEnum.Cancelled)
                throw new BadRequestException("Cannot report material usage for a cancelled task");

            if (taskDetail.Status != (int)DesignTaskStatusEnum.Assigned
                && taskDetail.Status != (int)DesignTaskStatusEnum.Completed)
            {
                throw new BadRequestException("Material usage can only be reported when task is Assigned or Completed");
            }

            var usageItems = request.MaterialUsages ?? new List<ReportDesignTaskMaterialUsageItemDto>();
            var duplicateMaterialIds = usageItems
                .GroupBy(x => x.MaterialId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateMaterialIds.Any())
                throw new BadRequestException("MaterialId must be unique in a report");

            var nurseryId = taskDetail.DesignRegistration.NurseryId;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingUsages = await _unitOfWork.TaskMaterialUsageRepository.GetByTaskIdAsync(taskId);

                foreach (var existing in existingUsages)
                {
                    if (existing.ActualQuantity.HasValue && existing.ActualQuantity.Value > 0)
                    {
                        var rollbackQty = ConvertToInventoryUnits(existing.ActualQuantity.Value);
                        var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository
                            .GetByMaterialAndNurseryAsync(existing.MaterialId, nurseryId);

                        if (nurseryMaterial != null)
                        {
                            nurseryMaterial.Quantity += rollbackQty;
                            _unitOfWork.NurseryMaterialRepository.PrepareUpdate(nurseryMaterial);
                        }
                    }

                    _unitOfWork.TaskMaterialUsageRepository.PrepareRemove(existing);
                }

                foreach (var item in usageItems)
                {
                    if (item.MaterialId <= 0)
                        throw new BadRequestException("MaterialId must be greater than 0");

                    if (item.ActualQuantity <= 0)
                        throw new BadRequestException("ActualQuantity must be greater than 0");

                    var quantity = ConvertToInventoryUnits(item.ActualQuantity);
                    var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository
                        .GetByMaterialAndNurseryAsync(item.MaterialId, nurseryId)
                        ?? throw new BadRequestException($"Material {item.MaterialId} is not available in nursery inventory");

                    if (!nurseryMaterial.IsActive)
                        throw new BadRequestException($"Material {item.MaterialId} is inactive in nursery inventory");

                    if (nurseryMaterial.Quantity < quantity)
                        throw new BadRequestException($"Insufficient stock for material {item.MaterialId}");

                    nurseryMaterial.Quantity -= quantity;
                    _unitOfWork.NurseryMaterialRepository.PrepareUpdate(nurseryMaterial);

                    _unitOfWork.TaskMaterialUsageRepository.PrepareCreate(new TaskMaterialUsage
                    {
                        DesignTaskId = taskId,
                        MaterialId = item.MaterialId,
                        ActualQuantity = item.ActualQuantity,
                        Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim(),
                        CreatedAt = DateTime.Now
                    });
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var updated = await _unitOfWork.DesignTaskRepository.GetByIdWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found after reporting material usage");

            return updated.ToResponse();
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
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.SaveAsync();
                await SyncRegistrationCompletionStatusAsync(task.DesignRegistrationId);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var updated = await _unitOfWork.DesignTaskRepository.GetByIdWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"DesignTask {taskId} not found after status update");

            return updated.ToResponse();
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
                if (registration.Status == (int)DesignRegistrationStatus.Completed)
                    return;

                await DeductTierPlantsForCompletedRegistrationAsync(registration);

                registration.Status = (int)DesignRegistrationStatus.Completed;
                _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);
                await _unitOfWork.SaveAsync();
            }
        }

        private async Task DeductTierPlantsForCompletedRegistrationAsync(DesignRegistration registration)
        {
            var tierItems = await _unitOfWork.DesignTemplateTierItemRepository
                .GetByTierIdAsync(registration.DesignTemplateTierId);

            var plantRequirements = tierItems
                .Where(i => i.PlantId.HasValue && i.Quantity > 0)
                .GroupBy(i => i.PlantId!.Value)
                .Select(g => new
                {
                    PlantId = g.Key,
                    RequiredQuantity = ConvertToInventoryUnits(g.Sum(x => x.Quantity))
                })
                .ToList();

            foreach (var item in plantRequirements)
            {
                var commonPlant = await _unitOfWork.CommonPlantRepository
                    .GetByPlantAndNurseryAsync(item.PlantId, registration.NurseryId)
                    ?? throw new BadRequestException($"Plant {item.PlantId} is not available in nursery inventory");

                if (!commonPlant.IsActive)
                    throw new BadRequestException($"Plant {item.PlantId} is inactive in nursery inventory");

                if (commonPlant.Quantity < item.RequiredQuantity)
                    throw new BadRequestException($"Insufficient stock for plant {item.PlantId}");

                commonPlant.Quantity -= item.RequiredQuantity;
                _unitOfWork.CommonPlantRepository.PrepareUpdate(commonPlant);
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

        private static int ConvertToInventoryUnits(decimal quantity)
        {
            if (quantity <= 0)
                throw new BadRequestException("ActualQuantity must be greater than 0");

            if (decimal.Truncate(quantity) != quantity)
                throw new BadRequestException("ActualQuantity must be a whole number to match inventory unit");

            if (quantity > int.MaxValue)
                throw new BadRequestException("ActualQuantity is too large");

            return (int)quantity;
        }

    }
}
