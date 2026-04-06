namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class RoomAnalysisJsonDto
    {
        public string? RoomType { get; set; }
        public string? RoomSize { get; set; }
        public string? LightingCondition { get; set; }
        public string? InteriorStyle { get; set; }
        public string? AvailableSpace { get; set; }
        public List<string>? ColorPalette { get; set; }
        public string? Summary { get; set; }
    }
}
