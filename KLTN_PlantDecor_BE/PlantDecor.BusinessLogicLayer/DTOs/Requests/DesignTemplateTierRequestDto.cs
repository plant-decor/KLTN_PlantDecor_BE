using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateDesignTemplateTierRequestDto
    {
        [Required]
        public int DesignTemplateId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TierName { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal MinArea { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MaxArea { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PackagePrice { get; set; }

        [Required]
        [MaxLength(2000)]
        public string ScopedOfWork { get; set; } = string.Empty;

        [Range(1, 3650)]
        public int EstimatedDays { get; set; }

        public bool IsActive { get; set; } = true;

        public List<DesignTemplateTierItemInputDto>? Items { get; set; }
    }

    public class UpdateDesignTemplateTierRequestDto
    {
        [MaxLength(100)]
        public string? TierName { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinArea { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxArea { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PackagePrice { get; set; }

        [MaxLength(2000)]
        public string? ScopedOfWork { get; set; }

        [Range(1, 3650)]
        public int? EstimatedDays { get; set; }

        public bool? IsActive { get; set; }
    }

    public class SetDesignTemplateTierItemsRequestDto
    {
        [Required]
        public List<DesignTemplateTierItemInputDto> Items { get; set; } = new();
    }

    public class DesignTemplateTierItemInputDto
    {
        public int? MaterialId { get; set; }
        public int? PlantId { get; set; }

        [Range(1, int.MaxValue)]
        public int ItemType { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal Quantity { get; set; }
    }
}
