namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class EligibleCaretakerWithAvailabilityDto
    {
        public StaffWithSpecializationsResponseDto Staff { get; set; } = null!;
        public bool IsAvailable { get; set; }
        public List<string> ConflictDates { get; set; } = new List<string>();
    }
}
