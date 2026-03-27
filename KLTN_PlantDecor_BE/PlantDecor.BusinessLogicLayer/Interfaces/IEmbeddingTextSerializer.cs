using PlantDecor.BusinessLogicLayer.DTOs.Embedding;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IEmbeddingTextSerializer
    {
        /// <summary>
        /// Serialize CommonPlant entity to rich text for embedding
        /// </summary>
        string SerializeCommonPlant(CommonPlantEmbeddingDto dto);

        /// <summary>
        /// Serialize PlantInstance entity to rich text for embedding
        /// </summary>
        string SerializePlantInstance(PlantInstanceEmbeddingDto dto);

        /// <summary>
        /// Serialize NurseryPlantCombo entity to rich text for embedding
        /// </summary>
        string SerializeNurseryPlantCombo(NurseryPlantComboEmbeddingDto dto);

        /// <summary>
        /// Serialize NurseryMaterial entity to rich text for embedding
        /// </summary>
        string SerializeNurseryMaterial(NurseryMaterialEmbeddingDto dto);

        /// <summary>
        /// Extract metadata from embedding DTO for filtering purposes
        /// </summary>
        Dictionary<string, object> ExtractMetadata(int nurseryId, decimal? price, string status, int originalEntityId);
    }
}
