using Hangfire;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Embedding;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class CommonPlantService : ICommonPlantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        private const string ALL_COMMON_PLANTS_KEY = "common_plants_all";
        private const string NURSERY_COMMON_PLANTS_KEY = "nursery_common_plants";
        private const string PLANT_NURSERIES_COMMON_KEY = "plant_nurseries_common";
        private const string PLANT_SHOP_SEARCH = "plants_shop_search";

        public CommonPlantService(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _backgroundJobClient = backgroundJobClient;
        }

        #region CRUD Operations

        public async Task<PaginatedResult<CommonPlantListResponseDto>> GetAllCommonPlantsAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_COMMON_PLANTS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<CommonPlantListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.CommonPlantRepository.GetAllWithDetailsAsync(pagination);
            var result = new PaginatedResult<CommonPlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<CommonPlantResponseDto> GetCommonPlantByIdAsync(int id)
        {
            var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(id);
            if (commonPlant == null)
                throw new NotFoundException($"CommonPlant với ID {id} không tồn tại");

            return commonPlant.ToResponse();
        }

        public async Task<CommonPlantResponseDto> CreateCommonPlantAsync(int nurseryId, int managerId, CommonPlantRequestDto request)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != nurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý vựa này");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate plant exists
                var plant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId);
                if (plant == null)
                    throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

                // Check duplicate plant + nursery combination
                if (await _unitOfWork.CommonPlantRepository.ExistsAsync(request.PlantId, nurseryId))
                    throw new BadRequestException($"CommonPlant cho Plant ID {request.PlantId} tại Nursery ID {nurseryId} đã tồn tại");

                var entity = request.ToEntity();

                _unitOfWork.CommonPlantRepository.PrepareCreate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(entity.NurseryId);

                // Reload with details
                var created = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(entity.Id);

                // Queue embedding job (background)
                QueueEmbeddingAsync(created!);

                return created!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CommonPlantResponseDto> UpdateCommonPlantAsync(int id, CommonPlantUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var entity = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    throw new NotFoundException($"CommonPlant với ID {id} không tồn tại");

                // Validate reserved quantity doesn't exceed quantity
                var newQuantity = request.Quantity ?? entity.Quantity;
                var newReserved = request.ReservedQuantity ?? entity.ReservedQuantity;
                if (newReserved > newQuantity)
                    throw new BadRequestException("Số lượng đặt trước không thể lớn hơn số lượng tồn kho");

                request.ToUpdate(entity);

                _unitOfWork.CommonPlantRepository.PrepareUpdate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(entity.NurseryId);

                // Reload and update embedding
                var updated = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(id);
                QueueEmbeddingAsync(updated!);

                return updated!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeleteCommonPlantAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var entity = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    throw new NotFoundException($"CommonPlant với ID {id} không tồn tại");

                if (entity.ReservedQuantity > 0)
                    throw new BadRequestException("Không thể xóa common plant đang có số lượng đặt trước");

                _unitOfWork.CommonPlantRepository.PrepareRemove(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(entity.NurseryId);

                // Delete embedding
                await DeleteEmbeddingAsync(id);

                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region Query Operations

        public async Task<PaginatedResult<CommonPlantListResponseDto>> GetByPlantIdAsync(int plantId, Pagination pagination)
        {
            var paginatedEntities = await _unitOfWork.CommonPlantRepository.GetByPlantIdAsync(plantId, pagination);
            return new PaginatedResult<CommonPlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );
        }

        public async Task<PaginatedResult<CommonPlantListResponseDto>> GetByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var paginatedEntities = await _unitOfWork.CommonPlantRepository.GetByNurseryIdAsync(nurseryId, pagination);
            return new PaginatedResult<CommonPlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );
        }

        #endregion

        #region Stock Management

        public async Task<CommonPlantResponseDto> UpdateQuantityAsync(int nurseryId, int plantId, int quantity)
        {
            var entity = await _unitOfWork.CommonPlantRepository.GetByPlantAndNurseryAsync(plantId, nurseryId);
            if (entity == null)
                throw new NotFoundException($"Không tìm thấy tồn kho cho PlantId {plantId} tại NurseryId {nurseryId}");

            if (quantity < 0)
                throw new BadRequestException("Số lượng không thể âm");

            if (quantity < entity.ReservedQuantity)
                throw new BadRequestException("Số lượng không thể nhỏ hơn số lượng đã đặt trước");

            entity.Quantity = quantity;

            _unitOfWork.CommonPlantRepository.PrepareUpdate(entity);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync(entity.NurseryId);

            return entity.ToResponse();
        }

        #endregion

        #region Cache Management

        private async Task InvalidateCacheAsync(int? nurseryId = null)
        {
            await _cacheService.RemoveByPrefixAsync(ALL_COMMON_PLANTS_KEY);
            await _cacheService.RemoveByPrefixAsync(PLANT_NURSERIES_COMMON_KEY);
            await _cacheService.RemoveByPrefixAsync(PLANT_SHOP_SEARCH);
            await _cacheService.RemoveByPrefixAsync("nurseries_all_");
            await _cacheService.RemoveByPrefixAsync("shop_unified_search");
            if (nurseryId.HasValue)
            {
                await _cacheService.RemoveByPrefixAsync($"{NURSERY_COMMON_PLANTS_KEY}_{nurseryId.Value}");
            }
            else
            {
                await _cacheService.RemoveByPrefixAsync(NURSERY_COMMON_PLANTS_KEY);
            }
        }

        #endregion

        #region Manager Operations

        public async Task<CommonPlantResponseDto> CreateForNurseryAsync(int nurseryId, int managerId, CommonPlantRequestDto request)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != nurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý vựa này");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var plant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId);
                if (plant == null)
                    throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

                if (await _unitOfWork.CommonPlantRepository.ExistsAsync(request.PlantId, nurseryId))
                    throw new BadRequestException($"Cây đại trà cho Plant ID {request.PlantId} tại vựa này đã tồn tại");

                var entity = request.ToEntity();
                entity.NurseryId = nurseryId;
                _unitOfWork.CommonPlantRepository.PrepareCreate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(nurseryId);

                var created = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(entity.Id);

                // Queue embedding job
                QueueEmbeddingAsync(created!);

                return created!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PaginatedResult<CommonPlantListResponseDto>> GetByNurseryForManagerAsync(int nurseryId, int managerId, Pagination pagination)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != nurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý vựa này");

            var cacheKey = $"{NURSERY_COMMON_PLANTS_KEY}_{nurseryId}_manager_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<CommonPlantListResponseDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            var paginatedEntities = await _unitOfWork.CommonPlantRepository.GetByNurseryIdAsync(nurseryId, pagination);
            var result = new PaginatedResult<CommonPlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(15));
            return result;
        }

        public async Task<PaginatedResult<PlantListResponseDto>> GetPlantsNotInNurseryForManagerAsync(int nurseryId, int managerId, Pagination pagination)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != nurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý vựa này");

            var cacheKey = $"{NURSERY_COMMON_PLANTS_KEY}_{nurseryId}_missing_plants_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantListResponseDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            var paginatedEntities = await _unitOfWork.PlantRepository.GetActivePlantsNotInNurseryAsync(nurseryId, pagination);
            var result = new PaginatedResult<PlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(15));
            return result;
        }

        public async Task<CommonPlantResponseDto> UpdateForManagerAsync(int nurseryId, int commonPlantId, int managerId, CommonPlantUpdateDto request)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != nurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý vựa này");

            var entity = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(commonPlantId);
            if (entity == null || entity.NurseryId != nurseryId)
                throw new NotFoundException($"Không tìm thấy cây đại trà với ID {commonPlantId} tại vựa này");

            var newQuantity = request.Quantity ?? entity.Quantity;
            var newReserved = request.ReservedQuantity ?? entity.ReservedQuantity;
            if (newReserved > newQuantity)
                throw new BadRequestException("Số lượng đặt trước không thể lớn hơn số lượng tồn kho");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                request.ToUpdate(entity);
                _unitOfWork.CommonPlantRepository.PrepareUpdate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(nurseryId);
                return entity.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CommonPlantResponseDto> ToggleActiveAsync(int nurseryId, int commonPlantId, int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != nurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý vựa này");

            var entity = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(commonPlantId);
            if (entity == null || entity.NurseryId != nurseryId)
                throw new NotFoundException($"Không tìm thấy cây đại trà với ID {commonPlantId} tại vựa này");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                entity.IsActive = !entity.IsActive;
                _unitOfWork.CommonPlantRepository.PrepareUpdate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(nurseryId);
                return entity.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region Shop Operations

        public async Task<PaginatedResult<CommonPlantListResponseDto>> GetActiveByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var cacheKey = $"{NURSERY_COMMON_PLANTS_KEY}_{nurseryId}_active_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<CommonPlantListResponseDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            var paginatedEntities = await _unitOfWork.CommonPlantRepository.GetActiveByNurseryIdAsync(nurseryId, pagination);
            var result = new PaginatedResult<CommonPlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(10));
            return result;
        }

        public async Task<List<PlantNurseryAvailabilityDto>> GetNurseriesWithCommonPlantAsync(int plantId)
        {
            var cacheKey = $"{PLANT_NURSERIES_COMMON_KEY}_{plantId}";
            var cachedData = await _cacheService.GetDataAsync<List<PlantNurseryAvailabilityDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            var plant = await _unitOfWork.PlantRepository.GetByIdAsync(plantId);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {plantId} không tồn tại");

            var commonPlants = await _unitOfWork.CommonPlantRepository.GetActiveByPlantIdAsync(plantId);

            var result = commonPlants
                .Where(cp => cp.Nursery != null && cp.Nursery.IsActive == true)
                .Select(cp => new PlantNurseryAvailabilityDto
                {
                    CommonPlantId = cp.Id,
                    NurseryId = cp.Nursery!.Id,
                    NurseryName = cp.Nursery.Name,
                    Address = cp.Nursery.Address,
                    Phone = cp.Nursery.Phone,
                    Latitude = cp.Nursery.Latitude,
                    Longitude = cp.Nursery.Longitude,
                    AvailableInstanceCount = cp.Quantity,
                    MinPrice = 0,
                    MaxPrice = 0
                })
                .OrderByDescending(n => n.AvailableInstanceCount)
                .ToList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(10));
            return result;
        }

        public async Task<PaginatedResult<CommonPlantListResponseDto>> SearchCommonPlantsForShopAsync(CommonPlantShopSearchRequestDto searchRequest, Pagination pagination)
        {
            var paginatedEntities = await _unitOfWork.CommonPlantRepository.SearchForShopAsync(
                pagination,
                searchRequest.SearchTerm,
                searchRequest.CategoryIds,
                searchRequest.TagIds,
                searchRequest.Sizes,
                searchRequest.MinPrice,
                searchRequest.MaxPrice,
                searchRequest.SortBy,
                searchRequest.SortDirection);

            return new PaginatedResult<CommonPlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );
        }

        #endregion

        #region Embedding Operations

        private void QueueEmbeddingAsync(CommonPlant entity)
        {
            try
            {
                var plant = entity.Plant;
                var guide = plant?.PlantGuide;

                var embeddingDto = new CommonPlantEmbeddingDto
                {
                    CommonPlantId = entity.Id,
                    PlantId = entity.PlantId,
                    IsActive = entity.IsActive,
                    PlantName = plant?.Name ?? string.Empty,
                    PlantSpecificName = plant?.SpecificName,
                    PlantDescription = plant?.Description,
                    PlantOrigin = plant?.Origin,
                    FengShuiElement = plant?.FengShuiElement,
                    FengShuiMeaning = plant?.FengShuiMeaning,
                    Size = plant?.Size,
                    PlacementType = plant?.PlacementType ?? 0,
                    PetSafe = plant?.PetSafe,
                    ChildSafe = plant?.ChildSafe,
                    AirPurifying = plant?.AirPurifying,
                    BasePrice = plant?.BasePrice,
                    CategoryNames = plant?.Categories?
                        .Select(c => c.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList() ?? new List<string>(),
                    TagNames = plant?.Tags?
                        .Select(t => t.TagName)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList() ?? new List<string>(),
                    RoomTypes = plant?.RoomType?.ToList() ?? new List<int>(),
                    RoomTypeNames = GetRoomTypeNames(plant?.RoomType),
                    RoomStyles = plant?.RoomStyle?.ToList() ?? new List<int>(),
                    RoomStyleNames = GetRoomStyleNames(plant?.RoomStyle),
                    NurseryId = entity.NurseryId,
                    NurseryName = entity.Nursery?.Name,
                    Price = plant?.BasePrice,
                    GuideLightRequirement = guide?.LightRequirement
                };

                var entityId = ConvertToGuid(entity.Id);

                // Queue Hangfire background job for local PostgreSQL
                _backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                    service => service.ProcessCommonPlantEmbeddingAsync(embeddingDto, entityId, EmbeddingEntityTypes.CommonPlant));
            }
            catch
            {
                // Log but don't fail the main operation
            }
        }

        private async Task DeleteEmbeddingAsync(int entityId)
        {
            try
            {
                var guid = ConvertToGuid(entityId);
                await _unitOfWork.EmbeddingRepository.DeleteByEntityAsync(EmbeddingEntityTypes.CommonPlant, guid);
            }
            catch
            {
                // Log but don't fail
            }
        }

        private static List<string> GetRoomTypeNames(List<int>? roomTypes)
        {
            if (roomTypes == null || roomTypes.Count == 0)
            {
                return new List<string>();
            }

            return roomTypes
                .Distinct()
                .Select(roomType => Enum.IsDefined(typeof(RoomTypeEnum), roomType)
                    ? ((RoomTypeEnum)roomType).ToString()
                    : roomType.ToString())
                .ToList();
        }

        private static List<string> GetRoomStyleNames(List<int>? roomStyles)
        {
            if (roomStyles == null || roomStyles.Count == 0)
            {
                return new List<string>();
            }

            return roomStyles
                .Distinct()
                .Select(roomStyle => Enum.IsDefined(typeof(RoomStyleEnum), roomStyle)
                    ? ((RoomStyleEnum)roomStyle).ToString()
                    : roomStyle.ToString())
                .ToList();
        }

        private static Guid ConvertToGuid(int id)
            => new Guid(id.ToString().PadLeft(32, '0'));

        #endregion
    }
}
