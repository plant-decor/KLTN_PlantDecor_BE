namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class LayoutDesignListResponseDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? RoomImageId { get; set; }
        public string? PreviewImageUrl { get; set; }
        public string? RawResponse { get; set; }
        public int? Status { get; set; }
        public bool? IsSaved { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<LayoutDesignPlantResponseDto> LayoutDesignPlants { get; set; } = new();
        public List<LayoutDesignAiResponseImageResponseDto> LayoutDesignAiResponseImages { get; set; } = new();
    }

    public class LayoutDesignPlantResponseDto
    {
        public int Id { get; set; }
        public int LayoutDesignId { get; set; }
        public int? CommonPlantId { get; set; }
        public int? PlantInstanceId { get; set; }
        public string? PlantReason { get; set; }
        public string? PlacementPosition { get; set; }
        public string? PlacementReason { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class LayoutDesignAiResponseImageResponseDto
    {
        public int Id { get; set; }
        public int LayoutDesignId { get; set; }
        public int? LayoutDesignPlantId { get; set; }
        public string? ImageUrl { get; set; }
        public string? PublicId { get; set; }
        public string? FluxPromptUsed { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
