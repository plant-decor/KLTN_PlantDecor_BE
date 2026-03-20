using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class CategoryMapper
    {
        #region Entity to Response
        public static CategoryResponseDto ToResponse(this Category category)
        {
            if (category == null) return null!;
            return new CategoryResponseDto
            {
                Id = category.Id,
                ParentCategoryId = category.ParentCategoryId,
                Name = category.Name,
                IsActive = category.IsActive,
                CategoryType = category.CategoryType,
                CategoryTypeName = ((DataAccessLayer.Enums.CategoryTypeEnum)category.CategoryType).ToString(),
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                ParentCategoryName = category.ParentCategory?.Name
            };
        }

        public static CategoryResponseDto ToResponseWithChildren(this Category category)
        {
            if (category == null) return null!;
            var dto = category.ToResponse();
            dto.SubCategories = category.InverseParentCategory
                .Select(c => c.ToResponseWithChildren())
                .ToList();
            return dto;
        }

        public static List<CategoryResponseDto> ToResponseList(this IEnumerable<Category> categories)
        {
            return categories.Select(c => c.ToResponse()).ToList();
        }

        public static List<CategoryResponseDto> ToResponseListWithChildren(this IEnumerable<Category> categories)
        {
            return categories.Select(c => c.ToResponseWithChildren()).ToList();
        }
        #endregion

        #region Request to Entity
        public static Category ToEntity(this CategoryRequestDto request)
        {
            if (request == null) return null!;

            return new Category
            {
                Name = request.Name,
                ParentCategoryId = request.ParentCategoryId,
                IsActive = request.IsActive,
                CategoryType = request.CategoryType,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this CategoryUpdateDto request, Category category)
        {
            if (request == null || category == null) return;

            if (request.Name != null)
                category.Name = request.Name;

            if (request.ParentCategoryId.HasValue)
                category.ParentCategoryId = request.ParentCategoryId;

            if (request.IsActive.HasValue)
                category.IsActive = request.IsActive;

            if (request.CategoryType.HasValue)
                category.CategoryType = request.CategoryType.Value;

            category.UpdatedAt = DateTime.Now;
        }
        #endregion
    }
}
