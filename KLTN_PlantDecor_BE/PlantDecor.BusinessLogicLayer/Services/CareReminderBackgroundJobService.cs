using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class CareReminderBackgroundJobService : ICareReminderBackgroundJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CareReminderBackgroundJobService> _logger;

        public CareReminderBackgroundJobService(IUnitOfWork unitOfWork, ILogger<CareReminderBackgroundJobService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ProcessTodayCareRemindersAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var reminders = await _unitOfWork.CareReminderRepository.GetByReminderDateWithUserPlantAsync(today);
            if (reminders.Count == 0)
            {
                _logger.LogInformation("No care reminders for {Date}", today);
                return;
            }

            _logger.LogInformation("Found {Count} care reminders for {Date}", reminders.Count, today);
        }
    }
}
