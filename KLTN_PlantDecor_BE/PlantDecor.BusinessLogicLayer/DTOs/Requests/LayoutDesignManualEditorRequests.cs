using Microsoft.AspNetCore.Http;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class LayoutDesignManualDraftRequestDto
    {
        public string LayerJson { get; set; } = string.Empty;
    }

    public class LayoutDesignManualPublishRequestDto
    {
        public IFormFile? Image { get; set; }
        public string? LayerJson { get; set; }
    }

    public class LayoutDesignManualBeautifyRequestDto
    {
        public IFormFile? Image { get; set; }
        public string? ImageUrl { get; set; }
        public string? LayerJson { get; set; }
    }

    public class LayoutDesignManualCalculateRequestDto
    {
        public List<LayoutDesignManualCalculateItemDto> Items { get; set; } = new();
    }

    public class LayoutDesignManualCalculateItemDto
    {
        public int? CommonPlantId { get; set; }
        public int? PlantInstanceId { get; set; }
        public int Quantity { get; set; }
    }
}
