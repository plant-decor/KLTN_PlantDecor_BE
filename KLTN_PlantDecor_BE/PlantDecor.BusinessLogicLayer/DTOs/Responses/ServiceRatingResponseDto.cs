namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class ServiceRatingResponseDto
    {
        public int Id { get; set; }
        public int ServiceRegistrationId { get; set; }
        public int Rating { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public UserSummaryDto? Customer { get; set; }
    }
}
