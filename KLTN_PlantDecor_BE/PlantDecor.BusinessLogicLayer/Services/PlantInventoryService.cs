using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class PlantInventoryService : IPlantInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_PLANT_INVENTORIES_KEY = "plant_inventories_all";

        public PlantInventoryService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        #region CRUD Operations

        public async Task<PaginatedResult<PlantInventoryListResponseDto>> GetAllPlantInventoriesAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_PLANT_INVENTORIES_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantInventoryListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.PlantInventoryRepository.GetAllWithDetailsAsync(pagination);
            var result = new PaginatedResult<PlantInventoryListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PlantInventoryResponseDto?> GetPlantInventoryByIdAsync(int id)
        {
            var inventory = await _unitOfWork.PlantInventoryRepository.GetByIdWithDetailsAsync(id);
            if (inventory == null)
                return null;

            return inventory.ToResponse();
        }

        public async Task<PlantInventoryResponseDto> CreatePlantInventoryAsync(PlantInventoryRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate plant exists
                var plant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId);
                if (plant == null)
                    throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

                // Check duplicate plant + nursery combination
                if (await _unitOfWork.PlantInventoryRepository.ExistsAsync(request.PlantId, request.NurseryId))
                    throw new BadRequestException($"Plant inventory cho Plant ID {request.PlantId} tại Nursery ID {request.NurseryId} đã tồn tại");

                var entity = request.ToEntity();

                _unitOfWork.PlantInventoryRepository.PrepareCreate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                // Reload with details
                var created = await _unitOfWork.PlantInventoryRepository.GetByIdWithDetailsAsync(entity.Id);
                return created!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantInventoryResponseDto> UpdatePlantInventoryAsync(int id, PlantInventoryUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var entity = await _unitOfWork.PlantInventoryRepository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    throw new NotFoundException($"PlantInventory với ID {id} không tồn tại");

                // Validate reserved quantity doesn't exceed quantity
                var newQuantity = request.Quantity ?? entity.Quantity;
                var newReserved = request.ReservedQuantity ?? entity.ReservedQuantity;
                if (newReserved > newQuantity)
                    throw new BadRequestException("Số lượng đặt trước không thể lớn hơn số lượng tồn kho");

                request.ToUpdate(entity);

                _unitOfWork.PlantInventoryRepository.PrepareUpdate(entity);
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

        public async Task<bool> DeletePlantInventoryAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var entity = await _unitOfWork.PlantInventoryRepository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    throw new NotFoundException($"PlantInventory với ID {id} không tồn tại");

                if (entity.ReservedQuantity > 0)
                    throw new BadRequestException("Không thể xóa plant inventory đang có số lượng đặt trước");

                _unitOfWork.PlantInventoryRepository.PrepareRemove(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

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

        public async Task<PaginatedResult<PlantInventoryListResponseDto>> GetByPlantIdAsync(int plantId, Pagination pagination)
        {
            var paginatedEntities = await _unitOfWork.PlantInventoryRepository.GetByPlantIdAsync(plantId, pagination);
            return new PaginatedResult<PlantInventoryListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );
        }

        public async Task<PaginatedResult<PlantInventoryListResponseDto>> GetByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var paginatedEntities = await _unitOfWork.PlantInventoryRepository.GetByNurseryIdAsync(nurseryId, pagination);
            return new PaginatedResult<PlantInventoryListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );
        }

        #endregion

        #region Stock Management

        public async Task<PlantInventoryResponseDto> UpdateQuantityAsync(int nurseryId, int plantId, int quantity)
        {
            var entity = await _unitOfWork.PlantInventoryRepository.GetByPlantAndNurseryAsync(plantId, nurseryId);
            if (entity == null)
                throw new NotFoundException($"Không tìm thấy tồn kho cho PlantId {plantId} tại NurseryId {nurseryId}");

            if (quantity < 0)
                throw new BadRequestException("Số lượng không thể âm");

            if (quantity < entity.ReservedQuantity)
                throw new BadRequestException("Số lượng không thể nhỏ hơn số lượng đã đặt trước");

            entity.Quantity = quantity;

            _unitOfWork.PlantInventoryRepository.PrepareUpdate(entity);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return entity.ToResponse();
        }

        #endregion

        #region Cache Management

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(ALL_PLANT_INVENTORIES_KEY);
        }

        #endregion
    }
}
