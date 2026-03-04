using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    /// <summary>
    /// DTO để nhập vật tư vào vựa
    /// </summary>
    public class NurseryMaterialRequestDto
    {
        [Required(ErrorMessage = "MaterialId là bắt buộc")]
        public int MaterialId { get; set; }

        [Required(ErrorMessage = "NurseryId là bắt buộc")]
        public int NurseryId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        public int Quantity { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO để nhập thêm vật tư (tăng số lượng)
    /// </summary>
    public class ImportMaterialRequestDto
    {
        [Required(ErrorMessage = "MaterialId là bắt buộc")]
        public int MaterialId { get; set; }

        [Required(ErrorMessage = "Số lượng nhập là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
        public int Quantity { get; set; }

        public string? Note { get; set; }
    }
}
