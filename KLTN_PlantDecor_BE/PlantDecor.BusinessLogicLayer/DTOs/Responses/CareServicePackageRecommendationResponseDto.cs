namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class CareServicePackageRecommendationResponseDto
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public int MatchScore { get; set; }
        public int MatchedCategoryCount { get; set; }
        public int MatchedCareLevelCount { get; set; }
        public int TotalPurchasedPlantItems { get; set; }
        public List<string> MatchReasons { get; set; } = new();
        public List<RecommendedPlantDto> Plants { get; set; } = new();
    }

    public class RecommendedPlantDto
    {
        public int PlantId { get; set; }
        public string PlantName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
