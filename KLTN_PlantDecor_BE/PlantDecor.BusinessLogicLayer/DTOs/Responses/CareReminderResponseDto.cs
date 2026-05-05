namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class CareReminderResponseDto
    {
        public int Id { get; set; }
        public int? UserPlantId { get; set; }
        public int? UserId { get; set; }
        public int? CareType { get; set; }
        public string? CareTypeName { get; set; }
        public string? PlantName { get; set; }
        public string? Content { get; set; }
        public DateOnly? ReminderDate { get; set; }
        public DateOnly? ScheduledDate { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
