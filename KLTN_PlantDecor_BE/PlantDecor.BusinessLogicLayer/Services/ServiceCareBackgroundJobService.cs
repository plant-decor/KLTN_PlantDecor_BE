using System.Text.Json;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Linq;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ServiceCareBackgroundJobService : IServiceCareBackgroundJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ServiceCareBackgroundJobService> _logger;

        public ServiceCareBackgroundJobService(IUnitOfWork unitOfWork, ILogger<ServiceCareBackgroundJobService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task GenerateServiceScheduleAsync(int serviceRegistrationId)
        {
            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(serviceRegistrationId);
            if (registration == null)
            {
                _logger.LogWarning("GenerateServiceSchedule: ServiceRegistration {Id} not found", serviceRegistrationId);
                return;
            }

            if (registration.Status != (int)ServiceRegistrationStatusEnum.AwaitPayment)
            {
                _logger.LogWarning("GenerateServiceSchedule: ServiceRegistration {Id} is not in AwaitPayment status", serviceRegistrationId);
                return;
            }

            if (!registration.ServiceDate.HasValue ||
                !registration.PreferredShiftId.HasValue ||
                !registration.TotalSessions.HasValue)
            {
                _logger.LogError("GenerateServiceSchedule: ServiceRegistration {Id} is missing required fields", serviceRegistrationId);
                return;
            }

            var pkg = registration.NurseryCareService?.CareServicePackage;
            bool isOneTime = pkg?.ServiceType == (int)CareServiceTypeEnum.OneTime;

            var taskDates = new List<DateOnly>();

            if (isOneTime)
            {
                taskDates.Add(registration.ServiceDate.Value);
            }
            else
            {
                if (string.IsNullOrEmpty(registration.ScheduleDaysOfWeek))
                {
                    _logger.LogError("GenerateServiceSchedule: ServiceRegistration {Id} is missing ScheduleDaysOfWeek", serviceRegistrationId);
                    return;
                }

                List<int> scheduleDays;
                try
                {
                    scheduleDays = JsonSerializer.Deserialize<List<int>>(registration.ScheduleDaysOfWeek)
                                   ?? new List<int>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GenerateServiceSchedule: Failed to parse ScheduleDaysOfWeek for registration {Id}", serviceRegistrationId);
                    return;
                }

                if (scheduleDays.Count == 0)
                {
                    _logger.LogError("GenerateServiceSchedule: Empty ScheduleDaysOfWeek for registration {Id}", serviceRegistrationId);
                    return;
                }

                var current = registration.ServiceDate.Value;
                var generated = 0;
                var totalSessions = registration.TotalSessions.Value;

                while (generated < totalSessions)
                {
                    if (scheduleDays.Contains((int)current.DayOfWeek))
                    {
                        taskDates.Add(current);
                        generated++;
                    }
                    current = current.AddDays(1);
                }
            }

            var selectedCaretakerId = await SelectBestCaretakerIdAsync(registration, taskDates);
            registration.MainCaretakerId = selectedCaretakerId;
            registration.CurrentCaretakerId = selectedCaretakerId;

            var progresses = new List<ServiceProgress>();

            foreach (var taskDate in taskDates)
            {
                progresses.Add(new ServiceProgress
                {
                    ServiceRegistrationId = registration.Id,
                    CaretakerId = selectedCaretakerId,
                    ShiftId = registration.PreferredShiftId.Value,
                    TaskDate = taskDate,
                    Status = selectedCaretakerId.HasValue
                        ? (int)ServiceProgressStatusEnum.Assigned
                        : (int)ServiceProgressStatusEnum.Pending
                });
            }

            foreach (var progress in progresses)
                _unitOfWork.ServiceProgressRepository.PrepareCreate(progress);

            registration.Status = (int)ServiceRegistrationStatusEnum.Active;
            _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);

            await _unitOfWork.SaveAsync();

            if (selectedCaretakerId.HasValue)
            {
                _logger.LogInformation(
                    "GenerateServiceSchedule: Created {Count} sessions for registration {Id} and auto-assigned caretaker {CaretakerId}",
                    progresses.Count,
                    serviceRegistrationId,
                    selectedCaretakerId.Value);
            }
            else
            {
                _logger.LogWarning(
                    "GenerateServiceSchedule: Created {Count} sessions for registration {Id} without caretaker assignment",
                    progresses.Count,
                    serviceRegistrationId);
            }
        }

        private async Task<int?> SelectBestCaretakerIdAsync(ServiceRegistration registration, List<DateOnly> taskDates)
        {
            var nurseryId = registration.NurseryCareService?.NurseryId;
            var packageId = registration.NurseryCareService?.CareServicePackageId;

            if (!nurseryId.HasValue || !packageId.HasValue)
            {
                return null;
            }

            var detailedPackage = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(packageId.Value);
            if (detailedPackage == null)
            {
                return null;
            }

            var caretakers = await _unitOfWork.UserRepository.GetCaretakersByNurseryIdAsync(nurseryId.Value);
            var eligible = caretakers
                .Where(u => u.Status == (int)UserStatusEnum.Active && u.IsVerified);

            if (detailedPackage.CareServiceSpecializations != null && detailedPackage.CareServiceSpecializations.Count > 0)
            {
                var requiredSpecIds = detailedPackage.CareServiceSpecializations
                    .Select(cs => cs.SpecializationId)
                    .ToHashSet();

                eligible = eligible.Where(u => requiredSpecIds.All(reqId =>
                    u.StaffSpecializations.Any(ss => ss.SpecializationId == reqId)));
            }

            if (registration.PreferredShiftId.HasValue && taskDates.Count > 0)
            {
                var conflictingIds = await _unitOfWork.ServiceProgressRepository
                    .GetConflictingCaretakerIdsAsync(registration.PreferredShiftId.Value, taskDates);

                eligible = eligible.Where(u => !conflictingIds.Contains(u.Id));
            }

            var eligibleList = eligible.ToList();
            if (!eligibleList.Any())
            {
                return null;
            }

            var workloads = await _unitOfWork.ServiceRegistrationRepository.CountOpenAssignmentsByCaretakerIdsAsync(
                eligibleList.Select(u => u.Id).ToList(),
                nurseryId.Value);

            var selected = eligibleList
                .OrderBy(u => workloads.TryGetValue(u.Id, out var count) ? count : 0)
                .ThenBy(u => u.Username ?? string.Empty)
                .ThenBy(u => u.Id)
                .First();

            return selected.Id;
        }
    }
}
