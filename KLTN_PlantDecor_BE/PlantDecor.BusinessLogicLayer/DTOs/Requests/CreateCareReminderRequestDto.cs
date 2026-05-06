namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateCareReminderRequestDto
    {
        public int? UserPlantId { get; set; }
        public int? CareType { get; set; }
        public string? Content { get; set; }
        public DateOnly? ReminderDate { get; set; }
    }
}
