using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class LayoutDesignManualEditorContextDto
    {
        public int LayoutDesignId { get; set; }
        public string RoomImageUrl { get; set; } = string.Empty;
        public bool IsFullCatalog { get; set; }
        public List<LayoutDesignManualEditorPlantDto> Plants { get; set; } = new();
        public List<LayoutDesignManualEditorImageDto> Images { get; set; } = new();
    }

    public class LayoutDesignManualEditorPlantDto
    {
        public int? LayoutDesignPlantId { get; set; }
        public int? CommonPlantId { get; set; }
        public int? PlantInstanceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? PlacementPosition { get; set; }
        public bool IsRecommended { get; set; }
    }

    public class LayoutDesignManualEditorImageDto
    {
        public int ImageId { get; set; }
        public int LayoutDesignId { get; set; }
        public int? LayoutDesignPlantId { get; set; }
        public string? ImageUrl { get; set; }
        public LayoutDesignImageSourceTypeEnum? SourceType { get; set; }
        public int? ReplacesImageId { get; set; }
        public string? ManualLayerJson { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class LayoutDesignManualCalculateResponseDto
    {
        public List<LayoutDesignManualCalculateLineItemDto> Items { get; set; } = new();
        public List<LayoutDesignManualCalculateErrorDto> Errors { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    public class LayoutDesignManualCalculateLineItemDto
    {
        public int? CommonPlantId { get; set; }
        public int? PlantInstanceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class LayoutDesignManualCalculateErrorDto
    {
        public int? Index { get; set; }
        public int? CommonPlantId { get; set; }
        public int? PlantInstanceId { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
