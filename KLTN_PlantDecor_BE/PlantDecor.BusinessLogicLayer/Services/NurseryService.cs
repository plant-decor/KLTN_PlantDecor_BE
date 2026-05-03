using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Extensions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class NurseryService : INurseryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_NURSERIES_KEY = "nurseries_all";
        private const string ACTIVE_NURSERIES_KEY = "nurseries_active";

        public NurseryService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        #region CRUD Operations

        public async Task<PaginatedResult<NurseryListResponseDto>> GetAllNurseriesAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_NURSERIES_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<NurseryListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.NurseryRepository.GetAllWithDetailsAsync(pagination);
            var result = new PaginatedResult<NurseryListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PaginatedResult<NurseryListResponseDto>> GetActiveNurseriesAsync(Pagination pagination)
        {
            var cacheKey = $"{ACTIVE_NURSERIES_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<NurseryListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.NurseryRepository.GetActiveNurseriesAsync(pagination);
            var result = new PaginatedResult<NurseryListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<NurseryResponseDto> GetNurseryByIdAsync(int id)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByIdWithDetailsAsync(id);
            if (nursery == null)
                throw new NotFoundException($"Nursery with ID {id} not found");

            return nursery.ToResponse();
        }

        public async Task<NurseryResponseDto> GetMyNurseryAsync(int userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
                throw new UnauthorizedException("User not found");

            int nurseryId;
            if (user.RoleId == (int)RoleEnum.Manager)
            {
                var managedNursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(userId);
                if (managedNursery == null)
                    throw new NotFoundException("Nursery not found for your account");

                nurseryId = managedNursery.Id;
            }
            else if (user.RoleId == (int)RoleEnum.Staff)
            {
                if (!user.NurseryId.HasValue)
                    throw new NotFoundException("You have not been assigned to any nursery");

                nurseryId = user.NurseryId.Value;
            }
            else
            {
                throw new ForbiddenException("Only Manager or Staff can view their nursery information");
            }

            // Load full details
            var fullNursery = await _unitOfWork.NurseryRepository.GetByIdWithDetailsAsync(nurseryId);
            if (fullNursery == null)
                throw new NotFoundException($"Nursery with ID {nurseryId} not found");

            return fullNursery.ToResponse();
        }

        public async Task<NurseryResponseDto> CreateNurseryAsync(NurseryRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Check duplicate name
                if (await _unitOfWork.NurseryRepository.ExistsByNameAsync(request.Name))
                    throw new BadRequestException($"Nursery with name '{request.Name}' already exists");

                var entity = request.ToEntity();

                _unitOfWork.NurseryRepository.PrepareCreate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                // Reload with details
                var created = await _unitOfWork.NurseryRepository.GetByIdWithDetailsAsync(entity.Id);
                return created!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<NurseryResponseDto> UpdateNurseryAsync(int id, NurseryUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var entity = await _unitOfWork.NurseryRepository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    throw new NotFoundException($"Nursery with ID {id} not found");

                // Check duplicate name if updating
                if (!string.IsNullOrEmpty(request.Name) &&
                    await _unitOfWork.NurseryRepository.ExistsByNameAsync(request.Name, id))
                    throw new BadRequestException($"Nursery with name '{request.Name}' already exists");

                if (request.ManagerId.HasValue)
                {
                    var manager = await _unitOfWork.UserRepository.GetByIdAsync(request.ManagerId.Value);
                    if (manager == null)
                        throw new NotFoundException($"User {request.ManagerId.Value} not found");
                    if (manager.RoleId != (int)RoleEnum.Manager)
                        throw new BadRequestException("Selected user is not a Manager");
                }

                request.ToUpdate(entity);

                _unitOfWork.NurseryRepository.PrepareUpdate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return entity.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByIdAsync(id);
            if (nursery == null)
                throw new NotFoundException($"Nursery with ID {id} not found");

            nursery.IsActive = !nursery.IsActive;

            _unitOfWork.NurseryRepository.PrepareUpdate(nursery);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();

            return nursery.IsActive ?? true;
        }

        public async Task<NurseryResponseDto> AssignManagerAsync(int nurseryId, int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByIdWithDetailsAsync(nurseryId);
            if (nursery == null)
                throw new NotFoundException($"Nursery with ID {nurseryId} not found");

            var manager = await _unitOfWork.UserRepository.GetByIdAsync(managerId);
            if (manager == null)
                throw new NotFoundException($"User with ID {managerId} not found");

            if (manager.RoleId != (int)RoleEnum.Manager)
                throw new BadRequestException("Chosen user is not a Manager");

            if (manager.Status != (int)UserStatusEnum.Active || !manager.IsVerified)
                throw new BadRequestException("Manager Account is not active or not verified");

            // Manager đang quản lý vựa khác
            var existingNursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (existingNursery != null && existingNursery.Id != nurseryId)
                throw new BadRequestException($"This Manager is managing nursery '{existingNursery.Name}' (ID: {existingNursery.Id})");

            // Vựa đã có manager khác
            if (nursery.ManagerId.HasValue && nursery.ManagerId.Value != managerId)
                throw new BadRequestException($"Nursery with ID {nursery.Id} is already assigned to a different Manager (ID: {nursery.ManagerId.Value}). Please remove the existing manager first.");

            nursery.ManagerId = managerId;
            _unitOfWork.NurseryRepository.PrepareUpdate(nursery);

            manager.NurseryId = nurseryId;
            _unitOfWork.UserRepository.PrepareUpdate(manager);

            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();

            return nursery.ToResponse();
        }

        #endregion

        #region Manager Operations

        public async Task<NurseryResponseDto> UpdateMyNurseryAsync(int managerId, NurseryUpdateDto request)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new NotFoundException("You do not have a nursery to update");

            // Manager không được tự thay đổi ManagerId — chỉ Admin mới được qua AssignManagerAsync
            request.ManagerId = null;

            return await UpdateNurseryAsync(nursery.Id, request);
        }

        public async Task<List<NurseryMaterialExpiryAlertDto>> GetMyNurseryExpiringMaterialsAsync(int managerId, int daysAhead = 30)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new NotFoundException("You do not have a nursery to update");

            var cacheKey = $"{ALL_NURSERIES_KEY}_{nursery.Id}_expiring_materials_d{daysAhead}";
            var cachedData = await _cacheService.GetDataAsync<List<NurseryMaterialExpiryAlertDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var endDate = today.AddDays(daysAhead);

            var materials = await _unitOfWork.NurseryMaterialRepository.GetAllByNurseryIdAsync(nursery.Id);
            var result = materials
                .Where(m => m.IsActive
                            && m.ExpiredDate.HasValue
                            && m.ExpiredDate.Value >= today
                            && m.ExpiredDate.Value <= endDate
                            && m.Quantity > 0)
                .OrderBy(m => m.ExpiredDate)
                .Select(m => new NurseryMaterialExpiryAlertDto
                {
                    NurseryMaterialId = m.Id,
                    MaterialId = m.MaterialId,
                    MaterialName = m.Material?.Name,
                    MaterialCode = m.Material?.MaterialCode,
                    Unit = m.Material?.Unit,
                    Quantity = m.Quantity,
                    ReservedQuantity = m.ReservedQuantity,
                    ExpiredDate = m.ExpiredDate,
                    DaysToExpire = m.ExpiredDate!.Value.DayNumber - today.DayNumber
                })
                .ToList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(10));
            return result;
        }

        public async Task<List<NurseryLowStockProductAlertDto>> GetMyNurseryLowStockProductsAsync(int managerId, int threshold = 5)
        {
            var nursery = await ResolveOperatorNurseryForReadAsync(managerId);

            var cacheKey = $"{ALL_NURSERIES_KEY}_{nursery.Id}_low_stock_t{threshold}";
            var cachedData = await _cacheService.GetDataAsync<List<NurseryLowStockProductAlertDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var commonPlants = await _unitOfWork.CommonPlantRepository.GetAllByNurseryIdAsync(nursery.Id);
            var plantInstances = await _unitOfWork.PlantInstanceRepository.GetAllByNurseryIdAsync(nursery.Id);

            var commonPlantLowStock = commonPlants
                .Where(cp => cp.IsActive)
                .Select(cp => new NurseryLowStockProductAlertDto
                {
                    ProductType = "CommonPlant",
                    ProductId = cp.Id,
                    ProductName = cp.Plant?.Name,
                    TotalQuantity = cp.Quantity,
                    ReservedQuantity = cp.ReservedQuantity,
                    AvailableQuantity = cp.Quantity,
                    Threshold = threshold
                })
                .Where(x => x.AvailableQuantity >= 0 && x.AvailableQuantity <= threshold)
                .ToList();

            var identifiedPlantLowStock = plantInstances
                .Where(pi => pi.PlantId.HasValue)
                .GroupBy(pi => new { pi.PlantId, PlantName = pi.Plant != null ? pi.Plant.Name : null })
                .Select(g => new NurseryLowStockProductAlertDto
                {
                    ProductType = "PlantInstance",
                    ProductId = g.Key.PlantId ?? 0,
                    ProductName = g.Key.PlantName,
                    TotalQuantity = g.Count(),
                    ReservedQuantity = g.Count(x => x.Status == (int)PlantInstanceStatusEnum.Reserved),
                    AvailableQuantity = g.Count(x => x.Status == (int)PlantInstanceStatusEnum.Available),
                    Threshold = threshold
                })
                .Where(x => x.AvailableQuantity <= threshold)
                .ToList();

            var result = commonPlantLowStock
                .Concat(identifiedPlantLowStock)
                .OrderBy(x => x.AvailableQuantity)
                .ThenBy(x => x.ProductName)
                .ToList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(10));
            return result;
        }

        public async Task<NurseryMaterialSummaryResponseDto> GetMyNurseryMaterialSummaryAsync(int managerId, int lowStockThreshold = 5, int expiringInDays = 30)
        {
            var nursery = await ResolveOperatorNurseryForReadAsync(managerId);

            var cacheKey = $"{ALL_NURSERIES_KEY}_{nursery.Id}_inventory_summary_t{lowStockThreshold}_d{expiringInDays}";
            var cachedData = await _cacheService.GetDataAsync<NurseryMaterialSummaryResponseDto>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var materials = await _unitOfWork.NurseryMaterialRepository.GetAllByNurseryIdAsync(nursery.Id);
            var commonPlants = await _unitOfWork.CommonPlantRepository.GetAllByNurseryIdAsync(nursery.Id);
            var plantInstances = await _unitOfWork.PlantInstanceRepository.GetAllByNurseryIdAsync(nursery.Id);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var expireEndDate = today.AddDays(expiringInDays);

            var activeMaterials = materials.Where(m => m.IsActive).ToList();
            var activeCommonPlants = commonPlants.Where(cp => cp.IsActive).ToList();

            var summary = new NurseryMaterialSummaryResponseDto
            {
                NurseryId = nursery.Id,
                NurseryName = nursery.Name,
                GeneratedAt = DateTime.UtcNow,
                CommonPlants = new NurseryCommonPlantSummaryDto
                {
                    TotalProducts = activeCommonPlants.Count,
                    TotalQuantity = activeCommonPlants.Sum(cp => cp.Quantity),
                    TotalReservedQuantity = activeCommonPlants.Sum(cp => cp.ReservedQuantity),
                    TotalAvailableQuantity = activeCommonPlants.Sum(cp => cp.Quantity),
                    LowStockProducts = activeCommonPlants.Count(cp => cp.Quantity <= lowStockThreshold)
                },
                PlantInstances = new NurseryPlantInstanceSummaryDto
                {
                    TotalInstances = plantInstances.Count,
                    AvailableInstances = plantInstances.Count(pi => pi.Status == (int)PlantInstanceStatusEnum.Available),
                    ReservedInstances = plantInstances.Count(pi => pi.Status == (int)PlantInstanceStatusEnum.Reserved),
                    SoldInstances = plantInstances.Count(pi => pi.Status == (int)PlantInstanceStatusEnum.Sold),
                    DamagedInstances = plantInstances.Count(pi => pi.Status == (int)PlantInstanceStatusEnum.Damaged),
                    InactiveInstances = plantInstances.Count(pi => pi.Status == (int)PlantInstanceStatusEnum.Inactive),
                    LowStockPlants = plantInstances
                        .Where(pi => pi.PlantId.HasValue)
                        .GroupBy(pi => pi.PlantId)
                        .Count(g => g.Count(pi => pi.Status == (int)PlantInstanceStatusEnum.Available) <= lowStockThreshold)
                },
                Materials = new NurseryMaterialSummaryDto
                {
                    TotalProducts = activeMaterials.Count,
                    TotalQuantity = activeMaterials.Sum(m => m.Quantity),
                    TotalReservedQuantity = activeMaterials.Sum(m => m.ReservedQuantity),
                    TotalAvailableQuantity = activeMaterials.Sum(m => m.Quantity),
                    ExpiringSoonProducts = activeMaterials.Count(m => m.ExpiredDate.HasValue
                        && m.ExpiredDate.Value >= today
                        && m.ExpiredDate.Value <= expireEndDate
                        && m.Quantity > 0),
                    LowStockProducts = activeMaterials.Count(m => m.Quantity <= lowStockThreshold)
                }
            };

            await _cacheService.SetDataAsync(cacheKey, summary, DateTimeOffset.Now.AddMinutes(10));
            return summary;
        }

        public async Task<List<SystemLowStockProductAlertDto>> GetSystemLowStockProductsAsync(int threshold = 5)
        {
            var cacheKey = $"{ALL_NURSERIES_KEY}_system_low_stock_t{threshold}";
            var cachedData = await _cacheService.GetDataAsync<List<SystemLowStockProductAlertDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var commonPlants = await _unitOfWork.CommonPlantRepository.GetAllAsync();
            var plantInstances = await _unitOfWork.PlantInstanceRepository.GetAllAsync();
            var nurseries = await _unitOfWork.NurseryRepository.GetAllAsync();

            var nurseryNames = nurseries
                .Where(n => !string.IsNullOrEmpty(n.Name))
                .ToDictionary(n => n.Id, n => n.Name ?? string.Empty);

            var commonPlantLowStock = commonPlants
                .Where(cp => cp.IsActive)
                .Select(cp => new SystemLowStockProductAlertDto
                {
                    NurseryId = cp.NurseryId,
                    NurseryName = nurseryNames.TryGetValue(cp.NurseryId, out var name) ? name : string.Empty,
                    ProductType = "CommonPlant",
                    ProductId = cp.Id,
                    ProductName = cp.Plant?.Name,
                    TotalQuantity = cp.Quantity,
                    ReservedQuantity = cp.ReservedQuantity,
                    AvailableQuantity = cp.Quantity,
                    Threshold = threshold
                })
                .Where(x => x.AvailableQuantity >= 0 && x.AvailableQuantity <= threshold)
                .ToList();

            var identifiedPlantLowStock = plantInstances
                .Where(pi => pi.PlantId.HasValue && pi.CurrentNurseryId.HasValue)
                .GroupBy(pi => new
                {
                    pi.CurrentNurseryId,
                    pi.PlantId,
                    PlantName = pi.Plant != null ? pi.Plant.Name : null
                })
                .Select(g => new SystemLowStockProductAlertDto
                {
                    NurseryId = g.Key.CurrentNurseryId ?? 0,
                    NurseryName = g.Key.CurrentNurseryId.HasValue && nurseryNames.TryGetValue(g.Key.CurrentNurseryId.Value, out var name)
                        ? name
                        : string.Empty,
                    ProductType = "PlantInstance",
                    ProductId = g.Key.PlantId ?? 0,
                    ProductName = g.Key.PlantName,
                    TotalQuantity = g.Count(),
                    ReservedQuantity = g.Count(x => x.Status == (int)PlantInstanceStatusEnum.Reserved),
                    AvailableQuantity = g.Count(x => x.Status == (int)PlantInstanceStatusEnum.Available),
                    Threshold = threshold
                })
                .Where(x => x.AvailableQuantity <= threshold)
                .ToList();

            var result = commonPlantLowStock
                .Concat(identifiedPlantLowStock)
                .OrderBy(x => x.AvailableQuantity)
                .ThenBy(x => x.ProductName)
                .ToList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(10));
            return result;
        }

        #endregion

        #region Nearby & Staff

        public async Task<List<NurseryNearbyResponseDto>> GetNearbyNurseriesAsync(decimal lat, decimal lng, decimal radiusKm, int? packageId)
        {
            var nurseries = await _unitOfWork.NurseryRepository.GetNearbyWithPackageAsync(lat, lng, packageId);

            return nurseries.Select(n => new NurseryNearbyResponseDto
            {
                Id = n.Id,
                Name = n.Name,
                Address = n.Address,
                Phone = n.Phone,
                Latitude = n.Latitude,
                Longitude = n.Longitude,
                DistanceKm = Math.Round(HaversineKm((double)lat, (double)lng, (double)n.Latitude!.Value, (double)n.Longitude!.Value), 2),
                AvailableServices = n.NurseryCareServices
                    .Where(ncs => ncs.IsActive)
                    .Select(ncs => new NurseryCareServiceSummaryDto
                    {
                        Id = ncs.Id,
                        NurseryId = n.Id,
                        NurseryName = n.Name,
                        CareServicePackage = ncs.CareServicePackage == null ? null : new CareServicePackageSummaryDto
                        {
                            Id = ncs.CareServicePackage.Id,
                            Name = ncs.CareServicePackage.Name,
                            Description = ncs.CareServicePackage.Description,
                            VisitPerWeek = ncs.CareServicePackage.VisitPerWeek,
                            DurationDays = ncs.CareServicePackage.DurationDays,
                            ServiceType = ncs.CareServicePackage.ServiceType,
                            UnitPrice = ncs.CareServicePackage.UnitPrice
                        }
                    }).ToList()
            }).ToList();
        }

        public async Task<List<StaffWithSpecializationsResponseDto>> GetNurseryStaffAsync(int managerId)
        {
            var nursery = await ResolveOperatorNurseryForReadAsync(managerId);

            var staff = await _unitOfWork.UserRepository.GetCaretakersByNurseryIdAsync(nursery.Id);
            return staff.Select(MapToStaffDtoPublic).ToList();
        }

        public async Task<StaffWithSpecializationsResponseDto> GetNurseryStaffDetailAsync(int managerId, int staffId)
        {
            var nursery = await ResolveOperatorNurseryForReadAsync(managerId);

            var staff = await _unitOfWork.UserRepository.GetCaretakerByIdWithSpecializationsAsync(staffId, nursery.Id);
            if (staff == null)
                throw new NotFoundException($"Staff ID {staffId} is not associated with your nursery");

            return MapToStaffDtoPublic(staff);
        }

        public async Task<List<StaffWithSpecializationsResponseDto>> GetNurseryTeamForManagerAsync(int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not the manager of any nursery");

            var staff = await _unitOfWork.UserRepository.GetStaffAndCaretakersByNurseryIdAsync(nursery.Id);
            return staff.Select(MapToStaffDtoPublic).ToList();
        }

        public async Task<StaffWithSpecializationsResponseDto> GetNurseryTeamDetailForManagerAsync(int managerId, int staffId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not the manager of any nursery");

            var staff = await _unitOfWork.UserRepository.GetStaffOrCaretakerByIdWithSpecializationsAsync(staffId, nursery.Id);
            if (staff == null)
                throw new NotFoundException($"Staff ID {staffId} is not associated with your nursery");

            return MapToStaffDtoPublic(staff);
        }

        public static StaffWithSpecializationsResponseDto MapToStaffDtoPublic(DataAccessLayer.Entities.User user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            Specializations = user.StaffSpecializations.Select(ss => new SpecializationSummaryDto
            {
                Id = ss.Specialization.Id,
                Name = ss.Specialization.Name,
                Description = ss.Specialization.Description
            }).ToList()
        };

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        #endregion

        #region Private Methods

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveDataAsync(ALL_NURSERIES_KEY);
            await _cacheService.RemoveDataAsync(ACTIVE_NURSERIES_KEY);
            // Remove all paginated cache
            await _cacheService.RemoveByPrefixAsync($"{ALL_NURSERIES_KEY}_");
            await _cacheService.RemoveByPrefixAsync($"{ACTIVE_NURSERIES_KEY}_");
            await _cacheService.RemoveByPrefixAsync("plants_shop_search");
            await _cacheService.RemoveByPrefixAsync("plant_nurseries");
            await _cacheService.RemoveByPrefixAsync("plant_nurseries_common");
            await _cacheService.RemoveByPrefixAsync("nursery_common_plants");
            await _cacheService.RemoveByPrefixAsync("shop_unified_search");
        }

        private async Task<DataAccessLayer.Entities.Nursery> ResolveOperatorNurseryForReadAsync(int operatorId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(operatorId);
            if (nursery != null)
                return nursery;

            var user = await _unitOfWork.UserRepository.GetByIdAsync(operatorId);
            if (user?.RoleId == (int)RoleEnum.Staff && user.NurseryId.HasValue)
            {
                var staffNursery = await _unitOfWork.NurseryRepository.GetByIdAsync(user.NurseryId.Value);
                if (staffNursery != null)
                    return staffNursery;
            }

            throw new ForbiddenException("You are not associated with any nursery to view staff list");
        }

        #endregion
    }
}
