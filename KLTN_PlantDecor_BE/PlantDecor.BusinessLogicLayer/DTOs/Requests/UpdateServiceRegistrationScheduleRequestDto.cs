namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class UpdateServiceRegistrationScheduleRequestDto
    {
        public DateOnly? ServiceDate { get; set; }
        public int? PreferredShiftId { get; set; }
    }
}
