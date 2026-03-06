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
