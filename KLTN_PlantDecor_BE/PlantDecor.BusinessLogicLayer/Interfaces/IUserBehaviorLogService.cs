using PlantDecor.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IUserBehaviorLogService
    {
        Task LogUserActionAsync(int userId, int? plantId, UserActionTypeEnum actionType);
    }
}
