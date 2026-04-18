namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class AssignDesignTaskRequestDto
    {
        public int AssignedStaffId { get; set; }
        public DateOnly? ScheduledDate { get; set; }
    }
}
