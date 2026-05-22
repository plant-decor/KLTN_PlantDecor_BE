namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class CareServicePackageRecommendationResponseDto
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public List<string> DominantCategories { get; set; } = new();
        public string? DominantCareLevel { get; set; }
        public bool CareLevelMatched { get; set; }
        public decimal CoveragePercentage { get; set; }
        public int EcosystemMatchPercentage { get; set; }
        public string? MatchReason { get; set; }
    }
}
