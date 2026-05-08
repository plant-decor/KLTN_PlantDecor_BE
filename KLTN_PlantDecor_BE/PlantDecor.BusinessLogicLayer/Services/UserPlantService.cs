using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
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

        public async Task<UserPlantResponseDto> UpdateMyPlantAsync(int userId, int userPlantId, UpdateUserPlantRequestDto request)
        {
            var userPlant = await _unitOfWork.UserPlantRepository.GetByIdAndUserIdWithDetailsAsync(userPlantId, userId)
                ?? throw new NotFoundException($"UserPlant {userPlantId} not found");

            ValidateUpdateRequest(request);

            if (request.PurchaseDate.HasValue)
                userPlant.PurchaseDate = request.PurchaseDate;

            if (request.LastWateredDate.HasValue)
                userPlant.LastWateredDate = request.LastWateredDate;

            if (request.LastFertilizedDate.HasValue)
                userPlant.LastFertilizedDate = request.LastFertilizedDate;

            if (request.LastPrunedDate.HasValue)
                userPlant.LastPrunedDate = request.LastPrunedDate;

            if (request.Location != null)
                userPlant.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();

            if (request.CurrentTrunkDiameter.HasValue)
                userPlant.CurrentTrunkDiameter = request.CurrentTrunkDiameter;

            if (request.CurrentHeight.HasValue)
                userPlant.CurrentHeight = request.CurrentHeight;

            if (request.HealthStatus != null)
                userPlant.HealthStatus = string.IsNullOrWhiteSpace(request.HealthStatus) ? null : request.HealthStatus.Trim();

            if (request.Age.HasValue)
                userPlant.Age = request.Age;

            userPlant.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.UserPlantRepository.PrepareUpdate(userPlant);
            await _unitOfWork.SaveAsync();

            return userPlant.ToResponse();
        }

        public async Task<PaginatedResult<CareReminderNotificationResponseDto>> GetMyCareRemindersAsync(int userId, int? careType, Pagination pagination)
        {
            if (careType.HasValue && !Enum.IsDefined(typeof(CareReminderTypeEnum), careType.Value))
            {
                throw new BadRequestException("Invalid CareType");
            }

            var result = await _unitOfWork.CareReminderRepository.GetByUserIdWithFiltersAsync(userId, careType, pagination);
            var items = result.Items.Select(reminder => reminder.ToNotificationResponse()).ToList();
            return new PaginatedResult<CareReminderNotificationResponseDto>(items, result.TotalCount, result.PageNumber, result.PageSize);
        }

        public async Task<List<CareReminderNotificationResponseDto>> GetMyCareRemindersTodayAsync(int userId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var reminders = await _unitOfWork.CareReminderRepository.GetByUserIdAndReminderDateAsync(userId, today);
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

        private static void ValidateUpdateRequest(UpdateUserPlantRequestDto request)
        {
            if (request.Location != null && request.Location.Trim().Length > 100)
                throw new BadRequestException("Location is too long");

            if (request.HealthStatus != null && request.HealthStatus.Trim().Length > 50)
                throw new BadRequestException("HealthStatus is too long");

            if (request.CurrentTrunkDiameter.HasValue && request.CurrentTrunkDiameter.Value < 0)
                throw new BadRequestException("CurrentTrunkDiameter cannot be negative");

            if (request.CurrentHeight.HasValue && request.CurrentHeight.Value < 0)
                throw new BadRequestException("CurrentHeight cannot be negative");

            if (request.Age.HasValue && request.Age.Value < 0)
                throw new BadRequestException("Age cannot be negative");
        }
    }
}
