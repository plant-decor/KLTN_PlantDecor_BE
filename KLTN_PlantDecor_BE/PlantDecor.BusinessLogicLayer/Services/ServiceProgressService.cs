using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ServiceProgressService : IServiceProgressService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public ServiceProgressService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ServiceProgressResponseDto> GetByIdAsync(int progressId, int userId)
        {
            var progress = await _unitOfWork.ServiceProgressRepository.GetByIdWithDetailsAsync(progressId);
            if (progress == null)
                throw new NotFoundException($"ServiceProgress {progressId} not found");

            var registration = progress.ServiceRegistration;

            var isCaretaker = progress.CaretakerId == userId;
            var isCustomer = registration?.UserId == userId;
            var isMainCaretaker = registration?.MainCaretakerId == userId;
            var isCurrentCaretaker = registration?.CurrentCaretakerId == userId;

            if (!isCaretaker && !isCustomer && !isMainCaretaker && !isCurrentCaretaker)
            {
                var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(userId);
                var isManager = nursery != null &&
                    registration?.NurseryCareService?.NurseryId == nursery.Id;
                if (!isManager)
                    throw new ForbiddenException("You don't have access to this progress");
            }

            return MapToDto(progress);
        }

        public async Task<List<ServiceProgressResponseDto>> GetTodayScheduleAsync(int caretakerId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var progresses = await _unitOfWork.ServiceProgressRepository.GetByCaretakerAndDateAsync(caretakerId, today);
            return progresses.Select(MapToDto).ToList();
        }

        public async Task<List<ServiceProgressResponseDto>> GetByRegistrationIdAsync(int registrationId, int userId)
        {
            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(registrationId);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {registrationId} not found");

            if (registration.UserId != userId &&
                registration.MainCaretakerId != userId &&
                registration.CurrentCaretakerId != userId)
                throw new ForbiddenException("You don't have access to this registration");

            var progresses = await _unitOfWork.ServiceProgressRepository.GetByServiceRegistrationIdAsync(registrationId);
            return progresses.Select(MapToDto).ToList();
        }

        public async Task<ServiceProgressResponseDto> CheckInAsync(int caretakerId, int progressId)
        {
            var progress = await _unitOfWork.ServiceProgressRepository.GetByIdWithDetailsAsync(progressId);
            if (progress == null)
                throw new NotFoundException($"ServiceProgress {progressId} not found");

            if (progress.CaretakerId != caretakerId)
                throw new ForbiddenException("This task is not assigned to you");

            if (progress.Status != (int)ServiceProgressStatusEnum.Assigned &&
                progress.Status != (int)ServiceProgressStatusEnum.Pending)
                throw new BadRequestException("Task is not in a state that allows check-in");

            progress.Status = (int)ServiceProgressStatusEnum.InProgress;
            progress.ActualStartTime = DateTime.Now;
            _unitOfWork.ServiceProgressRepository.PrepareUpdate(progress);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.ServiceProgressRepository.GetByIdWithDetailsAsync(progressId);
            return MapToDto(updated!);
        }

        public async Task<ServiceProgressResponseDto> CheckOutAsync(int caretakerId, int progressId, CheckOutRequestDto request, IFormFile? evidenceImage)
        {
            var progress = await _unitOfWork.ServiceProgressRepository.GetByIdWithDetailsAsync(progressId);
            if (progress == null)
                throw new NotFoundException($"ServiceProgress {progressId} not found");

            if (progress.CaretakerId != caretakerId)
                throw new ForbiddenException("This task is not assigned to you");

            if (progress.Status != (int)ServiceProgressStatusEnum.InProgress)
                throw new BadRequestException("Task is not in progress");

            string? evidenceImageUrl = null;
            if (evidenceImage != null)
            {
                var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(evidenceImage);
                if (!isValid)
                    throw new BadRequestException(errorMessage);

                var uploadResult = await _cloudinaryService.UploadFileAsync(evidenceImage, "ServiceProgress");
                evidenceImageUrl = uploadResult.SecureUrl;
            }

            progress.Status = (int)ServiceProgressStatusEnum.Completed;
            progress.ActualEndTime = DateTime.Now;
            progress.Description = request.Description;
            progress.EvidenceImageUrl = evidenceImageUrl;
            _unitOfWork.ServiceProgressRepository.PrepareUpdate(progress);
            await _unitOfWork.SaveAsync();

            // Tự động hoàn thành ServiceRegistration nếu tất cả session đã xong
            if (progress.ServiceRegistrationId.HasValue)
            {
                var allProgresses = await _unitOfWork.ServiceProgressRepository
                    .GetByServiceRegistrationIdAsync(progress.ServiceRegistrationId.Value);
                var allDone = allProgresses.All(p =>
                    p.Status == (int)ServiceProgressStatusEnum.Completed ||
                    p.Status == (int)ServiceProgressStatusEnum.Cancelled);
                var hasCompleted = allProgresses.Any(p => p.Status == (int)ServiceProgressStatusEnum.Completed);
                if (allDone && hasCompleted)
                {
                    var registration = await _unitOfWork.ServiceRegistrationRepository
                        .GetByIdWithDetailsAsync(progress.ServiceRegistrationId.Value);
                    if (registration != null && registration.Status == (int)ServiceRegistrationStatusEnum.Active)
                    {
                        registration.Status = (int)ServiceRegistrationStatusEnum.Completed;
                        _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);

                        // Cập nhật Order sang Completed
                        if (registration.OrderId.HasValue)
                        {
                            var order = await _unitOfWork.OrderRepository.GetByIdAsync(registration.OrderId.Value);
                            if (order != null)
                            {
                                order.Status = (int)OrderStatusEnum.Completed;
                                order.UpdatedAt = DateTime.Now;
                                _unitOfWork.OrderRepository.PrepareUpdate(order);
                            }
                        }

                        await _unitOfWork.SaveAsync();
                    }
                }
            }

            var updated = await _unitOfWork.ServiceProgressRepository.GetByIdWithDetailsAsync(progressId);
            return MapToDto(updated!);
        }

        public async Task<ServiceProgressResponseDto> ReassignCaretakerAsync(int managerId, int progressId, int newCaretakerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var progress = await _unitOfWork.ServiceProgressRepository.GetByIdWithDetailsAsync(progressId);
            if (progress == null)
                throw new NotFoundException($"ServiceProgress {progressId} not found");

            if (progress.ServiceRegistration?.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This task does not belong to your nursery");

            if (progress.Status == (int)ServiceProgressStatusEnum.Completed ||
                progress.Status == (int)ServiceProgressStatusEnum.Cancelled)
                throw new BadRequestException("Cannot reassign a completed or cancelled task");

            var newCaretaker = await _unitOfWork.UserRepository.GetByIdAsync(newCaretakerId);
            if (newCaretaker == null)
                throw new NotFoundException($"User {newCaretakerId} not found");

            if (newCaretaker.RoleId != (int)RoleEnum.Caretaker)
                throw new BadRequestException("Selected user is not a caretaker");

            if (newCaretaker.Status != (int)UserStatusEnum.Active || !newCaretaker.IsVerified)
                throw new BadRequestException("Caretaker account is not active or verified");

            if (!newCaretaker.NurseryId.HasValue || newCaretaker.NurseryId.Value != nursery.Id)
                throw new ForbiddenException("Caretaker is not assigned to your nursery");

            progress.CaretakerId = newCaretakerId;
            progress.Status = (int)ServiceProgressStatusEnum.Assigned;
            _unitOfWork.ServiceProgressRepository.PrepareUpdate(progress);

            if (progress.ServiceRegistration != null)
            {
                progress.ServiceRegistration.CurrentCaretakerId = newCaretakerId;
                _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(progress.ServiceRegistration);
            }

            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.ServiceProgressRepository.GetByIdWithDetailsAsync(progressId);
            return MapToDto(updated!);
        }

        public async Task<List<ServiceProgressResponseDto>> GetNurseryScheduleAsync(int managerId, DateOnly date)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var progresses = await _unitOfWork.ServiceProgressRepository.GetByNurseryAndDateAsync(nursery.Id, date);
            return progresses.Select(MapToDto).ToList();
        }

        public async Task<List<ServiceProgressResponseDto>> GetCaretakerScheduleAsync(int managerId, int caretakerId, DateOnly from, DateOnly to)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            if (to < from)
                throw new BadRequestException("'to' date must be >= 'from' date");

            if ((to.ToDateTime(TimeOnly.MinValue) - from.ToDateTime(TimeOnly.MinValue)).TotalDays > 90)
                throw new BadRequestException("Date range cannot exceed 90 days");

            var progresses = await _unitOfWork.ServiceProgressRepository.GetByCaretakerAndDateRangeAsync(nursery.Id, caretakerId, from, to);
            return progresses.Select(MapToDto).ToList();
        }

        public async Task<List<ServiceProgressResponseDto>> GetMyScheduleAsync(int caretakerId, DateOnly from, DateOnly to)
        {
            if (to < from)
                throw new BadRequestException("'to' date must be >= 'from' date");

            if ((to.ToDateTime(TimeOnly.MinValue) - from.ToDateTime(TimeOnly.MinValue)).TotalDays > 90)
                throw new BadRequestException("Date range cannot exceed 90 days");

            var progresses = await _unitOfWork.ServiceProgressRepository.GetByCaretakerSelfDateRangeAsync(caretakerId, from, to);
            return progresses.Select(MapToDto).ToList();
        }

        #region Mapping

        public static ServiceProgressResponseDto MapToDto(ServiceProgress sp)
        {
            return new ServiceProgressResponseDto
            {
                Id = sp.Id,
                ServiceRegistrationId = sp.ServiceRegistrationId,
                Status = sp.Status,
                StatusName = sp.Status.HasValue ? ((ServiceProgressStatusEnum)sp.Status.Value).ToString() : null,
                TaskDate = sp.TaskDate,
                ActualStartTime = sp.ActualStartTime,
                ActualEndTime = sp.ActualEndTime,
                Description = sp.Description,
                EvidenceImageUrl = sp.EvidenceImageUrl,
                Shift = sp.Shift == null ? null : new ShiftSummaryDto
                {
                    Id = sp.Shift.Id,
                    ShiftName = sp.Shift.ShiftName,
                    StartTime = sp.Shift.StartTime,
                    EndTime = sp.Shift.EndTime
                },
                Caretaker = sp.Caretaker == null ? null : ServiceRegistrationService.MapUserSummary(sp.Caretaker),
                ServiceRegistration = sp.ServiceRegistration == null ? null : new ServiceRegistrationBriefDto
                {
                    Id = sp.ServiceRegistration.Id,
                    Address = sp.ServiceRegistration.Address,
                    Phone = sp.ServiceRegistration.Phone,
                    NurseryCareService = sp.ServiceRegistration.NurseryCareService == null ? null : new NurseryCareServiceSummaryDto
                    {
                        Id = sp.ServiceRegistration.NurseryCareService.Id,
                        NurseryId = sp.ServiceRegistration.NurseryCareService.NurseryId,
                        NurseryName = sp.ServiceRegistration.NurseryCareService.Nursery?.Name,
                        CareServicePackage = sp.ServiceRegistration.NurseryCareService.CareServicePackage == null ? null : new CareServicePackageSummaryDto
                        {
                            Id = sp.ServiceRegistration.NurseryCareService.CareServicePackage.Id,
                            Name = sp.ServiceRegistration.NurseryCareService.CareServicePackage.Name,
                            Description = sp.ServiceRegistration.NurseryCareService.CareServicePackage.Description,
                            VisitPerWeek = sp.ServiceRegistration.NurseryCareService.CareServicePackage.VisitPerWeek,
                            DurationDays = sp.ServiceRegistration.NurseryCareService.CareServicePackage.DurationDays,
                            ServiceType = sp.ServiceRegistration.NurseryCareService.CareServicePackage.ServiceType,
                            UnitPrice = sp.ServiceRegistration.NurseryCareService.CareServicePackage.UnitPrice,
                        }
                    },
                    Customer = sp.ServiceRegistration.User == null ? null : ServiceRegistrationService.MapUserSummary(sp.ServiceRegistration.User)
                }
            };
        }

        #endregion
    }
}
