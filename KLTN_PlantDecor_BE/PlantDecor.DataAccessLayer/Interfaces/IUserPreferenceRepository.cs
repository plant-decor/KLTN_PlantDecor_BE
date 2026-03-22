using PlantDecor.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IUserPreferenceRepository
    {
        Task<UserPreference?> GetByUserAndPlantAsync(int userId, int plantId);
        Task AddAsync(UserPreference preference);
        void Update(UserPreference preference);

        // Phục vụ cho hàm hiển thị ban ngày (API)
        Task<List<UserPreference>> GetTopRecommendationsAsync(int userId, int limit);
    }
}
