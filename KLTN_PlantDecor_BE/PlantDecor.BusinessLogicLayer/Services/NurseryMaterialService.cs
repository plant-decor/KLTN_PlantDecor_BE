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
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class NurseryMaterialService : INurseryMaterialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILangflowService _langflowService;

        private const string ALL_NURSERY_MATERIALS_KEY = "nursery_materials_all";
        private const string NURSERIES_BY_MATERIAL_KEY = "nurseries_by_material";

        public NurseryMaterialService(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IBackgroundJobClient backgroundJobClient,
            ILangflowService langflowService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _backgroundJobClient = backgroundJobClient;
            _langflowService = langflowService;
        }

        #region CRUD Operations

        public async Task<PaginatedResult<NurseryMaterialListResponseDto>> GetAllNurseryMaterialsAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_NURSERY_MATERIALS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<NurseryMaterialListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.NurseryMaterialRepository.GetAllWithDetailsAsync(pagination);
            var result = new PaginatedResult<NurseryMaterialListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<NurseryMaterialResponseDto?> GetNurseryMaterialByIdAsync(int id)
        {
            var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(id);
            if (nurseryMaterial == null)
                return null;

            return nurseryMaterial.ToResponse();
        }

        public async Task<NurseryMaterialResponseDto> CreateNurseryMaterialAsync(NurseryMaterialRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate material exists
                var material = await _unitOfWork.MaterialRepository.GetByIdAsync(request.MaterialId);
                if (material == null)
                    throw new NotFoundException($"Material với ID {request.MaterialId} không tồn tại");

                // Validate nursery exists
                var nursery = await _unitOfWork.NurseryRepository.GetByIdAsync(request.NurseryId);
                if (nursery == null)
                    throw new NotFoundException($"Nursery với ID {request.NurseryId} không tồn tại");

                // Check duplicate material + nursery combination
                if (await _unitOfWork.NurseryMaterialRepository.ExistsAsync(request.MaterialId, request.NurseryId))
                    throw new BadRequestException($"Material đã tồn tại trong vựa này");

                var entity = request.ToEntity();

                _unitOfWork.NurseryMaterialRepository.PrepareCreate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(entity.MaterialId);

                // Reload with details
                var created = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(entity.Id);

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

        public async Task<NurseryMaterialResponseDto> UpdateNurseryMaterialAsync(int id, NurseryMaterialUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var entity = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    throw new NotFoundException($"NurseryMaterial với ID {id} không tồn tại");

                // Validate reserved quantity doesn't exceed quantity
                var newQuantity = request.Quantity ?? entity.Quantity;

                request.ToUpdate(entity);

                _unitOfWork.NurseryMaterialRepository.PrepareUpdate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(entity.MaterialId);

                // Update embedding
                var reloaded = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(id);
                QueueEmbeddingAsync(reloaded!);

                return entity.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeleteNurseryMaterialAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var entity = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    throw new NotFoundException($"NurseryMaterial với ID {id} không tồn tại");

                if (entity.ReservedQuantity > 0)
                    throw new BadRequestException("Không thể xóa vật tư đang có số lượng đặt trước");

                _unitOfWork.NurseryMaterialRepository.PrepareRemove(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(entity.MaterialId);

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

        public async Task<NurseryMaterialResponseDto> ToggleActiveAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var entity = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    throw new NotFoundException($"NurseryMaterial với ID {id} không tồn tại");

                if (entity.IsActive && entity.ReservedQuantity > 0)
                    throw new BadRequestException("Không thể tắt vật tư đang có số lượng đặt trước");

                entity.IsActive = !entity.IsActive;

                _unitOfWork.NurseryMaterialRepository.PrepareUpdate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(entity.MaterialId);

                // Update embedding
                var reloaded = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(id);
                QueueEmbeddingAsync(reloaded!);

                return entity.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region Query Operations

        public async Task<PaginatedResult<NurseryMaterialListResponseDto>> GetByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var paginatedEntities = await _unitOfWork.NurseryMaterialRepository.GetByNurseryIdAsync(nurseryId, pagination);
            return new PaginatedResult<NurseryMaterialListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );
        }

        public async Task<PaginatedResult<NurseryMaterialListResponseDto>> GetByMaterialIdAsync(int materialId, Pagination pagination)
        {
            var paginatedEntities = await _unitOfWork.NurseryMaterialRepository.GetByMaterialIdAsync(materialId, pagination);
            return new PaginatedResult<NurseryMaterialListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );
        }

        #endregion

        #region Stock Management

        public async Task<NurseryMaterialResponseDto> ImportMaterialAsync(int nurseryId, ImportMaterialRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate nursery exists
                var nursery = await _unitOfWork.NurseryRepository.GetByIdAsync(nurseryId);
                if (nursery == null)
                    throw new NotFoundException($"Nursery với ID {nurseryId} không tồn tại");

                // Validate material exists
                var material = await _unitOfWork.MaterialRepository.GetByIdAsync(request.MaterialId);
                if (material == null)
                    throw new NotFoundException($"Material với ID {request.MaterialId} không tồn tại");

                // Get or create NurseryMaterial
                var entity = await _unitOfWork.NurseryMaterialRepository.GetByMaterialAndNurseryAsync(request.MaterialId, nurseryId);

                NurseryMaterial targerEntity;

                if (entity != null && entity.ExpiredDate == request.ExpiredDate)
                {
                    // Update quantity
                    entity.Quantity += request.Quantity;
                    _unitOfWork.NurseryMaterialRepository.PrepareUpdate(entity);
                    targerEntity = entity;
                }
                else
                {
                    // Create new (case: entity null OR khác ExpiredDate)
                    var newEntity = new NurseryMaterial
                    {
                        MaterialId = request.MaterialId,
                        NurseryId = nurseryId,
                        Quantity = request.Quantity,
                        ExpiredDate = request.ExpiredDate,
                        ReservedQuantity = 0,
                        IsActive = true
                    };

                    _unitOfWork.NurseryMaterialRepository.PrepareCreate(newEntity);
                    targerEntity = newEntity;
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                // Reload with details
                var result = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(targerEntity.Id);
                return result!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<NurseryMaterialResponseDto> UpdateQuantityAsync(int nurseryId, int materialId, int quantity)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var entity = await _unitOfWork.NurseryMaterialRepository.GetByMaterialAndNurseryAsync(materialId, nurseryId);
                if (entity == null)
                    throw new NotFoundException($"Material {materialId} không tồn tại trong Nursery {nurseryId}");

                if (quantity < entity.ReservedQuantity)
                    throw new BadRequestException($"Số lượng không thể nhỏ hơn số lượng đã đặt trước ({entity.ReservedQuantity})");

                entity.Quantity = quantity;
                _unitOfWork.NurseryMaterialRepository.PrepareUpdate(entity);
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

        #endregion

        #region Manager Operations

        public async Task<PaginatedResult<NurseryMaterialListResponseDto>> GetMyNurseryMaterialsAsync(int managerId, Pagination pagination)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new NotFoundException("Bạn chưa có vựa nào");

            return await GetByNurseryIdAsync(nursery.Id, pagination);
        }

        public async Task<NurseryMaterialResponseDto> ImportToMyNurseryAsync(int managerId, ImportMaterialRequestDto request)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new NotFoundException("Bạn chưa có vựa nào");

            return await ImportMaterialAsync(nursery.Id, request);
        }

        #endregion

        #region Shop Operations

        public async Task<PaginatedResult<NurseryMaterialListResponseDto>> SearchNurseryMaterialsForShopAsync(NurseryMaterialShopSearchRequestDto searchRequest, Pagination pagination)
        {
            var paginatedEntities = await _unitOfWork.NurseryMaterialRepository.SearchForShopAsync(
                pagination,
                searchRequest.SearchTerm,
                searchRequest.CategoryIds,
                searchRequest.TagIds,
                searchRequest.MinPrice,
                searchRequest.MaxPrice,
                searchRequest.SortBy,
                searchRequest.SortDirection);

            return new PaginatedResult<NurseryMaterialListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );
        }

        #endregion

        #region Private Methods

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveDataAsync(ALL_NURSERY_MATERIALS_KEY);
            await _cacheService.RemoveByPrefixAsync($"{ALL_NURSERY_MATERIALS_KEY}_");
            await _cacheService.RemoveByPrefixAsync("nurseries_all_");
            await _cacheService.RemoveByPrefixAsync("shop_unified_search");
        }

        private async Task InvalidateCacheAsync(int? materialId = null)
        {
            await _cacheService.RemoveByPrefixAsync(ALL_NURSERY_MATERIALS_KEY);
            await _cacheService.RemoveByPrefixAsync("shop_unified_search");
            if (materialId.HasValue)
            {
                await _cacheService.RemoveByPrefixAsync($"{NURSERIES_BY_MATERIAL_KEY}_{materialId.Value}");
            }
        }

        #endregion

        #region Embedding Operations

        private void QueueEmbeddingAsync(NurseryMaterial entity)
        {
            try
            {
                var material = entity.Material;

                var embeddingDto = new NurseryMaterialEmbeddingDto
                {
                    NurseryMaterialId = entity.Id,
                    IsActive = entity.IsActive,
                    ExpiredDate = entity.ExpiredDate,
                    MaterialName = material?.Name ?? string.Empty,
                    Description = material?.Description,
                    Brand = material?.Brand,
                    Unit = material?.Unit,
                    Specifications = material?.Specifications,
                    BasePrice = material?.BasePrice,
                    CategoryNames = material?.Categories?
                        .Select(c => c.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList() ?? new List<string>(),
                    TagNames = material?.Tags?
                        .Select(t => t.TagName)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList() ?? new List<string>(),
                    NurseryId = entity.NurseryId,
                    NurseryName = entity.Nursery?.Name,
                    Price = material?.BasePrice
                };

                var entityId = ConvertToGuid(entity.Id);

                // Queue Hangfire background job for local PostgreSQL
                _backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                    service => service.ProcessNurseryMaterialEmbeddingAsync(embeddingDto, entityId, EmbeddingEntityTypes.NurseryMaterial));

                // Send to Langflow webhook via Hangfire
                //_backgroundJobClient.Enqueue<ILangflowBackgroundJobService>(
                //    service => service.ProcessNurseryMaterialIngestionAsync(embeddingDto, entityId, EmbeddingEntityTypes.NurseryMaterial));
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
                await _unitOfWork.EmbeddingRepository.DeleteByEntityAsync(EmbeddingEntityTypes.NurseryMaterial, guid);
            }
            catch
            {
                // Log but don't fail
            }
        }

        private static Guid ConvertToGuid(int id)
            => new Guid(id.ToString().PadLeft(32, '0'));

        #endregion
    }
}
