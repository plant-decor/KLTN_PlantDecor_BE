namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public int? ParentCategoryId { get; set; }
        public string Name { get; set; } = null!;
        public bool? IsActive { get; set; }
        public int CategoryType { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ParentCategoryName { get; set; }
        public List<CategoryResponseDto> SubCategories { get; set; } = new List<CategoryResponseDto>();
    }
}
