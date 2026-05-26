using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class PlantRequestDto
    {
        [Required(ErrorMessage = "Plant name is required")]
        [StringLength(200, ErrorMessage = "Plant name cannot exceed 200 characters")]
        public string Name { get; set; } = null!;

        public string? SpecificName { get; set; }

        public string? Origin { get; set; }

        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value")]
        public decimal? BasePrice { get; set; }

        public int PlacementType { get; set; }

        public List<int>? RoomStyle { get; set; }

        public List<int>? RoomType { get; set; }

        public int? Size { get; set; }

        public int? GrowthRate { get; set; }

        public bool? Toxicity { get; set; }

        public bool? AirPurifying { get; set; }

        public bool? HasFlower { get; set; }

        public bool? PetSafe { get; set; }

        public bool? ChildSafe { get; set; }

        public int? FengShuiElement { get; set; }

        public string? FengShuiMeaning { get; set; }

        public bool? PotIncluded { get; set; }

        public string? PotSize { get; set; }

        public int? CareLevelType { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsUniqueInstance { get; set; } = false;

        // Categories và Tags sẽ được gắn riêng qua API khác
    }

    public class AssignCategoriesDto
    {
        [Required(ErrorMessage = "PlantId is required")]
        public int PlantId { get; set; }

        [Required(ErrorMessage = "CategoryIds list is required")]
        [MinLength(1, ErrorMessage = "Exactly one category is required for each plant")]
        [MaxLength(1, ErrorMessage = "Exactly one category is allowed for each plant")]
        public List<int> CategoryIds { get; set; } = new List<int>();
    }

    public class AssignTagsDto
    {
        [Required(ErrorMessage = "PlantId is required")]
        public int PlantId { get; set; }

        [Required(ErrorMessage = "TagIds list is required")]
        public List<int> TagIds { get; set; } = new List<int>();
    }
}
