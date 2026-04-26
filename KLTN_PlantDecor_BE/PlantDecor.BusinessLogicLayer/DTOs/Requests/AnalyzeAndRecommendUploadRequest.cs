using Microsoft.AspNetCore.Http;
using PlantDecor.DataAccessLayer.Enums;
using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class AnalyzeAndRecommendUploadRequest
    {
        public List<IFormFile> Images { get; set; } = new();
        public List<RoomViewAngleEnum> ViewAngles { get; set; } = new();
        public FengShuiElementTypeEnum? FengShuiElement { get; set; }
        [Required(ErrorMessage = "RoomType is required")]
        public RoomTypeEnum RoomType { get; set; }
        [Required(ErrorMessage = "RoomStyle is required")]
        public RoomStyleEnum RoomStyle { get; set; }
        public decimal? RoomArea { get; set; }
        public DirectionEnum? LightDirection { get; set; }
        public DirectionEnum? DominantDirection { get; set; }
        public LightRequirementEnum? NaturalLightLevel { get; set; }
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
