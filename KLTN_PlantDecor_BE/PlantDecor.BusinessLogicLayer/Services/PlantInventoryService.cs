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
    public class CommonPlantService : ICommonPlantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_COMMON_PLANTS_KEY = "common_plants_all";

        public CommonPlantService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
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

        public async Task<CommonPlantResponseDto?> GetCommonPlantByIdAsync(int id)
        {
            var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(id);
            if (commonPlant == null)
                return null;

            return commonPlant.ToResponse();
        }

        public async Task<CommonPlantResponseDto> CreateCommonPlantAsync(CommonPlantRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate plant exists
                var plant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId);
                if (plant == null)
                    throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

                // Check duplicate plant + nursery combination
                if (await _unitOfWork.CommonPlantRepository.ExistsAsync(request.PlantId, request.NurseryId))
                    throw new BadRequestException($"CommonPlant cho Plant ID {request.PlantId} tại Nursery ID {request.NurseryId} đã tồn tại");

                var entity = request.ToEntity();

                _unitOfWork.CommonPlantRepository.PrepareCreate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                // Reload with details
                var created = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(entity.Id);
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

                await InvalidateCacheAsync();

                return entity.ToResponse();
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

            await InvalidateCacheAsync();

            return entity.ToResponse();
        }

        #endregion

        #region Cache Management

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(ALL_COMMON_PLANTS_KEY);
        }

        #endregion
    }
}
