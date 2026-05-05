using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class CareReminderService : ICareReminderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CareReminderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<CareReminderResponseDto> CreateForUserAsync(int userId, CreateCareReminderRequestDto request)
        {
            if (!request.UserPlantId.HasValue)
            {
                throw new BadRequestException("UserPlantId is required");
            }

            await EnsureUserPlantOwnedByUserAsync(userId, request.UserPlantId.Value);

            ValidateDates(request.ReminderDate);

            var reminder = new CareReminder
            {
                UserPlantId = request.UserPlantId,
                CareType = request.CareType,
                Content = NormalizeOptional(request.Content),
                ReminderDate = request.ReminderDate,
                ScheduledDate = null,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.CareReminderRepository.PrepareCreate(reminder);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.CareReminderRepository.GetByIdWithDetailsAsync(reminder.Id);
            return created?.ToResponse() ?? reminder.ToResponse();
        }


        public async Task<CareReminderResponseDto> UpdateForUserAsync(int userId, int id, UpdateCareReminderRequestDto request)
        {
            var reminder = await _unitOfWork.CareReminderRepository.GetByIdAsync(id);
            if (reminder == null)
            {
                throw new NotFoundException($"CareReminder {id} not found");
            }

            if (!reminder.UserPlantId.HasValue)
            {
                throw new ForbiddenException("You do not have access to this care reminder");
            }

            await EnsureUserPlantOwnedByUserAsync(userId, reminder.UserPlantId.Value);

            if (request.UserPlantId.HasValue && request.UserPlantId.Value != reminder.UserPlantId.Value)
            {
                await EnsureUserPlantOwnedByUserAsync(userId, request.UserPlantId.Value);
                reminder.UserPlantId = request.UserPlantId;
            }

            if (request.CareType.HasValue)
            {
                reminder.CareType = request.CareType;
            }

            if (request.Content != null)
            {
                reminder.Content = NormalizeOptional(request.Content);
            }

            if (request.ReminderDate.HasValue)
            {
                reminder.ReminderDate = request.ReminderDate;
            }

            reminder.ScheduledDate = null;

            ValidateDates(reminder.ReminderDate);

            _unitOfWork.CareReminderRepository.PrepareUpdate(reminder);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.CareReminderRepository.GetByIdWithDetailsAsync(id);
            return updated?.ToResponse() ?? reminder.ToResponse();
        }

        public async Task DeleteAsync(int id)
        {
            var reminder = await _unitOfWork.CareReminderRepository.GetByIdAsync(id);
            if (reminder == null)
            {
                throw new NotFoundException($"CareReminder {id} not found");
            }

            _unitOfWork.CareReminderRepository.PrepareRemove(reminder);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteForUserAsync(int userId, int id)
        {
            var reminder = await _unitOfWork.CareReminderRepository.GetByIdAsync(id);
            if (reminder == null)
            {
                throw new NotFoundException($"CareReminder {id} not found");
            }

            if (!reminder.UserPlantId.HasValue)
            {
                throw new ForbiddenException("You do not have access to this care reminder");
            }

            await EnsureUserPlantOwnedByUserAsync(userId, reminder.UserPlantId.Value);

            _unitOfWork.CareReminderRepository.PrepareRemove(reminder);
            await _unitOfWork.SaveAsync();
        }

        private async Task EnsureUserPlantExistsAsync(int userPlantId)
        {
            var userPlant = await _unitOfWork.UserPlantRepository.GetByIdAsync(userPlantId);
            if (userPlant == null)
            {
                throw new NotFoundException($"UserPlant {userPlantId} not found");
            }
        }

        private async Task EnsureUserPlantOwnedByUserAsync(int userId, int userPlantId)
        {
            var userPlant = await _unitOfWork.UserPlantRepository.GetByIdAsync(userPlantId);
            if (userPlant == null)
            {
                throw new NotFoundException($"UserPlant {userPlantId} not found");
            }

            if (userPlant.UserId != userId)
            {
                throw new ForbiddenException("You do not have access to this user plant");
            }
        }

        private static void ValidateDates(DateOnly? reminderDate)
        {
            if (!reminderDate.HasValue)
            {
                throw new BadRequestException("ReminderDate is required");
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (reminderDate.Value < today)
            {
                throw new BadRequestException("ReminderDate must be in the future");
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
