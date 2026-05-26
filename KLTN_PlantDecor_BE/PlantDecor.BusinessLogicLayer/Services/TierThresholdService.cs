using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class TierThresholdService : ITierThresholdService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TierThresholdService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<TierThresholdResponseDto>> GetAllActiveAsync()
        {
            var thresholds = await _unitOfWork.TierThresholdRepository.GetAllActiveAsync();
            return thresholds.Select(ToDto).ToList();
        }

        public async Task<List<TierThresholdResponseDto>> GetAllAsync()
        {
            var thresholds = await _unitOfWork.TierThresholdRepository.GetAllAsync();
            return thresholds.Select(ToDto).ToList();
        }

        public async Task<TierThresholdResponseDto> CreateAsync(CreateTierThresholdDto dto)
        {
            var entity = new TierThreshold
            {
                Name = dto.Name,
                TierLevel = dto.TierLevel,
                MinTotalSpent = dto.MinTotalSpent,
                BenefitDescription = dto.BenefitDescription,
                IsActive = true
            };
            await _unitOfWork.TierThresholdRepository.CreateAsync(entity);
            return ToDto(entity);
        }

        public async Task<TierThresholdResponseDto> UpdateAsync(int id, UpdateTierThresholdDto dto)
        {
            var entity = await _unitOfWork.TierThresholdRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"TierThreshold {id} not found");

            if (dto.Name != null) entity.Name = dto.Name;
            if (dto.TierLevel.HasValue) entity.TierLevel = dto.TierLevel.Value;
            if (dto.MinTotalSpent.HasValue) entity.MinTotalSpent = dto.MinTotalSpent.Value;
            if (dto.BenefitDescription != null) entity.BenefitDescription = dto.BenefitDescription;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;

            await _unitOfWork.TierThresholdRepository.UpdateAsync(entity);
            return ToDto(entity);
        }

        public async Task DeactivateAsync(int id)
        {
            var entity = await _unitOfWork.TierThresholdRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"TierThreshold {id} not found");

            entity.IsActive = false;
            await _unitOfWork.TierThresholdRepository.UpdateAsync(entity);
        }

        private static TierThresholdResponseDto ToDto(TierThreshold t) => new()
        {
            Id = t.Id,
            Name = t.Name,
            TierLevel = t.TierLevel,
            MinTotalSpent = t.MinTotalSpent,
            BenefitDescription = t.BenefitDescription,
            IsActive = t.IsActive
        };
    }
}
