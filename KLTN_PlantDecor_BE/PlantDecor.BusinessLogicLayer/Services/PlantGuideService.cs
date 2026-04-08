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
    public class PlantGuideService : IPlantGuideService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_PLANT_GUIDES_KEY = "plant_guides_all";
        private const string PLANT_GUIDES_BY_PLANT_KEY = "plant_guides_plant";

        public PlantGuideService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<PaginatedResult<PlantGuideResponseDto>> GetAllPlantGuidesAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_PLANT_GUIDES_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantGuideResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.PlantGuideRepository.GetAllWithPlantAsync(pagination);
            var result = new PaginatedResult<PlantGuideResponseDto>(
                paginatedEntities.Items.ToResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PlantGuideResponseDto?> GetPlantGuideByPlantIdAsync(int plantId)
        {
            var plant = await _unitOfWork.PlantRepository.GetByIdAsync(plantId);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {plantId} không tồn tại");

            var cacheKey = $"{PLANT_GUIDES_BY_PLANT_KEY}_{plantId}";
            var cachedData = await _cacheService.GetDataAsync<PlantGuideResponseDto>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var entity = await _unitOfWork.PlantGuideRepository.GetByPlantIdWithPlantAsync(plantId);
            if (entity == null)
                return null;

            var result = entity.ToResponse();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PlantGuideResponseDto?> GetPlantGuideByIdAsync(int id)
        {
            var entity = await _unitOfWork.PlantGuideRepository.GetByIdWithPlantAsync(id);
            if (entity == null)
                return null;

            return entity.ToResponse();
        }

        public async Task<PlantGuideResponseDto> CreatePlantGuideAsync(PlantGuideRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var plant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId);
                if (plant == null)
                    throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

                if (await _unitOfWork.PlantGuideRepository.ExistsByPlantIdAsync(request.PlantId))
                    throw new BadRequestException($"Plant với ID {request.PlantId} đã có PlantGuide");

                var entity = request.ToEntity();
                _unitOfWork.PlantGuideRepository.PrepareCreate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(request.PlantId);

                var created = await _unitOfWork.PlantGuideRepository.GetByIdWithPlantAsync(entity.Id);
                return created!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantGuideResponseDto> UpdatePlantGuideAsync(int id, PlantGuideUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var entity = await _unitOfWork.PlantGuideRepository.GetByIdWithPlantAsync(id);
                if (entity == null)
                    throw new NotFoundException($"PlantGuide với ID {id} không tồn tại");

                var oldPlantId = entity.PlantId;

                if (request.PlantId.HasValue)
                {
                    var targetPlant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId.Value);
                    if (targetPlant == null)
                        throw new NotFoundException($"Plant với ID {request.PlantId.Value} không tồn tại");

                    if (await _unitOfWork.PlantGuideRepository.ExistsByPlantIdAsync(request.PlantId.Value, id))
                        throw new BadRequestException($"Plant với ID {request.PlantId.Value} đã có PlantGuide khác");
                }

                request.ToUpdate(entity);

                _unitOfWork.PlantGuideRepository.PrepareUpdate(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(oldPlantId, entity.PlantId);

                var updated = await _unitOfWork.PlantGuideRepository.GetByIdWithPlantAsync(id);
                return updated!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeletePlantGuideAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var entity = await _unitOfWork.PlantGuideRepository.GetByIdWithPlantAsync(id);
                if (entity == null)
                    throw new NotFoundException($"PlantGuide với ID {id} không tồn tại");

                var plantId = entity.PlantId;

                _unitOfWork.PlantGuideRepository.PrepareRemove(entity);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(plantId);

                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task InvalidateCacheAsync(params int[] plantIds)
        {
            await _cacheService.RemoveByPrefixAsync(ALL_PLANT_GUIDES_KEY);

            foreach (var plantId in plantIds.Distinct())
            {
                await _cacheService.RemoveByPrefixAsync($"{PLANT_GUIDES_BY_PLANT_KEY}_{plantId}");
            }
        }
    }
}
