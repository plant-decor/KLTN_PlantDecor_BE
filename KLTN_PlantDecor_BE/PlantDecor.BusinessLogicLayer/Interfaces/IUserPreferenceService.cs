using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IUserPreferenceService
    {
        Task CalculatedAllUserPreferenceAsync();
        Task CalculateUserPreferenceForUserAsync(int userId);
        Task<List<UserPreferenceRecommendationResponseDto>> GetTopRecommendationsAsync(int userId, int limit);
        Task<List<UserPreferenceRecommendationResponseDto>> GetContextualRecommendationsAsync(int userId, int limit, int? seedPlantId = null);
        Task<CustomerSurveyResponseDto?> GetCustomerSurveyAsync(int userId);
        Task<CustomerSurveyResponseDto> UpsertCustomerSurveyAsync(int userId, CustomerSurveyUpsertRequestDto request);
    }
}
