namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class LayoutDesignImageGenerationResultDto
    {
        public int LayoutDesignId { get; set; }
        public int TotalItems { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int StatusAfter { get; set; }
        public List<LayoutDesignImageGenerationItemResultDto> Items { get; set; } = new();
    }

    public class LayoutDesignImageGenerationItemResultDto
    {
        public int LayoutDesignPlantId { get; set; }
        public int? CommonPlantId { get; set; }
        public int? PlantInstanceId { get; set; }
        public string? PlacementPosition { get; set; }
        public bool IsSuccess { get; set; }
        public string? ImageUrl { get; set; }
        public string? FluxPromptUsed { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class LayoutDesignGeneratedImageDto
    {
        public int Id { get; set; }
        public int LayoutDesignId { get; set; }
        public int? LayoutDesignPlantId { get; set; }
        public int? CommonPlantId { get; set; }
        public int? PlantInstanceId { get; set; }
        public string? ImageUrl { get; set; }
        public string? FluxPromptUsed { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
