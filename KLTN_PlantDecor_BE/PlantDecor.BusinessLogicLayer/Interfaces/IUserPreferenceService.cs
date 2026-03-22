using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IUserPreferenceService
    {
        Task CalculatedAllUserPreferenceAsync();
        Task CalculateUserPreferenceForUserAsync(int userId);
        Task<List<UserPreferenceRecommendationResponseDto>> GetTopRecommendationsAsync(int userId, int limit);
        Task<List<UserPreferenceRecommendationResponseDto>> GetContextualRecommendationsAsync(int userId, int limit, int? seedPlantId = null);
    }
}
