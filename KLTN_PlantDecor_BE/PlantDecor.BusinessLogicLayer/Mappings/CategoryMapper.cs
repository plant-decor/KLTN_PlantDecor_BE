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
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this CategoryUpdateDto request, Category category)
        {
            if (request == null || category == null) return;

            category.Name = request.Name;
            category.ParentCategoryId = request.ParentCategoryId;
            category.IsActive = request.IsActive ?? category.IsActive;
            category.UpdatedAt = DateTime.Now;
        }
        #endregion
    }
}
