using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
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

        public async Task<NurseryResponseDto?> GetNurseryByIdAsync(int id)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByIdWithDetailsAsync(id);
            if (nursery == null)
                return null;

            return nursery.ToResponse();
        }

        public async Task<NurseryResponseDto?> GetMyNurseryAsync(int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                return null;

            // Load full details
            var fullNursery = await _unitOfWork.NurseryRepository.GetByIdWithDetailsAsync(nursery.Id);
            return fullNursery?.ToResponse();
        }

        public async Task<NurseryResponseDto> CreateNurseryAsync(int managerId, NurseryRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Check if manager already has a nursery
                var existingNursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
                if (existingNursery != null)
                    throw new BadRequestException("Manager đã có vựa, không thể tạo thêm");

                // Check duplicate name
                if (await _unitOfWork.NurseryRepository.ExistsByNameAsync(request.Name))
                    throw new BadRequestException($"Vựa với tên '{request.Name}' đã tồn tại");

                var entity = request.ToEntity(managerId);

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
                    throw new NotFoundException($"Vựa với ID {id} không tồn tại");

                // Check duplicate name if updating
                if (!string.IsNullOrEmpty(request.Name) && 
                    await _unitOfWork.NurseryRepository.ExistsByNameAsync(request.Name, id))
                    throw new BadRequestException($"Vựa với tên '{request.Name}' đã tồn tại");

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
                throw new NotFoundException($"Nursery với ID {id} không tồn tại");

            nursery.IsActive = !nursery.IsActive;

            _unitOfWork.NurseryRepository.PrepareUpdate(nursery);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();

            return nursery.IsActive ?? true;
        }

        #endregion

        #region Manager Operations

        public async Task<NurseryResponseDto> UpdateMyNurseryAsync(int managerId, NurseryUpdateDto request)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new NotFoundException("Bạn chưa có vựa nào");

            return await UpdateNurseryAsync(nursery.Id, request);
        }

        public async Task<List<NurseryMaterialExpiryAlertDto>> GetMyNurseryExpiringMaterialsAsync(int managerId, int daysAhead = 30)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new NotFoundException("Bạn chưa có vựa nào");

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
                            && m.Quantity > m.ReservedQuantity)
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
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new NotFoundException("Bạn chưa có vựa nào");

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
                    AvailableQuantity = cp.Quantity - cp.ReservedQuantity,
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

        public async Task<NurseryInventorySummaryDto> GetMyNurseryInventorySummaryAsync(int managerId, int lowStockThreshold = 5, int expiringInDays = 30)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new NotFoundException("Bạn chưa có vựa nào");

            var cacheKey = $"{ALL_NURSERIES_KEY}_{nursery.Id}_inventory_summary_t{lowStockThreshold}_d{expiringInDays}";
            var cachedData = await _cacheService.GetDataAsync<NurseryInventorySummaryDto>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var materials = await _unitOfWork.NurseryMaterialRepository.GetAllByNurseryIdAsync(nursery.Id);
            var commonPlants = await _unitOfWork.CommonPlantRepository.GetAllByNurseryIdAsync(nursery.Id);
            var plantInstances = await _unitOfWork.PlantInstanceRepository.GetAllByNurseryIdAsync(nursery.Id);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var expireEndDate = today.AddDays(expiringInDays);

            var activeMaterials = materials.Where(m => m.IsActive).ToList();
            var activeCommonPlants = commonPlants.Where(cp => cp.IsActive).ToList();

            var summary = new NurseryInventorySummaryDto
            {
                NurseryId = nursery.Id,
                NurseryName = nursery.Name,
                GeneratedAt = DateTime.UtcNow,
                CommonPlants = new NurseryCommonPlantSummaryDto
                {
                    TotalProducts = activeCommonPlants.Count,
                    TotalQuantity = activeCommonPlants.Sum(cp => cp.Quantity),
                    TotalReservedQuantity = activeCommonPlants.Sum(cp => cp.ReservedQuantity),
                    TotalAvailableQuantity = activeCommonPlants.Sum(cp => cp.Quantity - cp.ReservedQuantity),
                    LowStockProducts = activeCommonPlants.Count(cp => (cp.Quantity - cp.ReservedQuantity) <= lowStockThreshold)
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
                    TotalAvailableQuantity = activeMaterials.Sum(m => m.Quantity - m.ReservedQuantity),
                    ExpiringSoonProducts = activeMaterials.Count(m => m.ExpiredDate.HasValue
                        && m.ExpiredDate.Value >= today
                        && m.ExpiredDate.Value <= expireEndDate
                        && m.Quantity > m.ReservedQuantity),
                    LowStockProducts = activeMaterials.Count(m => (m.Quantity - m.ReservedQuantity) <= lowStockThreshold)
                }
            };

            await _cacheService.SetDataAsync(cacheKey, summary, DateTimeOffset.Now.AddMinutes(10));
            return summary;
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
        }

        #endregion
    }
}
