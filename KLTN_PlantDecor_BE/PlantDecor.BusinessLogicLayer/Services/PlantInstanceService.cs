using Microsoft.EntityFrameworkCore;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class PlantInstanceService : IPlantInstanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_INSTANCES_KEY = "plant_instances_all";
        private const string INSTANCES_BY_PLANT_PREFIX = "plant_instances_plant_";

        public PlantInstanceService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<List<PlantInstanceResponseDto>> GetAllInstancesAsync()
        {
            var cachedData = await _cacheService.GetDataAsync<List<PlantInstanceResponseDto>>(ALL_INSTANCES_KEY);
            if (cachedData != null)
                return cachedData;

            var instances = await _unitOfWork.PlantInstanceRepository.GetAllWithPlantAsync();
            var result = instances.ToResponseList();

            await _cacheService.SetDataAsync(ALL_INSTANCES_KEY, result, DateTimeOffset.Now.AddMinutes(15));
            return result;
        }

        public async Task<List<PlantInstanceResponseDto>> GetInstancesByPlantIdAsync(int plantId)
        {
            var cacheKey = $"{INSTANCES_BY_PLANT_PREFIX}{plantId}";
            var cachedData = await _cacheService.GetDataAsync<List<PlantInstanceResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var plant = await _unitOfWork.PlantRepository.GetByIdAsync(plantId);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {plantId} không tồn tại");

            var instances = await _unitOfWork.PlantInstanceRepository.GetByPlantIdAsync(plantId);
            var result = instances.ToResponseList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(15));
            return result;
        }

        public async Task<PlantInstanceResponseDto?> GetInstanceByIdAsync(int id)
        {
            var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithPlantAsync(id);
            if (instance == null)
                return null;

            return instance.ToResponse();
        }

        public async Task<PlantInstanceResponseDto> CreateInstanceAsync(PlantInstanceRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate plant exists
                var plant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId);
                if (plant == null)
                    throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

                var instance = request.ToEntity(plant.BasePrice);

                _unitOfWork.PlantInstanceRepository.PrepareCreate(instance);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(request.PlantId);

                // Set plant info for response
                instance.Plant = plant;

                return instance.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantInstanceResponseDto> UpdateInstanceAsync(int id, PlantInstanceUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithPlantAsync(id);
                if (instance == null)
                    throw new NotFoundException($"Plant Instance với ID {id} không tồn tại");

                request.ToUpdate(instance);

                _unitOfWork.PlantInstanceRepository.PrepareUpdate(instance);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(instance.PlantId ?? 0);

                return instance.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeleteInstanceAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithOrdersAsync(id);
                if (instance == null)
                    throw new NotFoundException($"Plant Instance với ID {id} không tồn tại");

                // Check if instance is in cart or orders
                if (instance.CartItems.Any())
                    throw new BadRequestException("Không thể xóa instance đang có trong giỏ hàng.");

                if (instance.OrderItems.Any())
                    throw new BadRequestException("Không thể xóa instance đã có trong đơn hàng. Vui lòng đổi trạng thái thay vì xóa.");

                // Soft delete by setting status to Unavailable
                instance.Status = "Unavailable";
                instance.UpdatedAt = DateTime.Now;

                _unitOfWork.PlantInstanceRepository.PrepareUpdate(instance);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(instance.PlantId ?? 0);

                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var instance = await _unitOfWork.PlantInstanceRepository.GetByIdAsync(id);
            if (instance == null)
                throw new NotFoundException($"Plant Instance với ID {id} không tồn tại");

            // Validate status
            var validStatuses = new[] { "Available", "Reserved", "Sold", "Unavailable" };
            if (!validStatuses.Contains(status))
                throw new BadRequestException($"Status không hợp lệ. Các giá trị hợp lệ: {string.Join(", ", validStatuses)}");

            instance.Status = status;
            instance.UpdatedAt = DateTime.Now;

            _unitOfWork.PlantInstanceRepository.PrepareUpdate(instance);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync(instance.PlantId ?? 0);

            return true;
        }

        #region Cache Management

        private async Task InvalidateCacheAsync(int plantId)
        {
            await _cacheService.RemoveDataAsync(ALL_INSTANCES_KEY);
            await _cacheService.RemoveDataAsync($"{INSTANCES_BY_PLANT_PREFIX}{plantId}");
        }

        #endregion
    }
}
