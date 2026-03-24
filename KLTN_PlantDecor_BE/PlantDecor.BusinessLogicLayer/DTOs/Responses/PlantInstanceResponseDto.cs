using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    /// <summary>
    /// Response chi tiết một PlantInstance
    /// </summary>
    public class PlantInstanceResponseDto
    {
        public int Id { get; set; }
        public int? PlantId { get; set; }
        public string? PlantName { get; set; }
        public int? CurrentNurseryId { get; set; }
        public string? NurseryName { get; set; }
        public string? SKU { get; set; }
        public decimal? SpecificPrice { get; set; }
        public decimal? Height { get; set; }
        public decimal? TrunkDiameter { get; set; }
        public string? HealthStatus { get; set; }
        public int? Age { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PlantInstanceImageDto> Images { get; set; } = new();
    }

    /// <summary>
    /// Response danh sách PlantInstance (rút gọn)
    /// </summary>
    public class PlantInstanceListResponseDto
    {
        public int Id { get; set; }
        public int? PlantId { get; set; }
        public string? PlantName { get; set; }
        public string? SKU { get; set; }
        public decimal? SpecificPrice { get; set; }
        public decimal? Height { get; set; }
        public string? HealthStatus { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO hình ảnh PlantInstance
    /// </summary>
    public class PlantInstanceImageDto
    {
        public int Id { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsPrimary { get; set; }
    }

    /// <summary>
    /// Response tổng hợp plant summary theo nursery
    /// GET /api/manager/nurseries/{nurseryId}/plants-summary
    /// </summary>
    public class NurseryPlantSummaryDto
    {
        public int PlantId { get; set; }
        public string? PlantName { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public decimal? BasePrice { get; set; }
        public int TotalInstances { get; set; }
        public int AvailableCount { get; set; }
        public int SoldCount { get; set; }
        public int ReservedCount { get; set; }
        public int DamagedCount { get; set; }
        public int Inactive { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }

    /// <summary>
    /// Response batch create
    /// </summary>
    public class BatchCreatePlantInstanceResponseDto
    {
        public int TotalCreated { get; set; }
        public List<PlantInstanceResponseDto> Instances { get; set; } = new();
    }

    /// <summary>
    /// Response batch update status
    /// </summary>
    public class BatchUpdateStatusResponseDto
    {
        public int TotalUpdated { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
    }

    /// <summary>
    /// Response danh sách nursery có plant cụ thể (cho shop)
    /// GET /api/plants/{id}/nurseries
    /// </summary>
    public class PlantNurseryAvailabilityDto
    {
        public int? CommonPlantId { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int AvailableInstanceCount { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}
