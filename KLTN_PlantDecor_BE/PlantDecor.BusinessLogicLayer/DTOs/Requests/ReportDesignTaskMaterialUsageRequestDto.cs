using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class ReportDesignTaskMaterialUsageRequestDto
    {
        public List<ReportDesignTaskMaterialUsageItemDto> MaterialUsages { get; set; } = new();
    }

    public class ReportDesignTaskMaterialUsageItemDto
    {
        [Range(1, int.MaxValue)]
        public int MaterialId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal ActualQuantity { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
