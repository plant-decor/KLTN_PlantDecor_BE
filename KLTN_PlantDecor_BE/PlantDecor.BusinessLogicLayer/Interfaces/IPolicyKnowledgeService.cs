using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPolicyKnowledgeService
    {
        Task<List<PolicyContent>> GetAllActiveAsync();
        Task<List<PolicyContent>> GetByCategoryActiveAsync(PolicyContentCategoryEnum category);
        Task InvalidatePolicyCacheAsync();
    }
}
