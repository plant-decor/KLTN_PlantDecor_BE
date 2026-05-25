using System;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class RankedCarePackageDto
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public int Score { get; set; }
        public string? Reason { get; set; }
        public int EcosystemMatchPercentage { get; set; }
        public decimal CoveragePercentage { get; set; }
    }
}
