namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class PolicyContentResponseDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int? Category { get; set; }
        public string? Content { get; set; }
        public string? Summary { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
