using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreatePolicyContentRequestDto
    {
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public int Category { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Summary { get; set; }

        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdatePolicyContentRequestDto
    {
        [MaxLength(300)]
        public string? Title { get; set; }

        public int? Category { get; set; }

        public string? Content { get; set; }

        [MaxLength(1000)]
        public string? Summary { get; set; }

        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }

    public class SetPolicyContentStatusRequestDto
    {
        [Required]
        public bool IsActive { get; set; }
    }
}
