using System.Text.Json;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

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

            var progresses = new List<ServiceProgress>();

            if (isOneTime)
            {
                // Dịch vụ one-time: tạo đúng 1 ServiceProgress vào ngày ServiceDate
                progresses.Add(new ServiceProgress
                {
                    ServiceRegistrationId = registration.Id,
                    CaretakerId = registration.MainCaretakerId,
                    ShiftId = registration.PreferredShiftId.Value,
                    TaskDate = registration.ServiceDate.Value,
                    Status = registration.MainCaretakerId.HasValue
                        ? (int)ServiceProgressStatusEnum.Assigned
                        : (int)ServiceProgressStatusEnum.Pending
                });
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
                        progresses.Add(new ServiceProgress
                        {
                            ServiceRegistrationId = registration.Id,
                            CaretakerId = registration.MainCaretakerId,
                            ShiftId = registration.PreferredShiftId.Value,
                            TaskDate = current,
                            Status = registration.MainCaretakerId.HasValue
                                ? (int)ServiceProgressStatusEnum.Assigned
                                : (int)ServiceProgressStatusEnum.Pending
                        });
                        generated++;
                    }
                    current = current.AddDays(1);
                }
            }

            foreach (var progress in progresses)
                _unitOfWork.ServiceProgressRepository.PrepareCreate(progress);

            registration.Status = (int)ServiceRegistrationStatusEnum.Active;
            _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);

            await _unitOfWork.SaveAsync();

            _logger.LogInformation("GenerateServiceSchedule: Created {Count} sessions for registration {Id}", progresses.Count, serviceRegistrationId);
        }
    }
}
