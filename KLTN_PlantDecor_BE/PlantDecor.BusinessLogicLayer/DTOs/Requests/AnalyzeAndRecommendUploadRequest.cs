using Microsoft.AspNetCore.Http;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class AnalyzeAndRecommendUploadRequest
    {
        public IFormFile Image { get; set; } = null!;
        public FengShuiElementTypeEnum? FengShuiElement { get; set; }
        public decimal? MaxBudget { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public List<int>? PreferredNurseryIds { get; set; }
    }
}
