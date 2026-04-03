using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class RoomDesignRequestDto
    {
        /// <summary>
        /// Base64 encoded image of the room
        /// </summary>
        public string RoomImageBase64 { get; set; } = null!;

        /// <summary>
        /// Optional feng shui element filter
        /// </summary>
        public FengShuiElementEnum? FengShuiElement { get; set; }

        /// <summary>
        /// Maximum budget for plant recommendations
        /// </summary>
        public decimal? MaxBudget { get; set; }

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
    }
}
