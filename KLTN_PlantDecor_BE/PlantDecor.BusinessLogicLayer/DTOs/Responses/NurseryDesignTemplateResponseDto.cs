namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class NurseryDesignTemplateResponseDto
    {
        public int Id { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public int DesignTemplateId { get; set; }
        public string? DesignTemplateName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
