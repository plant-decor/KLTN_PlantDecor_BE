namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class ShiftResponseDto
    {
        public int Id { get; set; }
        public string ShiftName { get; set; } = null!;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}
