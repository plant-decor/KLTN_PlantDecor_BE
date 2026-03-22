using PlantDecor.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IUserBehaviorLogRepository
    {
        // Hàm này dùng để ghi nhận hành vi mới
        Task AddLogAsync(UserBehaviorLog log);

        // Hàm này chuẩn bị sẵn cho Service tính điểm Gợi ý (sau này dùng)
        Task<List<UserBehaviorLog>> GetLogsByUserAndDateAsync(int userId, DateTime fromDate);
    }
}
