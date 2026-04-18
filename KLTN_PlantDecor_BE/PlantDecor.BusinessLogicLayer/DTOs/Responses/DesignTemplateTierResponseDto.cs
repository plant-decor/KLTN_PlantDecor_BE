namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class DesignTemplateTierResponseDto
    {
        public int Id { get; set; }
        public int DesignTemplateId { get; set; }
        public string TierName { get; set; } = string.Empty;
        public decimal MinArea { get; set; }
        public decimal MaxArea { get; set; }
        public decimal PackagePrice { get; set; }
        public string ScopedOfWork { get; set; } = string.Empty;
        public int EstimatedDays { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<DesignTemplateTierItemResponseDto> Items { get; set; } = new();
    }

    public class DesignTemplateTierItemResponseDto
    {
        public int Id { get; set; }
        public int DesignTemplateTierId { get; set; }
        public int? MaterialId { get; set; }
        public int? PlantId { get; set; }
        public int ItemType { get; set; }
        public decimal Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
