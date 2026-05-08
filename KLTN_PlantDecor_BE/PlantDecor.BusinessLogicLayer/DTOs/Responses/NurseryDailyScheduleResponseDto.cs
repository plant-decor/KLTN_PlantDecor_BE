namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class NurseryDailyScheduleResponseDto
    {
        public DateOnly Date { get; set; }
        public List<ServiceProgressResponseDto> ServiceProgresses { get; set; } = new();
        public List<DesignTaskResponseDto> DesignTasks { get; set; } = new();
    }
}
