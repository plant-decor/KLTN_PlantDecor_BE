using Microsoft.AspNetCore.Http;
using PlantDecor.DataAccessLayer.Enums;
using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class AnalyzeAndRecommendUploadRequest
    {
        public IFormFile Image { get; set; } = null!;
        public FengShuiElementTypeEnum? FengShuiElement { get; set; }
        [Required(ErrorMessage = "RoomType is required")]
        public RoomTypeEnum RoomType { get; set; }
        [Required(ErrorMessage = "RoomStyle is required")]
        public RoomStyleEnum RoomStyle { get; set; }
        public decimal? MinBudget { get; set; }
        public decimal? MaxBudget { get; set; }
        public CareLevelTypeEnum? CareLevelType { get; set; }
        public bool? HasAllergy { get; set; }
        public string? AllergyNote { get; set; }
        public List<int>? AllergicPlantIds { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public List<int>? PreferredNurseryIds { get; set; }
    }
}
