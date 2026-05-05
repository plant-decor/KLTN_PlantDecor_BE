using System;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class CareReminderMapper
    {
        public static CareReminderResponseDto ToResponse(this CareReminder reminder)
        {
            var plant = reminder.UserPlant?.PlantInstance?.Plant ?? reminder.UserPlant?.Plant;

            return new CareReminderResponseDto
            {
                Id = reminder.Id,
                UserPlantId = reminder.UserPlantId,
                UserId = reminder.UserPlant?.UserId,
                CareType = reminder.CareType,
                CareTypeName = ResolveCareTypeName(reminder.CareType),
                PlantName = plant?.Name,
                Content = reminder.Content,
                ReminderDate = reminder.ReminderDate,
                ScheduledDate = reminder.ScheduledDate,
                CreatedAt = reminder.CreatedAt
            };
        }

        public static CareReminderNotificationResponseDto ToNotificationResponse(this CareReminder reminder)
        {
            var plant = reminder.UserPlant?.PlantInstance?.Plant ?? reminder.UserPlant?.Plant;
            var plantName = plant?.Name;
            var title = "Plant care reminder";
            var message = string.IsNullOrWhiteSpace(reminder.Content)
                ? BuildGenericMessage(plantName, reminder.ReminderDate)
                : reminder.Content;

            return new CareReminderNotificationResponseDto
            {
                Id = reminder.Id,
                UserPlantId = reminder.UserPlantId,
                CareType = reminder.CareType,
                CareTypeName = ResolveCareTypeName(reminder.CareType),
                PlantName = plantName,
                Title = title,
                Message = message,
                ReminderDate = reminder.ReminderDate,
                ScheduledDate = reminder.ScheduledDate,
                CreatedAt = reminder.CreatedAt
            };
        }

        private static string BuildGenericMessage(string? plantName, DateOnly? reminderDate)
        {
            var dateText = reminderDate.HasValue ? reminderDate.Value.ToString("yyyy-MM-dd") : null;

            if (!string.IsNullOrWhiteSpace(plantName) && !string.IsNullOrWhiteSpace(dateText))
            {
                return $"It's time to care for {plantName} on {dateText}.";
            }

            if (!string.IsNullOrWhiteSpace(plantName))
            {
                return $"It's time to care for {plantName}.";
            }

            if (!string.IsNullOrWhiteSpace(dateText))
            {
                return $"It's time to care for your plant on {dateText}.";
            }

            return "It's time to care for your plant.";
        }

        private static string ResolveCareTypeName(int? careType)
        {
            return careType switch
            {
                1 => "Watering",
                2 => "Fertilizing",
                3 => "Pruning",
                4 => "Misting",
                5 => "Cleaning",
                _ => "Plant care"
            };
        }
    }
}
