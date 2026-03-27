namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class CustomerSurveyResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool? HasPets { get; set; }
        public bool? HasChildren { get; set; }
        public decimal? MaxBudget { get; set; }
        public int ExperienceLevel { get; set; }
        public string ExperienceLevelName { get; set; } = null!;
        public int PreferredPlacement { get; set; }
        public string PreferredPlacementName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
