using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class SpecializationRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    public class UpdateSpecializationRequestDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }

    public class AssignStaffSpecializationDto
    {
        [Required]
        public int SpecializationId { get; set; }
    }

    public class SetSpecializationsDto
    {
        [Required]
        public List<int> SpecializationIds { get; set; } = new();
    }
}
