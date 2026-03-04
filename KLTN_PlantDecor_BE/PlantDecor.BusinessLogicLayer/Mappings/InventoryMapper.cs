using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class MaterialMapper
    {
        #region Entity to Response
        public static MaterialResponseDto ToResponse(this Material material)
        {
            if (material == null) return null!;
            return new MaterialResponseDto
            {
                Id = material.Id,
                MaterialCode = material.MaterialCode,
                Name = material.Name,
                Description = material.Description,
                BasePrice = material.BasePrice,
                Unit = material.Unit,
                Brand = material.Brand,
                Specifications = material.Specifications,
                ExpiryMonths = material.ExpiryMonths,
                IsActive = material.IsActive,
                CreatedAt = material.CreatedAt,
                UpdatedAt = material.UpdatedAt,
                Categories = material.Categories.Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParentCategoryId = c.ParentCategoryId,
                    IsActive = c.IsActive
                }).ToList(),
                Tags = material.Tags.Select(t => new TagResponseDto
                {
                    Id = t.Id,
                    TagName = t.TagName
                }).ToList(),
                Images = material.MaterialImages.Select(i => new MaterialImageResponseDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary
                }).ToList()
            };
        }

        public static MaterialListResponseDto ToListResponse(this Material material)
        {
            if (material == null) return null!;
            return new MaterialListResponseDto
            {
                Id = material.Id,
                MaterialCode = material.MaterialCode,
                Name = material.Name,
                BasePrice = material.BasePrice,
                Unit = material.Unit,
                Brand = material.Brand,
                IsActive = material.IsActive,
                PrimaryImageUrl = material.MaterialImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                    ?? material.MaterialImages.FirstOrDefault()?.ImageUrl,
                CategoryNames = material.Categories.Select(c => c.Name).ToList(),
                TagNames = material.Tags.Select(t => t.TagName).ToList()
            };
        }

        public static List<MaterialResponseDto> ToResponseList(this IEnumerable<Material> materials)
        {
            return materials.Select(m => m.ToResponse()).ToList();
        }

        public static List<MaterialListResponseDto> ToListResponseList(this IEnumerable<Material> materials)
        {
            return materials.Select(m => m.ToListResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static Material ToEntity(this MaterialRequestDto request)
        {
            if (request == null) return null!;

            return new Material
            {
                MaterialCode = request.MaterialCode,
                Name = request.Name,
                Description = request.Description,
                BasePrice = request.BasePrice,
                Unit = request.Unit,
                Brand = request.Brand,
                Specifications = request.Specifications,
                ExpiryMonths = request.ExpiryMonths,
                IsActive = request.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this MaterialUpdateDto request, Material material)
        {
            if (request == null || material == null) return;

            material.MaterialCode = request.MaterialCode ?? material.MaterialCode;
            material.Name = request.Name;
            material.Description = request.Description;
            material.BasePrice = request.BasePrice;
            material.Unit = request.Unit;
            material.Brand = request.Brand;
            material.Specifications = request.Specifications;
            material.ExpiryMonths = request.ExpiryMonths;
            material.IsActive = request.IsActive ?? material.IsActive;
            material.UpdatedAt = DateTime.Now;
        }
        #endregion

        #region Helper
        public static string GenerateMaterialCode()
        {
            return $"MAT{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";
        }
        #endregion
    }
}
