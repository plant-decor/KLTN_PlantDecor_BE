namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class CareReminderNotificationResponseDto
    {
        public int Id { get; set; }
        public int? UserPlantId { get; set; }
        public int? CareType { get; set; }
        public string? CareTypeName { get; set; }
        public string? PlantName { get; set; }
        public string? PlantImageUrl { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DateOnly? ReminderDate { get; set; }
        public DateOnly? ScheduledDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
