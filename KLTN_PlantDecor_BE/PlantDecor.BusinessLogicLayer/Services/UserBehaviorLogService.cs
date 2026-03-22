using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class UserBehaviorLogService : IUserBehaviorLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserBehaviorLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogUserActionAsync(int userId, int? plantId, UserActionTypeEnum actionType)
        {
            var log = new UserBehaviorLog
            {
                UserId = userId,
                PlantId = plantId,
                ActionType = (int)actionType,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.UserBehaviorLogRepository.AddLogAsync(log);

            await _unitOfWork.SaveAsync();
        }
    }
}
