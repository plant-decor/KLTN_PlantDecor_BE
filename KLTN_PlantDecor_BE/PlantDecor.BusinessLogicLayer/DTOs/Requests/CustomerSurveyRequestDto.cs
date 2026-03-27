using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CustomerSurveyUpsertRequestDto
    {
        public bool? HasPets { get; set; }
        public bool? HasChildren { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "MaxBudget must be greater than or equal to 0")]
        public decimal? MaxBudget { get; set; }

        [Range(1, 4, ErrorMessage = "ExperienceLevel must be between 1 and 4")]
        public int ExperienceLevel { get; set; }

        [Range(1, 3, ErrorMessage = "PreferredPlacement must be between 1 and 3")]
        public int PreferredPlacement { get; set; }
    }
}
