using System;
using System.Threading.Tasks;
using PlantDecor.BusinessLogicLayer.DTOs.Embedding;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    /// <summary>
    /// Service for processing Langflow data ingestion in background using Hangfire
    /// </summary>
    public interface ILangflowBackgroundJobService
    {
        Task ProcessCommonPlantIngestionAsync(CommonPlantEmbeddingDto dto, Guid entityId, string entityType);
        Task ProcessPlantInstanceIngestionAsync(PlantInstanceEmbeddingDto dto, Guid entityId, string entityType);
        Task ProcessNurseryPlantComboIngestionAsync(NurseryPlantComboEmbeddingDto dto, Guid entityId, string entityType);
        Task ProcessNurseryMaterialIngestionAsync(NurseryMaterialEmbeddingDto dto, Guid entityId, string entityType);
    }
}