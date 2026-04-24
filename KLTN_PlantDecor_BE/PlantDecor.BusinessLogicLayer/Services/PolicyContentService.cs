using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class PolicyContentService : IPolicyContentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPolicyKnowledgeService _policyKnowledgeService;

        public PolicyContentService(IUnitOfWork unitOfWork, IPolicyKnowledgeService policyKnowledgeService)
        {
            _unitOfWork = unitOfWork;
            _policyKnowledgeService = policyKnowledgeService;
        }

        public async Task<List<PolicyContentResponseDto>> GetAllActiveAsync()
        {
            var entities = await _policyKnowledgeService.GetAllActiveAsync();
            return entities.Select(MapToDto).ToList();
        }

        public async Task<List<PolicyContentResponseDto>> GetByCategoryActiveAsync(int category)
        {
            ValidateCategory(category);
            var entities = await _policyKnowledgeService.GetByCategoryActiveAsync((PolicyContentCategoryEnum)category);
            return entities.Select(MapToDto).ToList();
        }

        public async Task<PolicyContentResponseDto> GetByIdAsync(int id, bool includeInactive = false)
        {
            var entity = await _unitOfWork.PolicyContentRepository.GetByIdAsync(id);
            if (entity == null || (!includeInactive && entity.IsActive != true))
            {
                throw new NotFoundException($"Policy content {id} not found");
            }

            return MapToDto(entity);
        }

        public async Task<List<PolicyContentResponseDto>> GetAdminListAsync(bool includeInactive = true)
        {
            var entities = await _unitOfWork.PolicyContentRepository.GetAdminListAsync(includeInactive);
            return entities.Select(MapToDto).ToList();
        }

        public async Task<PolicyContentResponseDto> CreateAsync(CreatePolicyContentRequestDto request)
        {
            ValidateCategory(request.Category);

            var normalizedTitle = NormalizeRequired(request.Title, "Title");
            var normalizedContent = NormalizeRequired(request.Content, "Content");

            var exists = await _unitOfWork.PolicyContentRepository.ExistsByTitleInCategoryAsync(normalizedTitle, request.Category);
            if (exists)
            {
                throw new ConflictException("A policy content with the same title already exists in this category.");
            }

            var entity = new PolicyContent
            {
                Title = normalizedTitle,
                Category = request.Category,
                Content = normalizedContent,
                Summary = NormalizeOptional(request.Summary),
                DisplayOrder = request.DisplayOrder ?? 0,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _unitOfWork.PolicyContentRepository.PrepareCreate(entity);
            await _unitOfWork.SaveAsync();
            await _policyKnowledgeService.InvalidatePolicyCacheAsync();

            return MapToDto(entity);
        }

        public async Task<PolicyContentResponseDto> UpdateAsync(int id, UpdatePolicyContentRequestDto request)
        {
            var entity = await _unitOfWork.PolicyContentRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"Policy content {id} not found");
            }

            var targetCategory = request.Category ?? entity.Category ?? (int)PolicyContentCategoryEnum.Other;
            ValidateCategory(targetCategory);

            var targetTitle = request.Title != null ? NormalizeRequired(request.Title, "Title") : (entity.Title ?? string.Empty);
            if (string.IsNullOrWhiteSpace(targetTitle))
            {
                throw new BadRequestException("Title is required.");
            }

            var duplicate = await _unitOfWork.PolicyContentRepository.ExistsByTitleInCategoryAsync(targetTitle, targetCategory, id);
            if (duplicate)
            {
                throw new ConflictException("A policy content with the same title already exists in this category.");
            }

            if (request.Title != null) entity.Title = targetTitle;
            if (request.Category.HasValue) entity.Category = request.Category;
            if (request.Content != null) entity.Content = NormalizeRequired(request.Content, "Content");
            if (request.Summary != null) entity.Summary = NormalizeOptional(request.Summary);
            if (request.DisplayOrder.HasValue) entity.DisplayOrder = request.DisplayOrder;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive;

            entity.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.PolicyContentRepository.PrepareUpdate(entity);
            await _unitOfWork.SaveAsync();
            await _policyKnowledgeService.InvalidatePolicyCacheAsync();

            return MapToDto(entity);
        }

        public async Task<PolicyContentResponseDto> SetActiveStatusAsync(int id, bool isActive)
        {
            var entity = await _unitOfWork.PolicyContentRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"Policy content {id} not found");
            }

            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.PolicyContentRepository.PrepareUpdate(entity);
            await _unitOfWork.SaveAsync();
            await _policyKnowledgeService.InvalidatePolicyCacheAsync();

            return MapToDto(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _unitOfWork.PolicyContentRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"Policy content {id} not found");
            }

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.PolicyContentRepository.PrepareUpdate(entity);
            await _unitOfWork.SaveAsync();
            await _policyKnowledgeService.InvalidatePolicyCacheAsync();
        }

        private static PolicyContentResponseDto MapToDto(PolicyContent entity)
        {
            return new PolicyContentResponseDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Category = entity.Category,
                Content = entity.Content,
                Summary = entity.Summary,
                DisplayOrder = entity.DisplayOrder,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static void ValidateCategory(int category)
        {
            if (!Enum.IsDefined(typeof(PolicyContentCategoryEnum), category))
            {
                throw new BadRequestException($"Unsupported policy category: {category}.");
            }
        }

        private static string NormalizeRequired(string? value, string fieldName)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new BadRequestException($"{fieldName} is required.");
            }

            return normalized;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
