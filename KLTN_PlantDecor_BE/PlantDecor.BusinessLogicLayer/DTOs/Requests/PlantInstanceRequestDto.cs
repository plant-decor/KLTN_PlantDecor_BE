using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    /// <summary>
    /// DTO tạo một PlantInstance trong batch
    /// </summary>
    public class PlantInstanceItemDto
    {
        [Required(ErrorMessage = "PlantId là bắt buộc")]
        public int PlantId { get; set; }

        public string? SKU { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal? SpecificPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Chiều cao phải lớn hơn hoặc bằng 0")]
        public decimal? Height { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đường kính thân phải lớn hơn hoặc bằng 0")]
        public decimal? TrunkDiameter { get; set; }

        public string? HealthStatus { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tuổi phải lớn hơn hoặc bằng 0")]
        public int? Age { get; set; }

        public string? Description { get; set; }
    }

    /// <summary>
    /// DTO cho batch create PlantInstances
    /// POST /api/manager/nurseries/{nurseryId}/plant-instances/batch
    /// </summary>
    public class BatchCreatePlantInstanceRequestDto
    {
        [Required(ErrorMessage = "Danh sách instances là bắt buộc")]
        [MinLength(1, ErrorMessage = "Cần ít nhất 1 instance")]
        public List<PlantInstanceItemDto> Instances { get; set; } = new();
    }

    /// <summary>
    /// DTO cập nhật status một PlantInstance
    /// PATCH /api/manager/plant-instances/{instanceId}/status
    /// </summary>
    public class UpdatePlantInstanceStatusDto
    {
        [Required(ErrorMessage = "Status là bắt buộc")]
        [Range(1, 5, ErrorMessage = "Status phải nằm trong khoảng 1-5")]
        public int Status { get; set; }
    }

    /// <summary>
    /// DTO batch cập nhật status nhiều PlantInstance
    /// PATCH /api/manager/plant-instances/batch-status
    /// </summary>
    public class BatchUpdatePlantInstanceStatusDto
    {
        [Required(ErrorMessage = "Danh sách InstanceIds là bắt buộc")]
        [MinLength(1, ErrorMessage = "Cần ít nhất 1 instance ID")]
        public List<int> InstanceIds { get; set; } = new();

        [Required(ErrorMessage = "Status là bắt buộc")]
        [Range(1, 5, ErrorMessage = "Status phải nằm trong khoảng 1-5")]
        public int Status { get; set; }
    }

    /// <summary>
    /// DTO cập nhật thông tin PlantInstance
    /// PATCH /api/manager/plant-instances/{instanceId}
    /// </summary>
    public class UpdatePlantInstanceRequestDto
    {
        public string? SKU { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        public decimal? SpecificPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Height must be greater than or equal to 0")]
        public decimal? Height { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Trunk diameter must be greater than or equal to 0")]
        public decimal? TrunkDiameter { get; set; }

        public string? HealthStatus { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Age must be greater than or equal to 0")]
        public int? Age { get; set; }

        public string? Description { get; set; }
    }
}
