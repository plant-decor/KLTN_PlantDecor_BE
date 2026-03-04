namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class NurseryResponseDto
    {
        public int Id { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public decimal? Area { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Phone { get; set; }
        public int? Type { get; set; }
        public string? TypeName { get; set; }
        public int? LightCondition { get; set; }
        public string? LightConditionName { get; set; }
        public int? HumidityLevel { get; set; }
        public string? HumidityLevelName { get; set; }
        public bool? HasMistSystem { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Statistics
        public int TotalPlants { get; set; }
        public int TotalMaterials { get; set; }
    }

    public class NurseryListResponseDto
    {
        public int Id { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public int? Type { get; set; }
        public string? TypeName { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
