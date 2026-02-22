using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class InventoryRequestDto
    {
        public string? InventoryCode { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        public string? Name { get; set; }

        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal? BasePrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        public int? StockQuantity { get; set; }

        public string? Unit { get; set; }

        public string? Brand { get; set; }

        public string? Specifications { get; set; }

        public int? ExpiryMonths { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class AssignInventoryCategoriesDto
    {
        [Required(ErrorMessage = "InventoryId là bắt buộc")]
        public int InventoryId { get; set; }

        [Required(ErrorMessage = "Danh sách CategoryIds là bắt buộc")]
        public List<int> CategoryIds { get; set; } = new List<int>();
    }

    public class AssignInventoryTagsDto
    {
        [Required(ErrorMessage = "InventoryId là bắt buộc")]
        public int InventoryId { get; set; }

        [Required(ErrorMessage = "Danh sách TagIds là bắt buộc")]
        public List<int> TagIds { get; set; } = new List<int>();
    }
}
