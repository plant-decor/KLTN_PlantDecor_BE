namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class NurseryResponseDto
    {
        public int Id { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public decimal? Area { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Phone { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Statistics
        public int TotalPlants { get; set; }
        public int TotalMaterials { get; set; }
    }

    public class NurseryListResponseDto
    {
        public int Id { get; set; }
        public int? NurseryMaterialId { get; set; }
        public int? NurseryPlantComboId { get; set; }
        public int? CommonPlantId { get; set; }
        public int? Quantity { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class NurseryNearbyResponseDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public double DistanceKm { get; set; }
        public List<NurseryCareServiceSummaryDto> AvailableServices { get; set; } = new();
    }

    public class StaffWithSpecializationsResponseDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public int? Status { get; set; }
        public List<SpecializationSummaryDto> Specializations { get; set; } = new();
    }

    public class SpecializationSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
