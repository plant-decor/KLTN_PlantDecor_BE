using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateDesignTemplateRequestDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? Style { get; set; }

        public List<int>? RoomTypes { get; set; }

        [MaxLength(512)]
        public string? ImageUrl { get; set; }

        public List<int>? SpecializationIds { get; set; }
    }

    public class UpdateDesignTemplateRequestDto
    {
        [MaxLength(255)]
        public string? Name { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? Style { get; set; }

        public List<int>? RoomTypes { get; set; }

        [MaxLength(512)]
        public string? ImageUrl { get; set; }
    }

    public class SetDesignTemplateSpecializationsRequestDto
    {
        public List<int> SpecializationIds { get; set; } = new();
    }
}
