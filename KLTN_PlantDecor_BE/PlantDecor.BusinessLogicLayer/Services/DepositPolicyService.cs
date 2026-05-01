using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class DepositPolicyService : IDepositPolicyService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DepositPolicyService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<DepositPolicyResponseDto>> GetAllAsync()
        {
            var policies = await _unitOfWork.DepositPolicyRepository.GetAllOrderedAsync();
            return policies.Select(MapToDto).ToList();
        }

        public async Task<DepositPolicyResponseDto> GetByIdAsync(int id)
        {
            var policy = await _unitOfWork.DepositPolicyRepository.GetByIdAsync(id);
            if (policy == null)
                throw new NotFoundException($"DepositPolicy {id} not found");

            return MapToDto(policy);
        }

        public async Task<DepositPolicyResponseDto> CreateAsync(DepositPolicyRequestDto request)
        {
            ValidateRange(request.MinPrice, request.MaxPrice);
            ValidatePercentage(request.DepositPercentage);

            if (request.IsActive)
            {
                var hasOverlap = await _unitOfWork.DepositPolicyRepository
                    .HasOverlappingActiveRangeAsync(request.MinPrice, request.MaxPrice);

                if (hasOverlap)
                    throw new ConflictException("Deposit policy range overlaps with an existing active policy");
            }

            var entity = new DepositPolicy
            {
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                DepositPercentage = request.DepositPercentage,
                IsActive = request.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _unitOfWork.DepositPolicyRepository.PrepareCreate(entity);
            await _unitOfWork.SaveAsync();

            return MapToDto(entity);
        }

        public async Task<DepositPolicyResponseDto> UpdateAsync(int id, UpdateDepositPolicyRequestDto request)
        {
            var entity = await _unitOfWork.DepositPolicyRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"DepositPolicy {id} not found");

            var updatedMinPrice = request.MinPrice ?? entity.MinPrice;
            var updatedMaxPrice = request.MaxPrice ?? entity.MaxPrice;
            var updatedDepositPercentage = request.DepositPercentage ?? entity.DepositPercentage;
            var updatedIsActive = request.IsActive ?? entity.IsActive;

            ValidateRange(updatedMinPrice, updatedMaxPrice);
            ValidatePercentage(updatedDepositPercentage);

            if (updatedIsActive)
            {
                var hasOverlap = await _unitOfWork.DepositPolicyRepository
                    .HasOverlappingActiveRangeAsync(updatedMinPrice, updatedMaxPrice, id);

                if (hasOverlap)
                    throw new ConflictException("Deposit policy range overlaps with an existing active policy");
            }

            entity.MinPrice = updatedMinPrice;
            entity.MaxPrice = updatedMaxPrice;
            entity.DepositPercentage = updatedDepositPercentage;
            entity.IsActive = updatedIsActive;
            entity.UpdatedAt = DateTime.Now;

            _unitOfWork.DepositPolicyRepository.PrepareUpdate(entity);
            await _unitOfWork.SaveAsync();

            return MapToDto(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _unitOfWork.DepositPolicyRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"DepositPolicy {id} not found");

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.Now;

            _unitOfWork.DepositPolicyRepository.PrepareUpdate(entity);
            await _unitOfWork.SaveAsync();
        }

        private static void ValidateRange(decimal minPrice, decimal? maxPrice)
        {
            if (minPrice < 0)
                throw new BadRequestException("MinPrice must be greater than or equal to 0");

            if (maxPrice.HasValue && maxPrice.Value < 0)
                throw new BadRequestException("MaxPrice must be greater than or equal to 0");

            if (maxPrice.HasValue && maxPrice.Value <= minPrice)
                throw new BadRequestException("MaxPrice must be greater than MinPrice");
        }

        private static void ValidatePercentage(int percentage)
        {
            if (percentage < 1 || percentage > 100)
                throw new BadRequestException("DepositPercentage must be between 1 and 100");
        }

        private static DepositPolicyResponseDto MapToDto(DepositPolicy entity)
        {
            return new DepositPolicyResponseDto
            {
                Id = entity.Id,
                MinPrice = entity.MinPrice,
                MaxPrice = entity.MaxPrice,
                DepositPercentage = entity.DepositPercentage,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
