using PlantDecor.DataAccessLayer.Enums;
using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class RoomDesignRequestDto
    {
        /// <summary>
        /// Base64 encoded image of the room
        /// </summary>
        public string RoomImageBase64 { get; set; } = null!;

        /// <summary>
        /// One-to-one mapping between room image content and its shooting angle
        /// </summary>
        public List<RoomImageAnalysisInputDto> RoomImageAnalyses { get; set; } = new();

        /// <summary>
        /// Optional feng shui element filter
        /// </summary>
        public FengShuiElementTypeEnum? FengShuiElement { get; set; }

        /// <summary>
        /// Optional room type value aligned with RoomDesignPreferences
        /// </summary>
        [Required(ErrorMessage = "RoomType is required")]
        public RoomTypeEnum RoomType { get; set; }

        /// <summary>
        /// Optional room style value aligned with RoomDesignPreferences
        /// </summary>
        [Required(ErrorMessage = "RoomStyle is required")]
        public RoomStyleEnum RoomStyle { get; set; }

        /// <summary>
        /// Optional room area value aligned with RoomDesignPreferences
        /// </summary>
        public decimal? RoomArea { get; set; }

        /// <summary>
        /// Direction where main light enters the room
        /// </summary>
        public DirectionEnum? LightDirection { get; set; }

        /// <summary>
        /// Dominant direction of the room
        /// </summary>
        public DirectionEnum? DominantDirection { get; set; }

        /// <summary>
        /// Optional minimum budget filter
        /// </summary>
        public decimal? MinBudget { get; set; }

        /// <summary>
        /// Maximum budget for plant recommendations
        /// </summary>
        public decimal? MaxBudget { get; set; }

        /// <summary>
        /// Optional care level value aligned with RoomDesignPreferences
        /// </summary>
        public CareLevelTypeEnum? CareLevelType { get; set; }

        /// <summary>
        /// Optional flag aligned with RoomDesignPreferences
        /// </summary>
        public bool? IsOftenAway { get; set; }

        /// <summary>
        /// Optional natural light level value aligned with RoomDesignPreferences
        /// </summary>
        public LightRequirementEnum? NaturalLightLevel { get; set; }

        /// <summary>
        /// Optional allergy flag aligned with RoomDesignPreferences
        /// </summary>
        public bool? HasAllergy { get; set; }

        /// <summary>
        /// Optional allergy note aligned with RoomDesignPreferences
        /// </summary>
        public string? AllergyNote { get; set; }

        /// <summary>
        /// Optional selected active Plant IDs that the user is allergic to
        /// </summary>
        public List<int>? AllergicPlantIds { get; set; }

        /// <summary>
        /// Filter for pet-safe plants only
        /// </summary>
        public bool? PetSafe { get; set; }

        /// <summary>
        /// Filter for child-safe plants only
        /// </summary>
        public bool? ChildSafe { get; set; }

        /// <summary>
        /// Preferred nursery IDs (optional)
        /// </summary>
        public List<int>? PreferredNurseryIds { get; set; }

        /// <summary>
        /// Internal persistence metadata for uploaded room image
        /// </summary>
        public int? RoomImageId { get; set; }

        /// <summary>
        /// Internal metadata for all room image ids tied to this design request
        /// </summary>
        public List<int> RoomImageIds { get; set; } = new();

        /// <summary>
        /// Internal persistence metadata for request user
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Internal persistence metadata for uploaded image URL
        /// </summary>
        public string? UploadedImageUrl { get; set; }

        /// <summary>
        /// Internal persistence metadata for uploaded image URLs
        /// </summary>
        public List<string> UploadedImageUrls { get; set; } = new();
    }
}
