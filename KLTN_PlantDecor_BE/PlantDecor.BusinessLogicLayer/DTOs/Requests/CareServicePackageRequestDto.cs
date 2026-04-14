using System.ComponentModel.DataAnnotations;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateCareServicePackageRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string? Features { get; set; }

        /// <summary>
        /// 1 = OneTime, 2 = Periodic
        /// </summary>
        [Required]
        public int ServiceType { get; set; }

        /// <summary>
        /// Chỉ bắt buộc với Periodic (ServiceType = 2). Với OneTime thì bỏ qua.
        /// </summary>
        public int? VisitPerWeek { get; set; }

        [Required]
        [Range(1, 365)]
        public int DurationDays { get; set; }

        public int? AreaLimit { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public List<int>? SpecializationIds { get; set; }
    }

    public class UpdateCareServicePackageRequestDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string? Features { get; set; }

        [Range(1, 7)]
        public int? VisitPerWeek { get; set; }

        [Range(1, 365)]
        public int? DurationDays { get; set; }

        public int? ServiceType { get; set; }

        public int? AreaLimit { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UnitPrice { get; set; }

        public bool? IsActive { get; set; }
    }
}
