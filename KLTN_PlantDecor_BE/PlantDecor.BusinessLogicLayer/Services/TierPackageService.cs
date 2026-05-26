using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class TierPackageService : ITierPackageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TierPackageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<TierPackageResponseDto>> GetAllActiveAsync()
        {
            var packages = await _unitOfWork.TierPackageRepository.GetAllActiveAsync();
            return packages.Select(ToDto).ToList();
        }

        public async Task<List<TierPackageResponseDto>> GetAllAsync()
        {
            var packages = await _unitOfWork.TierPackageRepository.GetAllAsync();
            return packages.Select(ToDto).ToList();
        }

        public async Task<TierPackageResponseDto> GetByIdAsync(int id)
        {
            var package = await _unitOfWork.TierPackageRepository.GetByIdAsync(id);
            if (package == null)
                throw new NotFoundException($"TierPackage {id} not found");
            return ToDto(package);
        }

        public async Task<TierPackageResponseDto> CreateAsync(CreateTierPackageDto dto)
        {
            var entity = new TierPackage
            {
                Name = dto.Name,
                Price = dto.Price,
                QuotaRequests = dto.QuotaRequests,
                DurationMonths = dto.DurationMonths,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.TierPackageRepository.CreateAsync(entity);
            return ToDto(entity);
        }

        public async Task<TierPackageResponseDto> UpdateAsync(int id, UpdateTierPackageDto dto)
        {
            var entity = await _unitOfWork.TierPackageRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"TierPackage {id} not found");

            if (dto.Name != null) entity.Name = dto.Name;
            if (dto.Price.HasValue) entity.Price = dto.Price.Value;
            if (dto.QuotaRequests.HasValue) entity.QuotaRequests = dto.QuotaRequests.Value;
            if (dto.DurationMonths.HasValue) entity.DurationMonths = dto.DurationMonths.Value;
            if (dto.Description != null) entity.Description = dto.Description;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;

            await _unitOfWork.TierPackageRepository.UpdateAsync(entity);
            return ToDto(entity);
        }

        public async Task DeactivateAsync(int id)
        {
            var entity = await _unitOfWork.TierPackageRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"TierPackage {id} not found");

            entity.IsActive = false;
            await _unitOfWork.TierPackageRepository.UpdateAsync(entity);
        }

        private static TierPackageResponseDto ToDto(TierPackage p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            QuotaRequests = p.QuotaRequests,
            DurationMonths = p.DurationMonths,
            Description = p.Description,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        };
    }
}
