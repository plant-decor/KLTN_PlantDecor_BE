using Microsoft.AspNetCore.Http;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class AnalyzeAndRecommendUploadRequest
    {
        public IFormFile Image { get; set; } = null!;
        public string? FengShuiElement { get; set; }
        public decimal? MaxBudget { get; set; }
        public int Limit { get; set; } = 3;
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public List<int>? PreferredNurseryIds { get; set; }
    }
}
