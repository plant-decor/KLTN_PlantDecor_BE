namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class EmbeddingSearchItemDto
    {
        public string EntityType { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
