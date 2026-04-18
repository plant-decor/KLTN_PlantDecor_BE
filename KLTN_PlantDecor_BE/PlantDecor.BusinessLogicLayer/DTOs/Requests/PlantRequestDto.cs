using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class PlantRequestDto
    {
        [Required(ErrorMessage = "Tên cây là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên cây không được vượt quá 200 ký tự")]
        public string Name { get; set; } = null!;

        public string? SpecificName { get; set; }

        public string? Origin { get; set; }

        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
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
        [Required(ErrorMessage = "PlantId là bắt buộc")]
        public int PlantId { get; set; }

        [Required(ErrorMessage = "Danh sách CategoryIds là bắt buộc")]
        public List<int> CategoryIds { get; set; } = new List<int>();
    }

    public class AssignTagsDto
    {
        [Required(ErrorMessage = "PlantId là bắt buộc")]
        public int PlantId { get; set; }

        [Required(ErrorMessage = "Danh sách TagIds là bắt buộc")]
        public List<int> TagIds { get; set; } = new List<int>();
    }
}
