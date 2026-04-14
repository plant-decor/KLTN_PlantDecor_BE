using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Embedding
{
    /// <summary>
    /// DTO for embedding CommonPlant entity with related data
    /// </summary>
    public class CommonPlantEmbeddingDto
    {
        public int CommonPlantId { get; set; }
        public int PlantId { get; set; }
        public bool IsActive { get; set; }

        public string PlantName { get; set; } = string.Empty;
        public string? PlantSpecificName { get; set; }
        public string? PlantDescription { get; set; }
        public string? PlantOrigin { get; set; }

        public int? FengShuiElement { get; set; }
        public string? FengShuiMeaning { get; set; }
        public int? Size { get; set; }
        public int PlacementType { get; set; }

        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public bool? AirPurifying { get; set; }

        public decimal? BasePrice { get; set; }
        public List<string> CategoryNames { get; set; } = new();
        public List<string> TagNames { get; set; } = new();

        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public decimal? Price { get; set; }

        // PlantGuide snapshot by PlantId
        public int? GuideLightRequirement { get; set; }
        public string? GuideLightRequirementName { get; set; }
        public string? GuideWatering { get; set; }
        public string? GuideFertilizing { get; set; }
        public string? GuidePruning { get; set; }
        public string? GuideTemperature { get; set; }
        public string? GuideHumidity { get; set; }
        public string? GuideSoil { get; set; }
        public string? GuideCareNotes { get; set; }
    }

    /// <summary>
    /// DTO for embedding PlantInstance entity with related data
    /// </summary>
    public class PlantInstanceEmbeddingDto
    {
        public int PlantInstanceId { get; set; }
        public int PlantId { get; set; }
        public int Status { get; set; }

        public string PlantName { get; set; } = string.Empty;
        public string? PlantSpecificName { get; set; }
        public int? FengShuiElement { get; set; }
        public string? FengShuiMeaning { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public bool? AirPurifying { get; set; }

        public string? Description { get; set; }
        public string? HealthStatus { get; set; }
        public decimal? Height { get; set; }
        public decimal? TrunkDiameter { get; set; }
        public int? Age { get; set; }
        public string? SKU { get; set; }

        public decimal? Price { get; set; }
        public decimal? SpecificPrice { get; set; }
        public decimal? BasePrice { get; set; }
        public List<string> CategoryNames { get; set; } = new();
        public List<string> TagNames { get; set; } = new();

        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }

        // PlantGuide snapshot by PlantId
        public int? GuideLightRequirement { get; set; }
        public string? GuideLightRequirementName { get; set; }
        public string? GuideWatering { get; set; }
        public string? GuideFertilizing { get; set; }
        public string? GuidePruning { get; set; }
        public string? GuideTemperature { get; set; }
        public string? GuideHumidity { get; set; }
        public string? GuideSoil { get; set; }
        public string? GuideCareNotes { get; set; }
    }

    /// <summary>
    /// DTO for embedding NurseryPlantCombo entity with related data
    /// </summary>
    public class NurseryPlantComboEmbeddingDto
    {
        public int NurseryPlantComboId { get; set; }
        public bool IsActive { get; set; }

        public string ComboName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SuitableSpace { get; set; }
        public List<string> SuitableRooms { get; set; } = new();
        public int? FengShuiElement { get; set; }
        public string? FengShuiPurpose { get; set; }
        public string? ThemeName { get; set; }
        public string? ThemeDescription { get; set; }
        public SeasonTypeEnum? Season { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }

        public decimal? ComboPrice { get; set; }
        public List<string> TagNames { get; set; } = new();

        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public decimal? Price { get; set; }
    }

    /// <summary>
    /// DTO for embedding NurseryMaterial entity with related data
    /// </summary>
    public class NurseryMaterialEmbeddingDto
    {
        public int NurseryMaterialId { get; set; }
        public bool IsActive { get; set; }
        public DateOnly? ExpiredDate { get; set; }

        public string MaterialName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string? Unit { get; set; }
        public string? Specifications { get; set; }
        public decimal? BasePrice { get; set; }
        public List<string> CategoryNames { get; set; } = new();
        public List<string> TagNames { get; set; } = new();

        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public decimal? Price { get; set; }
    }

    /// <summary>
    /// Metadata to store with embedding for filtering
    /// </summary>
    public class EmbeddingMetadata
    {
        public int NurseryId { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
        public int OriginalEntityId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
