using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class UserPlantService : IUserPlantService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserPlantService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<UserPlantResponseDto>> GetMyPlantsAsync(int userId)
        {
            var userPlants = await _unitOfWork.UserPlantRepository.GetByUserIdWithDetailsAsync(userId);
            return userPlants.ToResponseList();
        }

        public async Task<List<CareReminderNotificationResponseDto>> GetMyCareRemindersAsync(int userId)
        {
            var reminders = await _unitOfWork.CareReminderRepository.GetByUserIdWithDetailsAsync(userId);
            return reminders.Select(reminder => reminder.ToNotificationResponse()).ToList();
        }

        public async Task AddPurchasedPlantsToMyPlantAsync(int orderId, DateTime purchasedAt)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);
            if (order == null || order.UserId <= 0)
            {
                return;
            }

            var purchaseDate = DateOnly.FromDateTime(purchasedAt.Date);
            var now = purchasedAt;
            var addedCommonPlantIds = new HashSet<int>();

            foreach (var nurseryOrder in order.NurseryOrders)
            {
                foreach (var detail in nurseryOrder.NurseryOrderDetails)
                {
                    if (detail.PlantInstanceId.HasValue)
                    {
                        var plantInstanceId = detail.PlantInstanceId.Value;
                        var alreadyOwned = await _unitOfWork.UserPlantRepository
                            .ExistsByUserIdAndPlantInstanceIdAsync(order.UserId, plantInstanceId);

                        if (alreadyOwned)
                        {
                            continue;
                        }

                        var userPlantFromInstance = new UserPlant
                        {
                            UserId = order.UserId,
                            PlantId = detail.PlantInstance?.PlantId,
                            PlantInstanceId = plantInstanceId,
                            PurchaseDate = purchaseDate,
                            CurrentHeight = detail.PlantInstance?.Height,
                            CurrentTrunkDiameter = detail.PlantInstance?.TrunkDiameter,
                            HealthStatus = detail.PlantInstance?.HealthStatus,
                            Age = detail.PlantInstance?.Age,
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        _unitOfWork.UserPlantRepository.PrepareCreate(userPlantFromInstance);
                        continue;
                    }

                    if (detail.CommonPlant?.PlantId is int plantId)
                    {
                        if (addedCommonPlantIds.Contains(plantId))
                        {
                            continue;
                        }

                        var alreadyOwned = await _unitOfWork.UserPlantRepository
                            .ExistsByUserIdAndPlantIdAsync(order.UserId, plantId);

                        if (alreadyOwned)
                        {
                            addedCommonPlantIds.Add(plantId);
                            continue;
                        }

                        var userPlant = new UserPlant
                        {
                            UserId = order.UserId,
                            PlantId = plantId,
                            PurchaseDate = purchaseDate,
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        _unitOfWork.UserPlantRepository.PrepareCreate(userPlant);
                        addedCommonPlantIds.Add(plantId);
                    }
                }
            }

            await _unitOfWork.SaveAsync();
        }
    }
}